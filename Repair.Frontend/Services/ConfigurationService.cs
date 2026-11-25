using Microsoft.Extensions.Configuration;
#if __WASM__
using System.Runtime.InteropServices.JavaScript;
#endif

namespace Repair.Frontend.Services;

public partial class ConfigurationService
{
    public const string APP_SETTINGS_FILE = "appsettings.json";
    public const string API_BASE_ADDRESS = "Api:BaseAddress";

    public async Task<IConfiguration> BuildConfiguration()
    {
#if __WASM__
        using var client = new HttpClient();
        var address = new Uri(new Uri(GetDocumentBaseUri("document.baseURI")), APP_SETTINGS_FILE);
        using var stream = await client.GetStreamAsync(address);
        return new ConfigurationBuilder().AddJsonStream(stream).Build();
#else
        throw new PlatformNotSupportedException("Hosted configuration requires WebAssembly.");
#endif
    }

    public Uri GetApiBaseAddress(IConfiguration configuration)
    {
        if (!Uri.TryCreate(configuration[API_BASE_ADDRESS], UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"Missing or invalid configuration: {API_BASE_ADDRESS}.");
        }

        return uri;
    }

#if __WASM__
    [JSImport("globalThis.eval")]
    private static partial string GetDocumentBaseUri(string expression);
#endif
}
