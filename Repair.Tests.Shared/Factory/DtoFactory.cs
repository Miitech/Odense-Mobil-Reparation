using Repair.Models.Dto.Add;
using Repair.Models.Dto.Update;

namespace Repair.Tests.Shared.Factory;

internal static class DtoFactory
{
    public static AddCustomerDto CreateAddCustomer(string name, string phone, string email)
    {
        return new AddCustomerDto
        {
            Name = name,
            Phone = phone,
            Email = email,
        };
    }

    public static UpdateCustomerDto CreateUpdateCustomer(string name, string phone, string email, byte[] version)
    {
        return new UpdateCustomerDto
        {
            Name = name,
            Phone = phone,
            Email = email,
            Version = version,
        };
    }
}
