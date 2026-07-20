using System.Windows.Forms;

namespace E_Key_Monitor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        string[] args = Environment.GetCommandLineArgs();
        bool followGame = args.Any(
            arg => string.Equals(
                arg,
                "--follow-game",
                StringComparison.OrdinalIgnoreCase));

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(followGame));
    }
}
