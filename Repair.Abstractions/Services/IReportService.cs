using Repair.Abstractions.Entity.Model;

namespace Repair.Abstractions.Services;

public interface IReportService
{
    GeneratedReport GenerateReport(IOrder order);
    GeneratedReport GenerateReport(ICustomer customer);

    Task<string> CreateReport(IOrder order);
    Task<string> CreateReport(ICustomer customer);
}
