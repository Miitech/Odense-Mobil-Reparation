using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Repair.Abstractions.Api;
using Repair.Abstractions.Startup;
using Repair.Models.Api;
using Repair.Models.Serialization;

namespace Repair.Frontend.Startup;

public class RefitStartupModule(Uri baseAddress, Action<Action<ILogger>> queueStartupLog) : IServiceStartupModule
{
    public string Name => "Refit Module";

    public void ConfigureServices(IServiceCollection services)
    {
        var settings = CreateSettings();

        RegisterClient<ICustomerApi>(services, settings);
        RegisterClient<IOrderApi>(services, settings);
        RegisterClient<IReportHttpApi>(services, settings);
    }

    private void RegisterClient<TApi>(IServiceCollection services, RefitSettings settings) where TApi : class
    {
        services.AddRefitGeneratedClient<TApi>(settings)
            .ConfigureHttpClient(client => client.BaseAddress = baseAddress);

        queueStartupLog(logger => logger.LogDebug(
            "Registered Refit client '{ApiName}' with base address '{BaseAddress}'.", typeof(TApi).Name, baseAddress));
    }

    private RefitSettings CreateSettings()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            IncludeFields = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            TypeInfoResolver = ApiJsonSerializerContext.Default,
        };

        return new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(options),
        };
    }
}
