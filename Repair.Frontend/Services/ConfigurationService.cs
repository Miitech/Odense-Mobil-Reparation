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

        string baseUri = GetDocumentBaseUri("document.baseURI");
        var address = new Uri(new Uri(baseUri), APP_SETTINGS_FILE);

        byte[] content = await client.GetByteArrayAsync(address);

        using var stream = new MemoryStream(content);

        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();

        return configuration;
#else
        throw new PlatformNotSupportedException(
            "Hosted configuration requires WebAssembly.");
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
