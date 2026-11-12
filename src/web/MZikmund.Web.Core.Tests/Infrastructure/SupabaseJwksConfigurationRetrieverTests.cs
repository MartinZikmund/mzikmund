using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using MZikmund.Web.Core.Infrastructure;

namespace MZikmund.Web.Core.Tests.Infrastructure;

public class SupabaseJwksConfigurationRetrieverTests
{
	private const string Issuer = "http://127.0.0.1:54321/auth/v1";

	private const string JwksAddress = "http://127.0.0.1:54321/auth/v1/.well-known/jwks.json";

	// A real P-256 public key. GetSigningKeys() builds an ECDsa from x/y, so the point must be valid.
	private const string Es256JwksDocument = """
		{
			"keys": [
				{
					"kty": "EC",
					"crv": "P-256",
					"alg": "ES256",
					"use": "sig",
					"kid": "b81269f1-21d8-4f2e-b719-c2240a840d90",
					"x": "YUTg-G6CG9X9nAfXkpbXRVWk9uw2XsqNSgHhrgVg-So",
					"y": "ngxRB4sQWupkrkbAEi5o1azsWAuIJmEMl5FYmbq_2dw"
				}
			]
		}
		""";

	[Fact]
	public async Task GetConfigurationAsync_WithAnEs256KeySet_ProjectsIssuerJwksUriAndSigningKeys()
	{
		// Arrange
		SupabaseJwksConfigurationRetriever retriever = new(Issuer);
		FakeDocumentRetriever documentRetriever = new(Es256JwksDocument);

		// Act
		var configuration = await retriever.GetConfigurationAsync(
			JwksAddress,
			documentRetriever,
			CancellationToken.None);

		// Assert
		Assert.Equal(Issuer, configuration.Issuer);
		Assert.Equal(JwksAddress, configuration.JwksUri);
		Assert.Single(configuration.SigningKeys);
		Assert.Equal("b81269f1-21d8-4f2e-b719-c2240a840d90", configuration.SigningKeys.Single().KeyId);
		Assert.Equal(JwksAddress, documentRetriever.RequestedAddress);
	}

	[Fact]
	public async Task GetConfigurationAsync_WithAnEmptyKeySet_ReturnsNoSigningKeys()
	{
		// Arrange
		SupabaseJwksConfigurationRetriever retriever = new(Issuer);
		FakeDocumentRetriever documentRetriever = new("""{ "keys": [] }""");

		// Act
		var configuration = await retriever.GetConfigurationAsync(
			JwksAddress,
			documentRetriever,
			CancellationToken.None);

		// Assert
		Assert.Empty(configuration.SigningKeys);
		Assert.Equal(Issuer, configuration.Issuer);
	}

	private sealed class FakeDocumentRetriever : IDocumentRetriever
	{
		private readonly string _document;

		public FakeDocumentRetriever(string document)
		{
			_document = document;
		}

		public string? RequestedAddress { get; private set; }

		public Task<string> GetDocumentAsync(string address, CancellationToken cancel)
		{
			RequestedAddress = address;
			return Task.FromResult(_document);
		}
	}
}
