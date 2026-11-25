
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Net.Http.Headers;
using Repair.Abstractions.Services;
using Repair.Api.Handlers;
using Repair.Api.Startup;
using Repair.Models.Entity.Model;
using Repair.Models.Entity.Searchable;
using Repair.Models.Settings;
using Repair.Persistence;
using Repair.Persistence.Services;
using Repair.Services;
using Repair.Startup;
using Repair.Startup.Modules;

namespace Repair.Api;

public class ApiStartup : ModularStartup<IApplicationBuilder>
{
    private const string CORS_POLICY = "RepairFrontend";

    private readonly IConfiguration configuration;
    private readonly ConfigurationService configurationService;

    public ApiStartup()
    {
        configurationService = new ConfigurationService(GetType().Assembly);
        configuration = configurationService.BuildConfiguration();

        AddModule(new LoggingStartupModule(new[]
        {
            LogTarget.CONSOLE,
            LogTarget.FILE,
        }, configurationService.GetApplicationDataPath()));

        AddModule(new SwaggerStartupModule("Repair"));

        AddModule(
            new DatabaseContextStartupModule<RepairDatabaseContext>(
                configurationService.ConfigureDatabaseOptions));

        AddModule(new EntityQueryServiceStartupModule<CustomerQueryService, Customer, SearchableCustomer>());
        AddModule(new EntityQueryServiceStartupModule<OrderQueryService, Order, SearchableOrder>());

#if DEBUG
        AddModule(new BogusStartupModule(40));
#endif
    }

    /// <inheritdoc />
    protected override void ConfigureApplication(IApplicationBuilder app)
    {
        base.ConfigureApplication(app);

        app.UseExceptionHandler();

        if (app is not WebApplication webApplication)
            throw new InvalidOperationException(
                $"Expected Supplied App to be of type {nameof(WebApplication)}, but it was a {app.GetType().Name}.");

        webApplication.UseHttpsRedirection();
        webApplication.UseCors(CORS_POLICY);
        webApplication.UseAuthorization();

        webApplication.MapControllers();
    }

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton(configurationService);

        services.AddSingleton<IReportService, ReportService>();
        services.Configure<ReportDataSettings>(
            configuration.GetSection(ConfigurationService.REPORT_DATA_SECTION));

        services.AddSingleton(new JsonSerializerOptions
        {
            IncludeFields = true,
        });

        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();

        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.IncludeFields = true;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddAuthorization();

        services.AddCors(options =>
        {
            options.AddPolicy(CORS_POLICY,
                policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
                    .WithExposedHeaders(HeaderNames.ContentDisposition));
        });
    }
}
