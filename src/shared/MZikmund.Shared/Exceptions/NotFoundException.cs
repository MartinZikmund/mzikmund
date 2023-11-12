namespace MZikmund.Shared.Exceptions;

public class NotFoundException : Exception
{
	public NotFoundException(string entityType)
		: base($"Item {entityType} was not found.")
	{
	}

	public NotFoundException(string entityType, object id)
		: base($"Item {entityType} with ID {id} was not found.")
	{
	}
}
