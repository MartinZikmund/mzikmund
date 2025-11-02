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
	services.AddAutoMapper(typeof(CoreAssemblyMarker).Assembly);
	services.AddSingleton<ICache, Cache>();
	services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
	services.AddSingleton<IMarkdownConverter, MarkdownConverter>();
	services.AddSingleton<IPostContentProcessor, PostContentProcessor>();
	services.AddSingleton<IFeedGenerator, FeedGenerator>();
	services.AddScoped<ISyndicationDataSource, SyndicationDataSource>();
	services.AddHttpContextAccessor();
	services.AddScoped<MetaTagsInfo>();
	services.AddCors(options =>
	{
		options.AddDefaultPolicy(
			policy =>
			{
				policy.WithOrigins(siteConfiguration.General.WasmAppUrl.AbsoluteUri).AllowAnyMethod().AllowAnyHeader();
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
			options.Authority = $"https://{auth0Config["Domain"]}";
			options.Audience = auth0Config["Audience"];
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true
			};
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
		if (!await context.Database.CanConnectAsync())
		{
			context.Database.Migrate();
		}
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
	app.UseCors();
	app.MapHealthChecks("/health");

	app.UseAuthentication();
	app.UseAuthorization();

	app.MapControllers();
	app.MapRazorPages();

	app.Run();
}
