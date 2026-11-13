namespace MZikmund.ViewModels.Admin;

/// <summary>
/// Debounces preview renders: renders once typing pauses for <c>quietPeriod</c>, but never lets
/// an edit wait longer than <c>maxDelay</c>, so continuous typing still refreshes regularly.
/// </summary>
public sealed class PreviewRefreshPolicy(TimeSpan quietPeriod, TimeSpan maxDelay)
{
	private DateTimeOffset? _pendingSince;
	private DateTimeOffset _lastEdit;

	public void NotifyEdit(DateTimeOffset now)
	{
		_pendingSince ??= now;
		_lastEdit = now;
	}

	public void RequestImmediate()
	{
		_pendingSince = DateTimeOffset.MinValue;
		_lastEdit = DateTimeOffset.MinValue;
	}

	/// <summary>
	/// Returns true when a render is due and marks the pending edits as rendered. Edits made after this
	/// call are tracked again, so nothing typed while rendering is lost.
	/// </summary>
	public bool TryBeginRender(DateTimeOffset now)
	{
		if (_pendingSince is not { } pendingSince ||
			(now - _lastEdit < quietPeriod && now - pendingSince < maxDelay))
		{
			return false;
		}

		_pendingSince = null;
		return true;
	}
}
