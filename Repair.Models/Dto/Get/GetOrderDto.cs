namespace Repair.Models.Dto.Get;

public class GetOrderDto
{
    public int Id { get; set; }

    public byte[] Version { get; set; } = [];

    public DateTime CreatedDateTime { get; set; }

    public DateTime UpdatedDateTime { get; set; }

    public string HandInWhat { get; set; } = string.Empty;

    public string RepairWhat { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string? BorrowedPhone { get; set; }

    public DateTime HandInWhen { get; set; }

    public DateTime? ReturnedWhen { get; set; }

    public bool IsOrderComplete { get; set; }

    public GetCustomerDto Customer { get; set; } = null!;
}
