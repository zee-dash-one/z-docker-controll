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

        var seedPath = Path.Combine(AppContext.BaseDirectory, "config", "services.default.json");
        var runtimeDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZLab");

        var store = new ServiceCatalogStore(runtimeDir, seedPath);
        try
        {
            store.EnsureSeeded();
            store.Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Não foi possível carregar o catálogo de serviços.\n\n" + ex.Message,
                "Z-Lab",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var docker = new DockerCli();
        var containers = new ContainerService(docker);
        var backups = new BackupService(docker, containers);
        var lan = new LanExposeHelper();
        var localHosts = new LocalHostsHelper();
        var watcher = new StatusWatcher(store, containers);
        var scheduledBackups = new ScheduledBackupService(store, backups);

        using var tray = new TrayApp(store, containers, backups, lan, localHosts, watcher, scheduledBackups);
        tray.Run();
    }
}
