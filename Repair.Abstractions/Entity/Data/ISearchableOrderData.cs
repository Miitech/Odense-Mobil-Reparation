namespace Repair.Abstractions.Entity.Data;

public interface ISearchableOrderData
{
    string HandInWhat { get; set; }

    string RepairWhat { get; set; }

    int CustomerId { get; set; }

    string? BorrowedDevice { get; set; }
}
