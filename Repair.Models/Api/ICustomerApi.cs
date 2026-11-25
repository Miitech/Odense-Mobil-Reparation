using Repair.Abstractions.Api;
using Repair.Models.Dto.Add;
using Repair.Models.Dto.Get;
using Repair.Models.Dto.Update;
using Repair.Models.Entity.ComplexSearchable;
using Repair.Models.Entity.Searchable;

namespace Repair.Models.Api;

public interface ICustomerApi : IEntityApi<
    SearchableCustomer,
    ComplexSearchableCustomer,
    AddCustomerDto,
    UpdateCustomerDto,
    GetCustomerDto>
{
}
