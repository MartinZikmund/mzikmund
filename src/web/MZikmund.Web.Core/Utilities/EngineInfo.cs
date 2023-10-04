using System.Reflection;

namespace MZikmund.Web.Core.Utilities;

public static class EngineInfo
{
	public static string Version
	{
		get
		{
			var entryAssembly = Assembly.GetEntryAssembly();

			if (entryAssembly is null)
			{
				return "N/A";
			}

			var fileVersion = entryAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

			var informationalVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
			if (!string.IsNullOrWhiteSpace(informationalVersion) && informationalVersion.IndexOf('+') > 0)
			{
				var gitHash = informationalVersion[(informationalVersion.IndexOf('+') + 1)..]; // e57ab0321ae44bd778c117646273a77123b6983f
				var prefix = informationalVersion[..informationalVersion.IndexOf('+')]; // 11.2-preview

				if (gitHash.Length <= 6) return informationalVersion;

				// consider valid hash
				var gitHashShort = gitHash[..6];
				return !string.IsNullOrWhiteSpace(gitHashShort) ? $"{prefix} ({gitHashShort})" : fileVersion ?? "";
			}

			return informationalVersion ?? fileVersion ?? "N/A";
		}
	}
}
