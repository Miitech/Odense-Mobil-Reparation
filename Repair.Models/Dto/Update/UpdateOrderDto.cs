namespace Repair.Models.Dto.Update;

public class UpdateOrderDto
{
    public byte[] Version { get; set; } = [];

    public string HandInWhat { get; set; } = string.Empty;

    public string RepairWhat { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string? BorrowedPhone { get; set; }

    public DateTime HandInWhen { get; set; }

    public DateTime? ReturnedWhen { get; set; }

    public bool IsOrderComplete { get; set; }
}
