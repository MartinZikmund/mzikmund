using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Configuration;
using MZikmund.Web.Configuration.Connections;
using MZikmund.Web.Core.Content.Meta;
using MZikmund.Web.Core.Features.Videos.RssParsing;
using MZikmund.Web.Core.Infrastructure;
using MZikmund.Web.Core.Middleware;
using MZikmund.Web.Core.Properties;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Syndication;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Extensions;
using MZikmund.Web.Infrastructure.Auth;
using MZikmund.Web.Services;

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
	services.AddSingleton<ICommentContentRenderer, CommentContentRenderer>();
	services.AddSingleton<IBlobUrlProvider, BlobUrlProvider>();
	services.AddSingleton<IFeedGenerator, FeedGenerator>();
	services.AddScoped<ISyndicationDataSource, SyndicationDataSource>();
	services.AddHttpClient("YouTube");
	services.AddHttpClient(TurnstileVerifier.HttpClientName, client =>
	{
		client.Timeout = TimeSpan.FromSeconds(10);
	});
	services.AddSingleton<ITurnstileVerifier, TurnstileVerifier>();
	services.AddScoped<YouTubeRssFeedParser>();
	services.AddHttpContextAccessor();
	services.AddScoped<MetaTagsInfo>();
	services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
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

	services.AddSupabaseAuthentication(configuration);

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

		// Upserts the comment author and rejects banned authors for every IAuthoredRequest.
		// The DI container honours the generic constraint, so unmarked requests are unaffected.
		config.AddOpenBehavior(typeof(AuthoredRequestBehavior<,>));
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
