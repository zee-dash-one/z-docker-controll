using ZLab.Services;
using ZLab.Ui;

namespace ZLab;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (args.Any(a => string.Equals(a, "--galeria", StringComparison.OrdinalIgnoreCase)))
        {
            ThemeManager.Apply(ThemeMode.Dark);
            Application.Run(new ComponentGalleryForm());
            return;
        }

        var runtimeDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZLab");

        var settings = new MonitorSettingsStore(runtimeDir);
        try
        {
            settings.Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Não foi possível carregar as preferências.\n\n" + ex.Message,
                "Z-Lab",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        ThemeManager.Apply(ThemeMode.System);
        var session = new SshSession();
        var monitor = new ServerMonitor(session, settings);
        var watcher = new StatusWatcher(monitor);
        var stats = new StatsPoller(session);
        using var tray = new TrayApp(session, monitor, watcher, stats);
        tray.Run();
    }
}
