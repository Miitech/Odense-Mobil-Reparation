using Uno.UI.Hosting;

namespace Repair.Frontend;

public class Program
{
    public static async Task Main(string[] args)
    {
        UnoPlatformHost host = UnoPlatformHostBuilder.Create().App(() => new App()).UseWebAssembly().Build();

        await host.RunAsync();
    }
}
