using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MZikmund.Web.Configuration.Connections;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Data.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDataContext(this IServiceCollection services)
	{
		services.AddScoped(typeof(IRepository<>), typeof(DbContextRepository<>));

		services.AddDbContext<DatabaseContext>((provider, options) =>
		{
			var connectionStringProvider = provider.GetRequiredService<IConnectionStringProvider>();
			options.UseLazyLoadingProxies()
				.UseSqlServer(connectionStringProvider.Database, builder =>
				{
					builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
				}).
				EnableDetailedErrors();
		});

		return services;
	}
}
