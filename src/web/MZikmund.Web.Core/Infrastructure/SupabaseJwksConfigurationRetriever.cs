using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace MZikmund.Web.Core.Infrastructure;

/// <summary>
/// Builds an <see cref="OpenIdConnectConfiguration"/> straight from a JWKS document, bypassing
/// OpenID discovery entirely.
/// </summary>
/// <remarks>
/// Supabase serves a reliable JWKS but an unreliable /.well-known/openid-configuration - several
/// deployments return an empty issuer and relative endpoint URLs. Driving the ConfigurationManager
/// off the JWKS gives one identical code path locally and in production, and keeps key rotation
/// working. It also makes the fetch lazy, so the site still serves anonymous pages with no
/// Supabase instance reachable.
/// </remarks>
public sealed class SupabaseJwksConfigurationRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
{
	private readonly string _issuer;

	public SupabaseJwksConfigurationRetriever(string issuer)
	{
		_issuer = issuer;
	}

	public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
		string address,
		IDocumentRetriever retriever,
		CancellationToken cancel)
	{
		var json = await retriever.GetDocumentAsync(address, cancel).ConfigureAwait(false);
		var keySet = JsonWebKeySet.Create(json);

		OpenIdConnectConfiguration configuration = new()
		{
			Issuer = _issuer,
			JwksUri = address,
			JsonWebKeySet = keySet
		};

		foreach (var key in keySet.GetSigningKeys())
		{
			configuration.SigningKeys.Add(key);
		}

		return configuration;
	}
}
