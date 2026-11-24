namespace Repair.Models.Dto.Add;

public class AddOrderDto
{
    public string HandInWhat { get; set; } = string.Empty;

    public string RepairWhat { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string? BorrowedPhone { get; set; }

    public DateTime HandInWhen { get; set; }

    public DateTime? ReturnedWhen { get; set; }

    public bool IsOrderComplete { get; set; }
}
