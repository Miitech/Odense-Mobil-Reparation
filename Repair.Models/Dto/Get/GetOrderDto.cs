using Repair.Abstractions.Dto;
using Repair.Abstractions.Entity.Data;

namespace Repair.Models.Dto.Get;

public class GetOrderDto : IGetDto, IOrderData
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
    public string HandInWhat { get; set; } = string.Empty;

    /// <inheritdoc />
    public string RepairWhat { get; set; } = string.Empty;

    /// <inheritdoc />
    public int CustomerId { get; set; }

    /// <inheritdoc />
    public string? BorrowedDevice { get; set; }

    /// <inheritdoc />
    public DateTime HandInWhen { get; set; }

    /// <inheritdoc />
    public DateTime? ReturnedWhen { get; set; }

    /// <inheritdoc />
    public bool IsOrderComplete { get; set; }

    public GetCustomerDto Customer { get; set; } = null!;
}
