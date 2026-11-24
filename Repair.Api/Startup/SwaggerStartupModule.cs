using Microsoft.OpenApi;
using Repair.Abstractions.Startup;

namespace Repair.Api.Startup;

public class SwaggerStartupModule : IServiceStartupModule, IApplicationStartupModule<IApplicationBuilder>
{
    private readonly string apiTitle;

    public SwaggerStartupModule(string apiTitle)
    {
        this.apiTitle = apiTitle;
    }

    public string Name => "Swagger Module";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = apiTitle,
                Version = "v1",
            });
        });
    }

    /// <inheritdoc />
    public void ConfigureApplication(IApplicationBuilder app)
    {
        if (app is not WebApplication webApplication)
            throw new InvalidOperationException(
                $"Expected Supplied App to be of type {nameof(WebApplication)}, but it was a {app.GetType().Name}.");

        if (!webApplication.Environment.IsDevelopment())
            return;

        webApplication.UseSwagger();

        webApplication.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", apiTitle);
            options.RoutePrefix = string.Empty;
        });
    }
}
