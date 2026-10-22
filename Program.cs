using System.Windows.Forms;

namespace E_Key_Monitor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        string[] args = Environment.GetCommandLineArgs();
        bool launchGame = HasFlag(args, "--launch-game");
        bool watchMode = HasFlag(args, "--watch");
        bool followGame = launchGame || watchMode || HasFlag(args, "--follow-game");

        // Per-monitor awareness keeps the window in real device pixels, so the
        // dot can be sized against the screen instead of against the Windows
        // scaling percentage.
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(followGame, launchGame, watchMode));
    }

    private static bool HasFlag(string[] args, string flag)
    {
        return args.Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
    }
}
