namespace RamDump.Services;

public enum ProcessCategory
{
    Other,
    Browser,
    Dev,
    System,
}

public static class ProcessClassifier
{
    private static readonly HashSet<string> BrowserNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "firefox", "chrome", "msedge", "msedgewebview2", "opera", "brave",
        "vivaldi", "steamwebhelper", "iexplore", "yandex", "tor",
    };

    private static readonly HashSet<string> DevNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "code", "code - insiders", "claude", "dotnet", "node", "python",
        "pwsh", "powershell", "WindowsTerminal", "git", "discord",
        "cryptomator", "ramdump-stack", "devenv", "rider", "rider64",
        "msbuild", "javaw", "java", "ServiceHub.Host.CLR.x64",
    };

    public static ProcessCategory Classify(string name, bool isSystem)
    {
        if (BrowserNames.Contains(name)) return ProcessCategory.Browser;
        if (DevNames.Contains(name)) return ProcessCategory.Dev;
        if (isSystem) return ProcessCategory.System;
        return ProcessCategory.Other;
    }

    public static string ToTag(this ProcessCategory c) => c switch
    {
        ProcessCategory.Browser => "browser",
        ProcessCategory.Dev => "dev",
        ProcessCategory.System => "sys",
        _ => "other",
    };
}
