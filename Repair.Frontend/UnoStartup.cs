using System.Text.Json;
using Microsoft.UI.Dispatching;
using Repair.Abstractions.Api;
using Repair.Frontend.Abstraction;
using Repair.Frontend.NavigationRegions;
using Repair.Frontend.Services;
using Repair.Frontend.Startup;
using Repair.Startup;
using Repair.Startup.Modules;

namespace Repair.Frontend;

internal class UnoStartup : ModularStartup<IApplicationBuilder>
{
    public IHost? Host { get; private set; }

    public UnoStartup(Uri apiBaseAddress)
    {
        AddModule(new LoggingStartupModule(new[]
        {
            LogTarget.BROWSER_CONSOLE,
        }));

        AddModule(new RefitStartupModule(apiBaseAddress, QueueStartupLog));
    }

    protected override void ConfigureApplication(IApplicationBuilder app)
    {
        app.Configure(host => host
#if DEBUG
            // Switch to Development environment when running in DEBUG
            .UseEnvironment(Environments.Development)
#endif
            .UseConfiguration(configure: ConfigureConfigurationSource).UseSerialization(ConfigureSerialization));

        base.ConfigureApplication(app);

        Host = app.Build();
    }

    private void ConfigureSerialization(HostBuilderContext builderContext, IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton(new JsonSerializerOptions {IncludeFields = true,});
    }

    private IHostBuilder ConfigureConfigurationSource(IConfigBuilder configBuilder)
    {
        return configBuilder.EmbeddedSource<App>().Section<AppConfig>();
    }

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<IReportApi, ReportApiAdapter>();

        DispatcherQueue uiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
        services.AddSingleton(uiDispatcherQueue);
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<ArgumentsFactory>();
        services.AddSingleton<TrialService>();

        services.AddSingleton<IPageRegion, CustomersPageRegionDefinition>();
        services.AddSingleton<IPageRegion, OrdersPageRegionDefinition>();
        services.AddSingleton<IPageRegion, CreateCustomerPageRegionDefinition>();
        services.AddSingleton<IPageRegion, CreateOrderPageRegionDefinition>();
    }
}
