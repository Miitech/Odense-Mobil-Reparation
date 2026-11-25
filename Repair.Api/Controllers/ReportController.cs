using Microsoft.AspNetCore.Mvc;
using Repair.Abstractions.Api;
using Repair.Abstractions.Entity.Model;
using Repair.Abstractions.Persistence;
using Repair.Abstractions.Services;
using Repair.Api.Exceptions;
using Repair.Models.Entity.Model;
using Repair.Models.Entity.Searchable;

namespace Repair.Api.Controllers;

[ApiController]
[Route(ApiRoutes.CONTROLLER_TEMPLATE)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ReportController(
    IEntityQueryService<Customer, SearchableCustomer> customerQueryService,
    IEntityQueryService<Order, SearchableOrder> orderQueryService,
    IReportService reportService,
    ILogger<ReportController> logger) : ControllerBase, IReportApi
{
    [HttpGet(nameof(GenerateOrderReport) + ApiRoutes.CONSTRAINED_ID)]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateOrderReport(int id)
    {
        var report = await ((IReportApi)this).GenerateOrderReport(id);
        return File(report.Content, "application/pdf", report.FileName);
    }

    async Task<GeneratedReport> IReportApi.GenerateOrderReport(int id)
    {
        var order = await orderQueryService.GetEntity(new SearchableOrder { Id = id });
        if (order is null)
        {
            logger.LogWarning(
                "Cannot generate report for Order with ID {OrderId}, as it does not exist.", id);

            throw new EntityNotFoundException(typeof(Order), id);
        }

        return reportService.GenerateReport(order);
    }

    [HttpGet(nameof(GenerateCustomerReport) + ApiRoutes.CONSTRAINED_ID)]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateCustomerReport(int id)
    {
        var report = await ((IReportApi)this).GenerateCustomerReport(id);
        return File(report.Content, "application/pdf", report.FileName);
    }

    async Task<GeneratedReport> IReportApi.GenerateCustomerReport(int id)
    {
        var customer = await customerQueryService.GetEntity(new SearchableCustomer { Id = id });
        if (customer is null)
        {
            logger.LogWarning(
                "Cannot generate report for Customer with ID {CustomerId}, as it does not exist.", id);

            throw new EntityNotFoundException(typeof(Customer), id);
        }

        var orders = await orderQueryService.GetEntities(new SearchableOrder { CustomerId = id });
        customer.Orders = orders.Cast<IOrder>().ToList();

        return reportService.GenerateReport(customer);
    }
}
