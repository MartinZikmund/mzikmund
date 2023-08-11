using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace MZikmund.Web.Core.Services;

public class Cache : ICache
{
	private readonly IMemoryCache _inMemoryCache;

	public Cache(IMemoryCache inMemoryCache)
	{
		_inMemoryCache = inMemoryCache;
	}
}
