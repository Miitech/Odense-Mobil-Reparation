namespace Repair.Api.Exceptions;

public class EntityConcurrencyException(string message, Exception? innerException = null) : Exception(message, innerException)
{
}
