namespace Repair.Models.Dto.Get;

public class GetCustomerDto
{
    public int Id { get; set; }

    public byte[] Version { get; set; } = [];

    public DateTime CreatedDateTime { get; set; }

    public DateTime UpdatedDateTime { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
