using Repair.Abstractions.Services;

namespace Repair.Abstractions.Api;

public interface IReportApi
{
    Task<GeneratedReport> GenerateOrderReport(int id);

    Task<GeneratedReport> GenerateCustomerReport(int id);
}
