using Repair.Abstractions.Persistence;
using Repair.Api.Controllers.Core;
using Repair.Models.Dto.Add;
using Repair.Models.Dto.Get;
using Repair.Models.Dto.Update;
using Repair.Models.Entity.ComplexSearchable;
using Repair.Models.Entity.Model;
using Repair.Models.Entity.Searchable;

namespace Repair.Api.Controllers;

public class CustomerController : EntityController<
    Customer,
    SearchableCustomer,
    ComplexSearchableCustomer,
    AddCustomerDto,
    UpdateCustomerDto,
    GetCustomerDto,
    CustomerController>
{
    public CustomerController(
        IEntityQueryService<Customer, SearchableCustomer> queryService,
        ILogger<CustomerController> logger)
        : base(queryService, logger)
    {
    }

    protected override Customer CreateEntity(AddCustomerDto dto)
    {
        return new Customer
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
        };
    }

    protected override void UpdateEntity(Customer entity, UpdateCustomerDto dto)
    {
        entity.Name = dto.Name;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
    }

    protected override GetCustomerDto CreateGetDto(Customer entity)
    {
        return new GetCustomerDto
        {
            Id = entity.Id,
            Version = entity.Version,
            CreatedDateTime = entity.CreatedDateTime,
            UpdatedDateTime = entity.UpdatedDateTime,
            Name = entity.Name,
            Phone = entity.Phone,
            Email = entity.Email,
        };
    }

    protected override bool HasMatchingVersion(
        Customer entity, UpdateCustomerDto dto)
    {
        return entity.Version.SequenceEqual(dto.Version);
    }
}
