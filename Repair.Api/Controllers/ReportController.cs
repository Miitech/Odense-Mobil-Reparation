using Microsoft.AspNetCore.Mvc;
using Repair.Abstractions.Entity.Model;
using Repair.Abstractions.Persistence;
using Repair.Abstractions.Services;
using Repair.Models.Entity.Model;
using Repair.Models.Entity.Searchable;

namespace Repair.Api.Controllers;

[ApiController]
[Route(Constants.ROUTE_TEMPLATE)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ReportController(
    IEntityQueryService<Customer, SearchableCustomer> customerQueryService,
    IEntityQueryService<Order, SearchableOrder> orderQueryService,
    IReportService reportService) : ControllerBase
{
    [HttpGet("{id:int:min(1)}")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateOrderReport(int id)
    {
        var order = await orderQueryService.GetEntity(new SearchableOrder { Id = id });
        if (order is null)
            return NotFound();

        var report = reportService.GenerateReport(order);
        return File(report.Content, "application/pdf", report.FileName);
    }

    [HttpGet("{id:int:min(1)}")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateCustomerReport(int id)
    {
        var customer = await customerQueryService.GetEntity(new SearchableCustomer { Id = id });
        if (customer is null)
            return NotFound();

        var orders = await orderQueryService.GetEntities(new SearchableOrder { CustomerId = id });
        customer.Orders = orders.Cast<IOrder>().ToList();

        var report = reportService.GenerateReport(customer);
        return File(report.Content, "application/pdf", report.FileName);
    }
}
