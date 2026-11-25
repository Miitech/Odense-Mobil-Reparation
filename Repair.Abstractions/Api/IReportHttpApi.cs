using Refit;

namespace Repair.Abstractions.Api;

[PathPrefix(ApiRoutes.REPORT_PREFIX)]
public interface IReportHttpApi
{
    [Get("/" + nameof(GenerateOrderReport) + ApiRoutes.ID)]
    Task<HttpResponseMessage> GenerateOrderReport(int id);

    [Get("/" + nameof(GenerateCustomerReport) + ApiRoutes.ID)]
    Task<HttpResponseMessage> GenerateCustomerReport(int id);
}
