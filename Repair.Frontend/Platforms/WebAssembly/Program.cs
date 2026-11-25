using Uno.UI.Hosting;

namespace Repair.Frontend;

public class Program
{
    public static int Main(string[] args)
    {
        UnoPlatformHost host = UnoPlatformHostBuilder.Create().App(() => new App()).UseWebAssembly().Build();

        host.Run();

        return 0;
    }
}
