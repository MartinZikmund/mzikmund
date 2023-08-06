using AutoMapper;
using MZikmund.Web.Configuration.Connections;
using MZikmund.Web.Core;
using MZikmund.Web.Data.Extensions;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMediatR(config =>
{
	config.RegisterServicesFromAssemblyContaining<CoreAssemblyMarker>();
});

ConfigureServices(builder.Services);

var app = builder.Build();

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
	services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
	services.AddDataContext();
}
