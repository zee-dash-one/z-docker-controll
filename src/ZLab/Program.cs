using ZLab.Services;

namespace ZLab;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

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

        var session = new SshSession();
        var monitor = new ServerMonitor(session, settings);
        var watcher = new StatusWatcher(monitor);
        using var tray = new TrayApp(session, monitor, watcher);
        tray.Run();
    }
}
