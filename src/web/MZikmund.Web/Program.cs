using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MZikmund.Web.Configuration;
using MZikmund.Web.Configuration.Connections;
using MZikmund.Web.Core.Content.Meta;
using MZikmund.Web.Core.Infrastructure;
using MZikmund.Web.Core.Middleware;
using MZikmund.Web.Core.Properties;
using MZikmund.Web.Core.Features.Videos.RssParsing;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Core.Syndication;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Extensions;

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

	var youtubeOptions = youtubeConfig.Get<YouTubeOptions>();
	if (string.IsNullOrEmpty(youtubeOptions?.FeedUrl) ||
		!youtubeOptions.FeedUrl.StartsWith("https://www.youtube.com/feeds/videos.xml"))
	{
		throw new InvalidOperationException(
			"YouTube:FeedUrl not configured correctly. Set in appsettings.json: " +
			"YouTube:FeedUrl=https://www.youtube.com/feeds/videos.xml?channel_id=YOUR_CHANNEL_ID");
	}
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

	services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
		.AddJwtBearer(options =>
		{
			var auth0Config = configuration.GetSection("Auth0");
			var domain = auth0Config["Domain"];
			var audience = auth0Config["Audience"];

			// Validate that Auth0 configuration is not using placeholder values
			if (string.IsNullOrEmpty(domain) || domain.Contains("your-auth0-domain") ||
				string.IsNullOrEmpty(audience) || audience.Contains("your-api-identifier"))
			{
				throw new InvalidOperationException(
					"Auth0 configuration is not set or contains placeholder values. " +
					"Please configure Auth0:Domain and Auth0:Audience in appsettings.json, " +
					"User Secrets, or environment variables. See docs/AUTH0_MIGRATION.md for details.");
			}

			options.Authority = $"https://{domain}";
			options.Audience = audience;
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true
			};
		});

	// Add authorization policies
	services.AddAuthorization(options =>
	{
		options.AddPolicy("AdminPolicy", policy =>
		{
			policy.RequireAuthenticatedUser();
			// Check for "admin:all" permission in the permissions claim
			policy.RequireClaim("permissions", "admin:all");
		});
	});

	services.AddLocalization(options => options.ResourcesPath = "Resources");

	services.AddControllers()
			.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

	services.AddEndpointsApiExplorer();
	services.AddSwaggerGen();

	services.AddApplicationInsightsTelemetry();
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
