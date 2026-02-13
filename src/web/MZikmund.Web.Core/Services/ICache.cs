namespace MZikmund.Web.Core.Services;

public interface ICache
{
	Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
	Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
	Task RemoveAsync(string key, CancellationToken ct = default);
}
