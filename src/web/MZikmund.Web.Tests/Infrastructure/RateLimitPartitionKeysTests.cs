using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MZikmund.Web.Infrastructure;

namespace MZikmund.Web.Tests.Infrastructure;

public class RateLimitPartitionKeysTests
{
	[Fact]
	public void ForRequest_WithNameIdentifierClaim_ReturnsUserPrefixedKey()
	{
		// Arrange
		DefaultHttpContext httpContext = new();
		httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
			new[] { new Claim(ClaimTypes.NameIdentifier, "9e1f0f7c-2f4a-4d0c-8f1a-1c9d6a2b3c4d") },
			"TestAuth"));

		// Act
		var key = RateLimitPartitionKeys.ForRequest(httpContext);

		// Assert
		Assert.Equal("user:9e1f0f7c-2f4a-4d0c-8f1a-1c9d6a2b3c4d", key);
	}

	[Fact]
	public void ForRequest_WithRawSubClaimOnly_ReturnsUserPrefixedKey()
	{
		// Arrange
		DefaultHttpContext httpContext = new();
		httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "abc-123") }, "TestAuth"));

		// Act
		var key = RateLimitPartitionKeys.ForRequest(httpContext);

		// Assert
		Assert.Equal("user:abc-123", key);
	}

	[Fact]
	public void ForRequest_WhenAnonymous_FallsBackToIpPrefixedKey()
	{
		// Arrange
		DefaultHttpContext httpContext = new();
		httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");

		// Act
		var key = RateLimitPartitionKeys.ForRequest(httpContext);

		// Assert
		Assert.Equal("ip:203.0.113.9", key);
	}

	[Fact]
	public void ForClientIp_WithoutRemoteIpAddress_ReturnsUnknownIpKey()
	{
		// Arrange
		DefaultHttpContext httpContext = new();

		// Act
		var key = RateLimitPartitionKeys.ForClientIp(httpContext);

		// Assert
		Assert.Equal("ip:unknown", key);
	}
}
