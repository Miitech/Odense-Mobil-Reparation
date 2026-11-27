using Repair.Models.Dto.Add;
using Repair.Models.Dto.Get;
using Repair.Models.Dto.Update;

namespace Repair.Tests.Shared.Factory;

internal static class DtoFactory
{
    public static AddCustomerDto CreateAddCustomer(string name, string phone, string email)
    {
        return new AddCustomerDto { Name = name, Phone = phone, Email = email, };
    }

    public static UpdateCustomerDto CreateUpdateCustomer(string name, string phone, string email, byte[] version)
    {
        return new UpdateCustomerDto { Name = name, Phone = phone, Email = email, Version = version, };
    }

    public static AddOrderDto CreateAddOrder(string handInWhat, string repairWhat, int customerId,
        string? borrowedPhone, DateTime handInWhen, DateTime? returnedWhen, bool isOrderComplete)
    {
        return new AddOrderDto
        {
            HandInWhat = handInWhat,
            RepairWhat = repairWhat,
            CustomerId = customerId,
            BorrowedPhone = borrowedPhone,
            HandInWhen = handInWhen,
            ReturnedWhen = returnedWhen,
            IsOrderComplete = isOrderComplete,
        };
    }

    public static UpdateOrderDto CreateUpdateOrder(string handInWhat, string repairWhat, int customerId,
        string? borrowedPhone, DateTime handInWhen, DateTime? returnedWhen, bool isOrderComplete, byte[] version)
    {
        return new UpdateOrderDto
        {
            HandInWhat = handInWhat,
            RepairWhat = repairWhat,
            CustomerId = customerId,
            BorrowedPhone = borrowedPhone,
            HandInWhen = handInWhen,
            ReturnedWhen = returnedWhen,
            IsOrderComplete = isOrderComplete,
            Version = version,
        };
    }

    public static GetCustomerDto CreateGetCustomer(int id, byte version, string name = "Customer")
    {
        return new GetCustomerDto { Id = id, Version = [version], Name = name, };
    }

    public static GetOrderDto CreateGetOrder(int id, byte version, GetCustomerDto customer)
    {
        return new GetOrderDto { Id = id, Version = [version], CustomerId = customer.Id, Customer = customer, };
    }
}
