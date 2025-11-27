using System.Reflection;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Core.Tests.Infrastructure;

public class VersioningTests
{
	[Fact]
	public void AssemblyVersion_IsSet()
	{
		// Arrange & Act
		var assembly = typeof(IDateProvider).Assembly;
		var version = assembly.GetName().Version;

		// Assert
		Assert.NotNull(version);
		Assert.True(version.Major >= 1, "Major version should be at least 1");
		Assert.True(version.Minor >= 0, "Minor version should be at least 0");
	}

	[Fact]
	public void AssemblyInformationalVersion_IsSet()
	{
		// Arrange & Act
		var assembly = typeof(IDateProvider).Assembly;
		var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

		// Assert
		Assert.NotNull(informationalVersion);
		Assert.NotEmpty(informationalVersion);
		// Version should contain either a commit hash or be in semver format
		Assert.True(
			informationalVersion.Contains("+") || informationalVersion.Contains("-") || char.IsDigit(informationalVersion[0]),
			$"Informational version '{informationalVersion}' should be in valid format"
		);
	}

	[Fact]
	public void AssemblyFileVersion_IsSet()
	{
		// Arrange & Act
		var assembly = typeof(IDateProvider).Assembly;
		var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

		// Assert
		Assert.NotNull(fileVersion);
		Assert.NotEmpty(fileVersion);
	}
}
