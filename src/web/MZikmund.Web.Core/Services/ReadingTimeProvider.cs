using MZikmund.Shared.Blog;

namespace MZikmund.Web.Core.Services;

/// <summary>
/// Caches reading time estimates per post version: estimating parses the whole post, which costs
/// milliseconds and megabytes for a long one, and lists would otherwise pay that on every request.
/// </summary>
public sealed class ReadingTimeProvider
{
	private static readonly TimeSpan Lifetime = TimeSpan.FromDays(1);

	private readonly ICache _cache;

	public ReadingTimeProvider(ICache cache) => _cache = cache;

	public async Task<int> GetMinutesAsync(Guid postId, DateTimeOffset? lastModified, string? content, CancellationToken cancellationToken = default)
	{
		// The modification time is part of the key, so an edited post gets a fresh estimate.
		var key = $"reading-time:{postId:N}:{lastModified?.UtcTicks ?? 0}";
		if (await _cache.GetAsync<int?>(key, cancellationToken) is { } cached)
		{
			return cached;
		}

		var minutes = ReadingTime.EstimateMinutes(content);
		await _cache.SetAsync<int?>(key, minutes, Lifetime, cancellationToken);
		return minutes;
	}
}
