namespace MZikmund.Web.Core.Infrastructure;

/// <summary>
/// Marks a MediatR request that is performed by an authenticated comment author.
/// <see cref="AuthoredRequestBehavior{TRequest, TResponse}"/> upserts the author row and rejects
/// banned authors before the handler runs, so no handler repeats either concern.
/// </summary>
public interface IAuthoredRequest
{
}
