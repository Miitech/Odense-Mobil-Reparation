using Repair.Abstractions.Api;
using Repair.Abstractions.Services;

namespace Repair.Frontend.Services;

public class ReportApiAdapter(IReportHttpApi reportHttpApi) : IReportApi
{
    public async Task<GeneratedReport> GenerateOrderReport(int id)
    {
        using var response = await reportHttpApi.GenerateOrderReport(id);
        return await CreateGeneratedReport(response);
    }

    public async Task<GeneratedReport> GenerateCustomerReport(int id)
    {
        using var response = await reportHttpApi.GenerateCustomerReport(id);
        return await CreateGeneratedReport(response);
    }

    private async Task<GeneratedReport> CreateGeneratedReport(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        var disposition = response.Content.Headers.ContentDisposition;
        string? fileName = (disposition?.FileNameStar ?? disposition?.FileName)?.Trim('"');
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("The report response did not contain a filename.");
        }

        byte[] content = await response.Content.ReadAsByteArrayAsync();
        return new GeneratedReport(fileName, content);
    }
}
