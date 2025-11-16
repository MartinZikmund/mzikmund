using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MZikmund.HeroImageGenerator.Services;
using MZikmund.Web.Configuration;
using MZikmund.Web.Configuration.Connections;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Data;

namespace MZikmund.HeroImageGenerator;

sealed class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting Hero Image Generator Tool");

            var generatorService = services.GetRequiredService<HeroImageGeneratorService>();
            await generatorService.ProcessPostsAsync();

            logger.LogInformation("Hero Image Generator Tool completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Register configuration options
                services.Configure<OpenAIOptions>(configuration.GetSection("OpenAI"));
                services.AddSingleton<ISiteConfiguration>(sp =>
                {
                    return new SiteConfiguration(configuration);
                });

                // Register connection string provider
                services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();

                // Register database context
                var connectionStringProvider = services.BuildServiceProvider().GetRequiredService<IConnectionStringProvider>();
                services.AddDbContext<DatabaseContext>(options =>
                {
                    options.UseSqlServer(connectionStringProvider.Database);
                });

                // Register services
                services.AddSingleton<IImageGenerationService, OpenAIImageGenerationService>();
                services.AddSingleton<IBlobStorage, BlobStorage>();
                services.AddScoped<HeroImageGeneratorService>();
            });
}
