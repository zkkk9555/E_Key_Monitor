using Microsoft.Win32;

namespace E_Key_Monitor;

/// <summary>
/// Optional start-with-Windows entry kept in the current user's Run key.
/// Nothing is written until the user turns the switch on in the tray menu, and
/// removing the value is the exact inverse, so the feature is reversible.
/// </summary>
internal static class AutoStartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "E_Key_Monitor";

    /// <summary>
    /// The executable that should be launched at sign-in. Falls back to the
    /// app folder when the process is hosted by the dotnet muxer.
    /// </summary>
    public static string GetExecutablePath()
    {
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath) &&
            !Path.GetFileName(processPath).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            return processPath;
        }

        return Path.Combine(AppContext.BaseDirectory, "E_Key_Monitor.exe");
    }

    public static string? GetRegisteredCommandLine()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(ValueName) as string;
    }

    public static bool IsEnabled()
    {
        return !string.IsNullOrWhiteSpace(GetRegisteredCommandLine());
    }

    /// <summary>
    /// True only when the registered entry points at the executable that is
    /// running right now. A portable folder that was moved leaves a stale
    /// entry behind, and that must not show as "enabled".
    /// </summary>
    public static bool IsEnabledForCurrentExecutable()
    {
        string? registered = GetRegisteredCommandLine();
        if (string.IsNullOrWhiteSpace(registered))
        {
            return false;
        }

        string? registeredPath = ExtractExecutablePath(registered);
        if (string.IsNullOrWhiteSpace(registeredPath))
        {
            return false;
        }

        // Exact path comparison: a prefix match would count
        // "E_Key_Monitor.exe.bak" as the running executable.
        return string.Equals(registeredPath, GetExecutablePath(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractExecutablePath(string commandLine)
    {
        string trimmed = commandLine.Trim();
        if (trimmed.StartsWith("\"", StringComparison.Ordinal))
        {
            int closingQuote = trimmed.IndexOf('"', 1);
            return closingQuote > 1 ? trimmed.Substring(1, closingQuote - 1) : null;
        }

        int firstSpace = trimmed.IndexOf(' ');
        return firstSpace > 0 ? trimmed.Substring(0, firstSpace) : trimmed;
    }

    public static void Enable(string executablePath, string arguments)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true)
            ?? throw new InvalidOperationException($"无法打开注册表键：{RunKeyPath}");
        key.SetValue(ValueName, $"\"{executablePath}\" {arguments}", RegistryValueKind.String);
    }

    public static void Disable()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(ValueName, false);
    }
}
