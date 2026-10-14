using MZikmund.Api.Client;
using MZikmund.Services.Preferences;

namespace MZikmund.Services.Comments;

public class CommentsBadgeService : ICommentsBadgeService
{
	private readonly IMZikmundApi _api;
	private readonly IAppPreferences _preferences;
	private readonly ILogger<CommentsBadgeService> _logger;
	private int _refreshVersion;

	public CommentsBadgeService(IMZikmundApi api, IAppPreferences preferences, ILogger<CommentsBadgeService> logger)
	{
		_api = api;
		_preferences = preferences;
		_logger = logger;
	}

	public int UnreadCount { get; private set; }

	public event EventHandler? Changed;

	public async Task RefreshAsync()
	{
		var version = ++_refreshVersion;
		try
		{
			var since = _preferences.CommentsLastSeenOn?.UtcDateTime;
			var response = await _api.CountNewCommentsAsync(since);
			await response.EnsureSuccessfulAsync();

			// MarkSeen or a newer refresh may have overtaken this answer.
			if (version == _refreshVersion)
			{
				SetCount(response.Content);
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Refreshing the unread comments count failed");
		}
	}

	public void MarkSeen()
	{
		_refreshVersion++;
		_preferences.CommentsLastSeenOn = DateTimeOffset.UtcNow;
		SetCount(0);
	}

	private void SetCount(int count)
	{
		if (UnreadCount != count)
		{
			UnreadCount = count;
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}
}
