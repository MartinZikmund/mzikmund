using System.Reflection;

namespace MZikmund.Web.Core.Services;

public class VersionInfoProvider : IVersionInfoProvider
{
	private const string RepositoryUrl = "https://github.com/MartinZikmund/mzikmund";
	private const string DefaultVersion = "1.0.0";
	
	public string Version { get; }
	public string? CommitSha { get; }
	public string? CommitUrl { get; }

	public VersionInfoProvider()
	{
		var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
		var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		
		if (!string.IsNullOrEmpty(informationalVersion))
		{
			// InformationalVersion format: "1.0.0+<commit-sha>"
			var parts = informationalVersion.Split('+', 2);
			Version = parts[0];
			
			if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) && parts[1] != "local-dev")
			{
				CommitSha = parts[1];
				CommitUrl = $"{RepositoryUrl}/commit/{CommitSha}";
			}
		}
		else
		{
			Version = DefaultVersion;
		}
	}
}
