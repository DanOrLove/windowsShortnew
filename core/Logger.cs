using System.IO;

namespace CustomUninstaller.Core;

public static class UninstallLogger
{
    private static readonly object _lock = new();
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CustomUninstaller", "Logs");
    private static readonly string LogFile = Path.Combine(LogDir, $"uninstall_{DateTime.Now:yyyy-MM-dd}.log");

    static UninstallLogger() => Directory.CreateDirectory(LogDir);

    public enum Level { Info, Warning, Error, Success }

    public static void Write(string message, Level level = Level.Info)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss.fff}] [{level,-7}] {message}{Environment.NewLine}";
        lock (_lock) File.AppendAllText(LogFile, entry);
    }
}