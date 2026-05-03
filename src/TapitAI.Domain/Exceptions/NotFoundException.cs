namespace TapitAI.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"'{entityName}' ({key}) was not found.") { }
}
