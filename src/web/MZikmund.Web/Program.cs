using AutoMapper;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Configuration.Connections;
using MZikmund.Web.Core;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Extensions;
using MZikmund.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMediatR(config =>
{
	config.RegisterServicesFromAssemblyContaining<CoreAssemblyMarker>();
});

ConfigureServices(builder.Services);

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirectToNonWwwPermanent());

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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();


void ConfigureServices(IServiceCollection services)
{
	services.AddAutoMapper(typeof(CoreAssemblyMarker).Assembly);
	services.AddSingleton<ICache, Cache>();
	services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
	services.AddSingleton<IMarkdownConverter, MarkdownConverter>();
	services.AddSingleton<IPostContentProcessor, PostContentProcessor>();

	services.Configure<RouteOptions>(option =>
	{
		option.LowercaseUrls = true;
		option.LowercaseQueryStrings = true;
	});

	services.AddLocalization(options => options.ResourcesPath = "Resources");

	services.AddDataContext();
}
