namespace CustomUninstaller.Core;

public record InstalledProgram(
    string DisplayName,
    string UninstallString,
    string? QuietUninstallString,
    string? IconPath,
    long SizeKB,
    string RegistryPath,
    bool IsSystemComponent
);