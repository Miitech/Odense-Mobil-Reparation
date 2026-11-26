using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Repair.Frontend.Styles;
using Repair.Frontend.Services;
using Uno.Resizetizer;

namespace Repair.Frontend;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    private Window? MainWindow { get; set; }
    protected IHost? Host { get; private set; }
    internal static UnoStartup Startup { get; private set; } = null!;

    [SuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Uno.Extensions APIs are used in a way that is safe for trimming in this template context.")]
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var configurationService = new ConfigurationService();

            IConfiguration deploymentConfiguration = await configurationService.BuildConfiguration();

            Startup = new UnoStartup(configurationService.GetApiBaseAddress(deploymentConfiguration),
                configurationService.GetSearchSettings(deploymentConfiguration));

            // Load WinUI Resources
            Resources.Build(r => r.Merged(new XamlControlsResources()));

            // Load Uno.UI.Toolkit and Material Resources
            Resources.Build(r => r.Merged(
                new MaterialToolkitTheme(new ColorPaletteOverride(), new MaterialFontsOverride())));

            IApplicationBuilder builder = this.CreateBuilder(args);

            Startup.SetupApplication(builder).Configure(host =>
                host.ConfigureServices(collection => Startup.SetupServices(collection)));

            MainWindow = builder.Window;

#if DEBUG
            MainWindow.UseStudio();
#endif

            MainWindow.SetWindowIcon();

            Host = builder.Build();

            ActivateStartingWindow(MainWindow, args);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[Startup ERROR] Application launch failed: {exception}");
            throw;
        }
    }

    private void ActivateStartingWindow(Window mainWindow, LaunchActivatedEventArgs args)
    {
        if (mainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            mainWindow.Content = rootFrame;
        }

        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        Startup.FlushStartupLogs();

        mainWindow.Activate();
    }
}
