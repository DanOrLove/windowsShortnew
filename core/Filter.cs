using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace CustomUninstaller.Core;

public static class SystemFilter
{
    private static readonly HashSet<string> SystemPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "update for windows", "security update", "hotfix", @"^kb\d+", "cumulative update",
        "windows driver", "microsoft .net", @"visual c\+\+ redistributable",
        "microsoft office", "windows feature", "windows defender", "microsoft edge",
        "intel driver", "amd software", "nvidia", "realtek"
    };

    public static bool IsSystem(RegistryKey key, string displayName)
    {
        var sysComp = key.GetValue("SystemComponent");
        if (sysComp is int i1 && i1 == 1) return true;

        var noRemove = key.GetValue("NoRemove");
        if (noRemove is int i2 && i2 == 1) return true;

        var parent = key.GetValue("ParentDisplayName") as string;
        if (!string.IsNullOrWhiteSpace(parent)) return true;

        return SystemPatterns.Any(p => Regex.IsMatch(displayName, p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }
}