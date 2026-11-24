namespace Repair.Abstractions.Entity.Data;

public interface IOrderData : ISearchableOrderData
{
    DateTime HandInWhen { get; set; }

    DateTime? ReturnedWhen { get; set; }

    bool IsOrderComplete { get; set; }
}
