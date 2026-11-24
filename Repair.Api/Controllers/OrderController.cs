using Microsoft.AspNetCore.Mvc;
using Repair.Abstractions.Persistence;
using Repair.Api.Controllers.Core;
using Repair.Models.Dto.Add;
using Repair.Models.Dto.Get;
using Repair.Models.Dto.Update;
using Repair.Models.Entity.ComplexSearchable;
using Repair.Models.Entity.Model;
using Repair.Models.Entity.Searchable;

namespace Repair.Api.Controllers;

public class OrderController : EntityController<
    Order,
    SearchableOrder,
    ComplexSearchableOrder,
    AddOrderDto,
    UpdateOrderDto,
    GetOrderDto,
    OrderController>
{
    private readonly IEntityQueryService<Customer, SearchableCustomer> customerQueryService;
    private readonly Dictionary<int, Customer> customers = new();

    public OrderController(
        IEntityQueryService<Order, SearchableOrder> queryService,
        IEntityQueryService<Customer, SearchableCustomer> customerQueryService,
        ILogger<OrderController> logger)
        : base(queryService, logger)
    {
        this.customerQueryService = customerQueryService;
    }

    public override async Task<ActionResult<GetOrderDto>> AddSingle([FromBody] AddOrderDto dto)
    {
        if (!await LoadCustomer(dto.CustomerId))
            return BadRequest("Invalid customer ID.");

        return await base.AddSingle(dto);
    }

    public override async Task<ActionResult<IEnumerable<GetOrderDto>>> AddMultiple(
        [FromBody] IEnumerable<AddOrderDto> dtos)
    {
        var orders = dtos.ToList();
        foreach (int customerId in orders.Select(x => x.CustomerId).Distinct())
        {
            if (!await LoadCustomer(customerId))
                return BadRequest($"Invalid customer ID: {customerId}.");
        }

        return await base.AddMultiple(orders);
    }

    public override async Task<ActionResult<GetOrderDto>> UpdateSingle(int id, [FromBody] UpdateOrderDto dto)
    {
        if (!await LoadCustomer(dto.CustomerId))
            return BadRequest("Invalid customer ID.");

        return await base.UpdateSingle(id, dto);
    }

    private async Task<bool> LoadCustomer(int id)
    {
        if (id <= 0)
            return false;

        var customer = await customerQueryService.GetEntity(new SearchableCustomer { Id = id });
        if (customer is null)
            return false;

        customers[id] = customer;
        return true;
    }

    protected override Order CreateEntity(AddOrderDto dto)
    {
        return new Order
        {
            HandInWhat = dto.HandInWhat,
            RepairWhat = dto.RepairWhat,
            CustomerId = dto.CustomerId,
            BorrowedPhone = dto.BorrowedPhone,
            HandInWhen = dto.HandInWhen,
            ReturnedWhen = dto.ReturnedWhen,
            IsOrderComplete = dto.IsOrderComplete,
            Customer = customers[dto.CustomerId],
        };
    }

    protected override void UpdateEntity(Order entity, UpdateOrderDto dto)
    {
        entity.HandInWhat = dto.HandInWhat;
        entity.RepairWhat = dto.RepairWhat;
        entity.CustomerId = dto.CustomerId;
        entity.BorrowedPhone = dto.BorrowedPhone;
        entity.HandInWhen = dto.HandInWhen;
        entity.ReturnedWhen = dto.ReturnedWhen;
        entity.IsOrderComplete = dto.IsOrderComplete;
        entity.Customer = customers[dto.CustomerId];
    }

    protected override GetOrderDto CreateGetDto(Order entity)
    {
        return new GetOrderDto
        {
            Id = entity.Id,
            Version = entity.Version,
            CreatedDateTime = entity.CreatedDateTime,
            UpdatedDateTime = entity.UpdatedDateTime,
            HandInWhat = entity.HandInWhat,
            RepairWhat = entity.RepairWhat,
            CustomerId = entity.CustomerId,
            BorrowedPhone = entity.BorrowedPhone,
            HandInWhen = entity.HandInWhen,
            ReturnedWhen = entity.ReturnedWhen,
            IsOrderComplete = entity.IsOrderComplete,
            Customer = new GetCustomerDto
            {
                Id = entity.Customer.Id,
                Version = entity.Customer.Version,
                CreatedDateTime = entity.Customer.CreatedDateTime,
                UpdatedDateTime = entity.Customer.UpdatedDateTime,
                Name = entity.Customer.Name,
                Phone = entity.Customer.Phone,
                Email = entity.Customer.Email,
            },
        };
    }

    protected override bool HasMatchingVersion(Order entity, UpdateOrderDto dto)
    {
        return entity.Version.SequenceEqual(dto.Version);
    }
}
