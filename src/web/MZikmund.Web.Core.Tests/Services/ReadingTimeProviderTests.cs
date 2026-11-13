using Microsoft.Extensions.Caching.Memory;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Core.Tests.Services;

public class ReadingTimeProviderTests
{
	private static readonly DateTimeOffset Modified = new(2026, 11, 13, 18, 0, 0, TimeSpan.Zero);

	private readonly Cache _cache = new(new MemoryCache(new MemoryCacheOptions()));

	private static string Words(int count) => string.Join(' ', Enumerable.Repeat("word", count));

	[Fact]
	public async Task EstimatesFromTheContent()
	{
		ReadingTimeProvider provider = new(_cache);

		Assert.Equal(3, await provider.GetMinutesAsync(Guid.NewGuid(), Modified, Words(500)));
	}

	[Fact]
	public async Task ReusesTheEstimateForTheSameVersionOfAPost()
	{
		ReadingTimeProvider provider = new(_cache);
		var postId = Guid.NewGuid();

		await provider.GetMinutesAsync(postId, Modified, Words(500));

		Assert.Equal(3, await provider.GetMinutesAsync(postId, Modified, Words(5000)));
	}

	[Fact]
	public async Task AnEditedPostIsEstimatedAgain()
	{
		ReadingTimeProvider provider = new(_cache);
		var postId = Guid.NewGuid();

		await provider.GetMinutesAsync(postId, Modified, Words(500));

		Assert.Equal(22, await provider.GetMinutesAsync(postId, Modified.AddMinutes(1), Words(5000)));
	}
}
