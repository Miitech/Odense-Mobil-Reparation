using Repair.Abstractions.Dto;
using Repair.Abstractions.Entity.Data;

namespace Repair.Models.Dto.Update;

public class UpdateCustomerDto : IUpdateDto, ICustomerData
{
    /// <inheritdoc />
    public byte[] Version { get; set; } = [];

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Phone { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Email { get; set; } = string.Empty;
}
