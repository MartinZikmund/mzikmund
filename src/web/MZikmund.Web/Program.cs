using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MZikmund.Web.Configuration;
using MZikmund.Web.Configuration.Connections;
using MZikmund.Web.Core.Content.Meta;
using MZikmund.Web.Core.Features.Videos.RssParsing;
using MZikmund.Web.Core.Infrastructure;
using MZikmund.Web.Core.Middleware;
using MZikmund.Web.Core.Properties;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Core.Syndication;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Extensions;
using MZikmund.Web.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

await Configure(app);

void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
	var siteConfiguration = new SiteConfiguration(configuration);
	services.AddSingleton<ISiteConfiguration>(siteConfiguration);

	// Configure YouTube options with validation
	var youtubeConfig = configuration.GetSection("YouTube");
	services.Configure<YouTubeOptions>(youtubeConfig);
	ValidateYouTubeOptions(youtubeConfig);

	services.Configure<CommentsOptions>(configuration.GetSection("Comments"));
	services.Configure<SupabaseOptions>(configuration.GetSection("Supabase"));
	services.Configure<TurnstileOptions>(configuration.GetSection("Turnstile"));

	services.AddAutoMapper(c => c.AddMaps(typeof(CoreAssemblyMarker)));
	services.AddSingleton<ICache, Cache>();
	services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
	services.AddSingleton<IMarkdownConverter, MarkdownConverter>();
	services.AddSingleton<IPostContentProcessor, PostContentProcessor>();
	services.AddSingleton<IBlobUrlProvider, BlobUrlProvider>();
	services.AddSingleton<IFeedGenerator, FeedGenerator>();
	services.AddScoped<ISyndicationDataSource, SyndicationDataSource>();
	services.AddHttpClient("YouTube");
	services.AddScoped<YouTubeRssFeedParser>();
	services.AddHttpContextAccessor();
	services.AddScoped<MetaTagsInfo>();
	services.AddCors(options =>
	{
		options.AddPolicy("WasmAppPolicy",
			policy =>
			{
				policy.WithOrigins(
					siteConfiguration.General.WasmAppUrl.AbsoluteUri.TrimEnd('/'),
					"https://mzikmund.app")
					.AllowAnyMethod()
					.AllowAnyHeader()
					.AllowCredentials();
			});
	});
	services.Configure<RouteOptions>(option =>
	{
		option.LowercaseUrls = true;
		option.LowercaseQueryStrings = true;
		option.AppendTrailingSlash = false;
	});

	// --- Supabase authentication ---------------------------------------------------------------
	// ONE registration for every environment. The local Supabase CLI stack (v2.71.1 and newer)
	// signs access tokens with ES256 and publishes the key at /auth/v1/.well-known/jwks.json,
	// exactly like the hosted project - so there is no HS256 branch and no second code path.
	// The only thing that differs is the base URL, which comes from configuration:
	//     Production  -> Supabase:ProjectRef => https://<ref>.supabase.co
	//     Development -> Supabase:LocalUrl   => http://127.0.0.1:54321
	// plus the plain-HTTP exemption that a loopback URL needs for metadata retrieval.
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
				ValidAudience = "authenticated",
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,

				// SECURITY: pin the asymmetric algorithms. The local stack also ships a publicly
				// known HS256 secret (it signs the legacy anon/service_role API keys). Without
				// this pin, anyone could mint an admin token with that public secret.
				ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256],

				NameClaimType = "sub",
				RoleClaimType = SupabaseClaims.Role,
				ClockSkew = TimeSpan.FromSeconds(30)
			};
		});

	// app_metadata arrives as ONE claim whose value is raw JSON, so the nested role has to be
	// projected before any policy can read it.
	services.AddSingleton<IClaimsTransformation, SupabaseAppMetadataClaimsTransformation>();

	services.AddAuthorization(options =>
	{
		options.AddPolicy("AdminPolicy", policy =>
		{
			policy.RequireAuthenticatedUser();
			policy.RequireClaim(SupabaseClaims.Role, "admin");
		});
	});

	services.AddLocalization(options => options.ResourcesPath = "Resources");

	services.AddControllers()
			.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

	services.AddEndpointsApiExplorer();
	services.AddSwaggerGen();

	// Only if APPLICATIONINSIGHTS_CONNECTION_STRING is set
	if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
	{
		services.AddApplicationInsightsTelemetry();
	}

	services.AddHealthChecks();
	services.AddRazorPages();
	services.AddMediatR(config =>
	{
		config.RegisterServicesFromAssemblyContaining<CoreAssemblyMarker>();
	});

	services.AddSingleton<IDateProvider, DateProvider>();
	services.AddSingleton<IVersionInfoProvider, VersionInfoProvider>();
	services.AddSingleton<IBlobPathGenerator, BlobPathGenerator>();
	services.AddSingleton<IBlobStorage, BlobStorage>();

	services.AddDataContext();

}

async Task Configure(WebApplication app)
{
	app.UseRewriter(new RewriteOptions()
		.Add(new LegacyUrlRedirectRule())
		.AddRedirectToNonWwwPermanent());

	await app.Services.GetRequiredService<IBlobStorage>().InitializeAsync();

	// Todo: Improve this
	using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
	{
		var context = serviceScope.ServiceProvider.GetRequiredService<DatabaseContext>();
		context.Database.Migrate();
	}

	// Configure the HTTP request pipeline.
	if (!app.Environment.IsDevelopment())
	{
		app.UseExceptionHandler("/Error");
		// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
		app.UseHsts();
	}
	else
	{
		app.UseDeveloperExceptionPage();
		app.UseSwagger();
		app.UseSwaggerUI();
	}

	app.UseHttpsRedirection();
	app.UseStaticFiles();

	app.UseMiddleware<ReallySimpleDiscoveryMiddleware>();

	app.UseRouting();
	app.UseCors("WasmAppPolicy");

	app.UseAuthentication();
	app.UseAuthorization();

	app.MapHealthChecks("/health");

	app.MapControllers();
	app.MapRazorPages();

	app.Run();
}

void ValidateYouTubeOptions(IConfigurationSection section)
{
	var options = section.Get<YouTubeOptions>();

	if (string.IsNullOrEmpty(options?.FeedUrl) ||
		!options.FeedUrl.StartsWith("https://www.youtube.com/feeds/videos.xml"))
	{
		throw new InvalidOperationException(
			"YouTube:FeedUrl not configured correctly. Set in appsettings.json: " +
			"YouTube:FeedUrl=https://www.youtube.com/feeds/videos.xml?channel_id=YOUR_CHANNEL_ID");
	}

	if (string.IsNullOrEmpty(options.ChannelUrl) ||
		!Uri.TryCreate(options.ChannelUrl, UriKind.Absolute, out var channelUri) ||
		channelUri.Scheme != "https")
	{
		throw new InvalidOperationException(
			"YouTube:ChannelUrl not configured correctly. Set in appsettings.json: " +
			"YouTube:ChannelUrl=https://www.youtube.com/@YOUR_HANDLE");
	}

	if (options.CacheTtlMinutes < 30)
	{
		throw new InvalidOperationException(
			"YouTube:CacheTtlMinutes must be at least 30. " +
			"Set in appsettings.json: YouTube:CacheTtlMinutes=30");
	}
}
