namespace MZikmund.Web.Core.Infrastructure;

/// <summary>
/// Marks an <see cref="IAuthoredRequest"/> that a banned author may still perform. A ban blocks new
/// speech, not the right to take your own words down.
/// </summary>
public interface IAllowsBannedAuthor
{
}
