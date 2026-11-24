using Repair.Abstractions.Entity.Data;
using Repair.Abstractions.Entity.Searchable;
using Repair.Abstractions.Persistence;

namespace Repair.Abstractions.Entity.Model;

public interface IOrder : ISearchableOrder, IEntity, IOrderData
{
    ICustomer Customer { get; set; }
}
