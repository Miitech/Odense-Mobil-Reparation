using System.Text.Json;
using Repair.Abstractions.Services;
#if __WASM__
using System.Runtime.InteropServices.JavaScript;
#endif

namespace Repair.Frontend.Services;

internal partial class ReportDownloadService
{
    internal void Download(GeneratedReport report)
    {
#if __WASM__
        string fileName = JsonSerializer.Serialize(report.FileName);
        string content = JsonSerializer.Serialize(Convert.ToBase64String(report.Content));
        DownloadInBrowser($$"""
            (() => {
                const bytes = Uint8Array.from(atob({{content}}), c => c.charCodeAt(0));
                const url = URL.createObjectURL(new Blob([bytes], { type: 'application/pdf' }));
                const link = document.createElement('a');
                link.href = url;
                link.download = {{fileName}};
                document.body.appendChild(link);
                link.click();
                link.remove();
                setTimeout(() => URL.revokeObjectURL(url), 1000);
            })()
            """);
#else
        throw new PlatformNotSupportedException("Report downloading requires WebAssembly.");
#endif
    }

#if __WASM__
    [JSImport("globalThis.eval")]
    private static partial void DownloadInBrowser(string expression);
#endif
}
