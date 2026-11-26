using System.Text.Json;
using System.Text.Json.Serialization;
using Repair.Models.Dto.Add;
using Repair.Models.Dto.Get;
using Repair.Models.Dto.Update;
using Repair.Models.Entity.ComplexSearchable;
using Repair.Models.Entity.Searchable;

namespace Repair.Models.Serialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, IncludeFields = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(SearchableCustomer))]
[JsonSerializable(typeof(ComplexSearchableCustomer))]
[JsonSerializable(typeof(AddCustomerDto))]
[JsonSerializable(typeof(UpdateCustomerDto))]
[JsonSerializable(typeof(GetCustomerDto))]
[JsonSerializable(typeof(IEnumerable<AddCustomerDto>))]
[JsonSerializable(typeof(IEnumerable<GetCustomerDto>))]
[JsonSerializable(typeof(SearchableOrder))]
[JsonSerializable(typeof(ComplexSearchableOrder))]
[JsonSerializable(typeof(AddOrderDto))]
[JsonSerializable(typeof(UpdateOrderDto))]
[JsonSerializable(typeof(GetOrderDto))]
[JsonSerializable(typeof(IEnumerable<AddOrderDto>))]
[JsonSerializable(typeof(IEnumerable<GetOrderDto>))]
public partial class ApiJsonSerializerContext : JsonSerializerContext
{
}
