namespace MZikmund.Web.Core.Services;

public interface IVersionInfoProvider
{
	string Version { get; }
	string? CommitSha { get; }
	string? CommitUrl { get; }
}
