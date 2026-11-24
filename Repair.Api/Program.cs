namespace Repair.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var startup = new ApiStartup();

        startup.SetupServices(builder.Services);

        var app = builder.Build();

        startup.SetupApplication(app);
        startup.FlushStartupLogs();

        app.Run();
    }
}
