namespace Repair.Api.Exceptions;

public class EntityNotFoundException(Type entityType, int id) : Exception($"{entityType.Name} with ID {id} was not found.")
{
}
