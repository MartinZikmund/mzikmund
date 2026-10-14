using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Infrastructure;

namespace MZikmund.Web.Infrastructure.Auth;

/// <summary>
/// Registers Supabase as the sole identity provider for the API.
/// </summary>
public static class SupabaseAuthenticationExtensions
{
	/// <summary>
	/// Name of the authorization policy guarding every admin endpoint.
	/// </summary>
	public const string AdminPolicyName = "AdminPolicy";

	/// <summary>
	/// Adds Supabase JWT bearer authentication, the app_metadata claims transformation
	/// and the admin authorization policy.
	/// </summary>
	/// <param name="services">Service collection.</param>
	/// <param name="configuration">Application configuration containing the "Supabase" section.</param>
	/// <returns>The same service collection, for chaining.</returns>
	public static IServiceCollection AddSupabaseAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		// ONE registration for every environment. The local Supabase CLI stack (v2.71.1 and newer)
		// signs access tokens with ES256 and publishes the key at /auth/v1/.well-known/jwks.json,
		// exactly like the hosted project - so there is no HS256 branch and no second code path.
		// The only thing that differs is the base URL, which comes from configuration:
		//     Production  -> Supabase:ProjectRef => https://<ref>.supabase.co
		//     Development -> Supabase:LocalUrl   => http://127.0.0.1:54321
		// The options are bound in Program.cs, not here.
		var supabaseOptions = configuration.GetSection("Supabase").Get<SupabaseOptions>() ?? new SupabaseOptions();

		// Derived from the URL rather than from IWebHostEnvironment, so a stray LocalUrl in production
		// cannot silently disable TLS for a public host.
		var supabaseIsLoopbackHttp =
			Uri.TryCreate(supabaseOptions.BaseUrl, UriKind.Absolute, out var supabaseUri) &&
			supabaseUri.Scheme == Uri.UriSchemeHttp &&
			supabaseUri.IsLoopback;

		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.RequireHttpsMetadata = !supabaseIsLoopbackHttp;

				// Deliberately NOT options.Authority. That fetches /.well-known/openid-configuration,
				// which Supabase serves with an empty issuer and relative endpoints in several
				// deployments. Supplying a ConfigurationManager also keeps the JWKS fetch LAZY, so
				// `dotnet run` with no Supabase reachable still serves every anonymous page and only
				// authenticated requests fail. That property is intentional - do not validate the
				// Supabase configuration at startup.
				options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
					supabaseOptions.JwksUri,
					new SupabaseJwksConfigurationRetriever(supabaseOptions.Issuer),
					new HttpDocumentRetriever { RequireHttps = !supabaseIsLoopbackHttp });

				// Recover automatically when Supabase rotates its signing key.
				options.RefreshOnIssuerKeyNotFound = true;

				// Keep the raw Supabase claim names. With the default mapping on, "sub" becomes
				// ClaimTypes.NameIdentifier and Supabase's Postgres "role" claim ("authenticated")
				// becomes a genuine ASP.NET role for every signed-in user.
				options.MapInboundClaims = false;

				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidIssuer = supabaseOptions.Issuer,
					ValidateAudience = true,
					ValidAudience = SupabaseClaims.Audience,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,

					// SECURITY: pin the asymmetric algorithms. The local stack also ships a publicly
					// known HS256 secret (it signs the legacy anon/service_role API keys). Without
					// this pin, anyone could mint an admin token with that public secret.
					ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256],

					NameClaimType = SupabaseClaims.Subject,
					RoleClaimType = SupabaseClaims.Role,
					ClockSkew = TimeSpan.FromSeconds(30)
				};
			});

		// app_metadata arrives as ONE claim whose value is raw JSON, so the nested role has to be
		// projected before any policy can read it. The only IClaimsTransformation in the solution.
		services.AddSingleton<IClaimsTransformation, SupabaseAppMetadataClaimsTransformation>();

		services.AddAuthorization(options =>
		{
			options.AddPolicy(AdminPolicyName, policy =>
			{
				policy.RequireAuthenticatedUser();
				policy.RequireClaim(SupabaseClaims.Role, SupabaseClaims.AdminRole);
			});
		});

		return services;
	}
}
