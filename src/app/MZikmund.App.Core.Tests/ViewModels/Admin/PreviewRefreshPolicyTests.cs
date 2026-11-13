using MZikmund.ViewModels.Admin;

namespace MZikmund.App.Core.Tests.ViewModels.Admin;

public class PreviewRefreshPolicyTests
{
	private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(300);
	private static readonly TimeSpan MaxDelay = TimeSpan.FromMilliseconds(1500);
	private static readonly DateTimeOffset Start = new(2026, 11, 13, 16, 0, 0, TimeSpan.Zero);

	private readonly PreviewRefreshPolicy _policy = new(Quiet, MaxDelay);

	private static DateTimeOffset At(int milliseconds) => Start.AddMilliseconds(milliseconds);

	[Fact]
	public void NothingRendersWithoutAnEdit() =>
		Assert.False(_policy.TryBeginRender(At(10_000)));

	[Fact]
	public void RendersOnceTypingPauses()
	{
		_policy.NotifyEdit(At(0));

		Assert.False(_policy.TryBeginRender(At(299)));
		Assert.True(_policy.TryBeginRender(At(300)));
		Assert.False(_policy.TryBeginRender(At(301)));
	}

	[Fact]
	public void EachKeystrokeRestartsTheQuietPeriod()
	{
		_policy.NotifyEdit(At(0));
		_policy.NotifyEdit(At(200));

		Assert.False(_policy.TryBeginRender(At(400)));
		Assert.True(_policy.TryBeginRender(At(500)));
	}

	[Fact]
	public void ContinuousTypingStillRendersWithinTheMaxDelay()
	{
		var rendersAt = new List<int>();
		for (var ms = 0; ms <= 4000; ms += 100)
		{
			_policy.NotifyEdit(At(ms));
			if (_policy.TryBeginRender(At(ms)))
			{
				rendersAt.Add(ms);
			}
		}

		Assert.Equal([1500, 3100], rendersAt);
	}

	[Fact]
	public void AnEditDuringRenderingIsNotLost()
	{
		_policy.NotifyEdit(At(0));
		Assert.True(_policy.TryBeginRender(At(300)));

		_policy.NotifyEdit(At(350));

		Assert.True(_policy.TryBeginRender(At(650)));
	}

	[Fact]
	public void RequestImmediateRendersOnTheNextTick()
	{
		_policy.RequestImmediate();

		Assert.True(_policy.TryBeginRender(At(0)));
	}
}
