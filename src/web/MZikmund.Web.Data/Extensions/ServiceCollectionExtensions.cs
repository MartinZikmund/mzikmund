using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Data.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDataContext(this IServiceCollection services, string connectionString)
	{
		services.AddScoped(typeof(IRepository<>), typeof(DbContextRepository<>));

		services.AddDbContext<DatabaseContext>(options =>
			options.UseLazyLoadingProxies()
				.UseSqlServer(connectionString, builder =>
				{
					builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
				}).
				EnableDetailedErrors());

		return services;
	}
}
