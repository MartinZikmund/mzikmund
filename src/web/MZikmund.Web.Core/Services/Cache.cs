using Microsoft.Extensions.Caching.Memory;

namespace MZikmund.Web.Core.Services;

public class Cache : ICache
{
	private readonly IMemoryCache _inMemoryCache;

	public Cache(IMemoryCache inMemoryCache)
	{
		_inMemoryCache = inMemoryCache;
	}

	public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
	{
		_inMemoryCache.TryGetValue(key, out T? value);
		return Task.FromResult(value);
	}

	public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
	{
		var cacheOptions = new MemoryCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = ttl
		};
		_inMemoryCache.Set(key, value, cacheOptions);
		return Task.CompletedTask;
	}

	public Task RemoveAsync(string key, CancellationToken ct = default)
	{
		_inMemoryCache.Remove(key);
		return Task.CompletedTask;
	}
}
