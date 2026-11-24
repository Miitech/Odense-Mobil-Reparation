using Repair.Abstractions.Dto;
using Repair.Abstractions.Entity.Data;

namespace Repair.Models.Dto.Get;

public class GetCustomerDto : IGetDto, ICustomerData
{
    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public byte[] Version { get; set; } = [];

    /// <inheritdoc />
    public DateTime CreatedDateTime { get; set; }

    /// <inheritdoc />
    public DateTime UpdatedDateTime { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Phone { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Email { get; set; } = string.Empty;
}
