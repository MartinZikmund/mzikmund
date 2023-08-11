using Microsoft.Extensions.Caching.Memory;

namespace MZikmund.Web.Core.Services;

internal class Cache
{
	private readonly IMemoryCache _inMemoryCache;

	public Cache(IMemoryCache inMemoryCache)
	{
		_inMemoryCache = inMemoryCache;
	}
}
