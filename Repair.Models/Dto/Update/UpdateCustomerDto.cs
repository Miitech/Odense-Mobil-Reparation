namespace Repair.Models.Dto.Update;

public class UpdateCustomerDto
{
    public byte[] Version { get; set; } = [];

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
