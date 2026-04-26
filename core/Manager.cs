using System.Diagnostics;
using Microsoft.Win32;

namespace CustomUninstaller.Core;

public static class UninstallManager
{
    public static List<InstalledProgram> GetInstalledPrograms(bool hideSystem = true)
    {
        var programs = new List<InstalledProgram>();
        var hives = new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser };
        var views = new[] { RegistryView.Registry64, RegistryView.Registry32 };

        foreach (var view in views)
        {
            foreach (var hive in hives)
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                    if (uninstallKey == null) continue;

                    foreach (var keyName in uninstallKey.GetSubKeyNames())
                    {
                        using var key = uninstallKey.OpenSubKey(keyName);
                        if (key == null) continue;

                        var displayName = key.GetValue("DisplayName") as string;
                        var uninstall = key.GetValue("UninstallString") as string;
                        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(uninstall))
                            continue;

                        bool isSystem = SystemFilter.IsSystem(key, displayName);
                        if (hideSystem && isSystem) continue;

                        var sizeObj = key.GetValue("EstimatedSize");
                        long sizeKB = sizeObj switch
                        {
                            int i => i,
                            uint u => u,
                            long l => l,
                            _ => 0
                        };

                        programs.Add(new InstalledProgram(
                            DisplayName: displayName,
                            UninstallString: uninstall,
                            QuietUninstallString: key.GetValue("QuietUninstallString") as string,
                            IconPath: key.GetValue("DisplayIcon") as string,
                            SizeKB: sizeKB,
                            RegistryPath: $"{hive}\\{key.Name}",
                            IsSystemComponent: isSystem
                        ));
                    }
                }
                catch (Exception ex)
                {
                    UninstallLogger.Write($"⚠️ Ошибка чтения реестра ({hive}/{view}): {ex.Message}", UninstallLogger.Level.Warning);
                }
            }
        }

        UninstallLogger.Write($"📋 Загружено программ: {programs.Count}", UninstallLogger.Level.Info);
        return programs;
    }

    public static async Task<bool> UninstallAsync(InstalledProgram app, bool silent = true)
    {
        var cmd = PrepareUninstallCommand(app, silent);
        if (string.IsNullOrEmpty(cmd))
        {
            UninstallLogger.Write($"❌ Нет команды удаления: {app.DisplayName}", UninstallLogger.Level.Error);
            return false;
        }

        UninstallLogger.Write($"🚀 Запуск: {app.DisplayName} | Команда: {cmd}", UninstallLogger.Level.Info);

        var (fileName, arguments) = ParseCommandLine(cmd);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var proc = Process.Start(startInfo);
            if (proc == null) return false;

            await proc.WaitForExitAsync();
            int exitCode = proc.ExitCode;

            if (exitCode is 0 or 3010 or 1641)
            {
                UninstallLogger.Write($"✅ Успех ({exitCode}): {app.DisplayName}", UninstallLogger.Level.Success);
                return true;
            }

            UninstallLogger.Write($"⚠️ Код {exitCode}: {app.DisplayName}", UninstallLogger.Level.Warning);
            return false;
        }
        catch (Exception ex)
        {
            UninstallLogger.Write($"💥 Ошибка запуска: {ex.Message} | {app.DisplayName}", UninstallLogger.Level.Error);
            return false;
        }
    }

    private static string PrepareUninstallCommand(InstalledProgram app, bool silent)
    {
        if (silent && !string.IsNullOrWhiteSpace(app.QuietUninstallString))
            return app.QuietUninstallString;

        string cmd = app.UninstallString;
        if (string.IsNullOrEmpty(cmd)) return string.Empty;

        if (cmd.Contains("msiexec", StringComparison.OrdinalIgnoreCase))
        {
            if (!cmd.Contains("/qn", StringComparison.OrdinalIgnoreCase) && 
                !cmd.Contains("/quiet", StringComparison.OrdinalIgnoreCase))
            {
                return $"{cmd} /qn /norestart";
            }
            return cmd;
        }

        if (silent && !cmd.Contains(" /", StringComparison.OrdinalIgnoreCase))
        {
            return $"{cmd} /VERYSILENT /SUPPRESSMSGBOXES /NORESTART";
        }

        return cmd;
    }

    private static (string FileName, string Arguments) ParseCommandLine(string command)
    {
        var cmd = command.Trim();
        if (string.IsNullOrEmpty(cmd)) return (string.Empty, string.Empty);

        if (cmd.StartsWith("\""))
        {
            var endQuote = cmd.IndexOf('"', 1);
            if (endQuote > 0)
            {
                var file = cmd.Substring(1, endQuote - 1);
                var args = (endQuote + 1 < cmd.Length) ? cmd.Substring(endQuote + 1).Trim() : string.Empty;
                return (file, args);
            }
        }

        var parts = cmd.Split(new[] { ' ' }, 2);
        return (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
    }
}