using Repair.Abstractions.Dto;
using Repair.Abstractions.Entity.Data;

namespace Repair.Models.Dto.Add;

public class AddCustomerDto : IAddDto, ICustomerData
{
    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Phone { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Email { get; set; } = string.Empty;
}
