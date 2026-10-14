using System.Collections.ObjectModel;
using Humanizer;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Services.Localization;
using MZikmund.Shared.Blog;

namespace MZikmund.ViewModels.Items;

public partial class CommentItemViewModel : ObservableObject
{
	private bool _repliesExhausted;

	public CommentItemViewModel(CommentDto item)
	{
		Item = item;
		Apply(item);
		foreach (var reply in item.Replies)
		{
			Replies.Add(new CommentItemViewModel(reply));
		}
	}

	public CommentDto Item { get; private set; }

	public Guid Id => Item.Id;

	public Guid? ParentId => Item.ParentId;

	public ObservableCollection<CommentItemViewModel> Replies { get; } = new();

	// The inline replies are the head of page 1, so the first expand fetches page 1 and merges.
	public int NextRepliesPage { get; private set; } = 1;

	public string DisplayName => Item.AuthorDisplayName;

	public Uri? AvatarUri => Uri.TryCreate(Item.AuthorAvatarUrl, UriKind.Absolute, out var uri) &&
		(uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) ? uri : null;

	public bool IsAuthorAdmin => Item.AuthorIsAdmin;

	public string MentionPrefix => CommentDisplay.MentionPrefix(Item.MentionedDisplayName);

	public string Content => IsTombstone ? "" : Item.Content;

	public string CreatedOnHumanized => Item.CreatedOn.Humanize();

	public string MetaText => IsEdited ?
		$"{CreatedOnHumanized} \u00B7 {Localizer.Instance.GetString("CommentEdited")}" :
		CreatedOnHumanized;

	public bool IsEdited => Item.EditedOn is not null && !IsTombstone;

	public bool IsTombstone => Item.Status != CommentStatus.Visible;

	public bool IsNotTombstone => !IsTombstone;

	public string TombstoneText => Localizer.Instance.GetString(
		Item.Status == CommentStatus.HiddenByModerator ? "CommentRemoved" : "CommentDeleted");

	public bool IsReply => Item.ParentId is not null;

	public bool ShowBody => !IsTombstone && !IsEditing;

	public bool ShowEditor => !IsTombstone && IsEditing;

	public bool CanReply => !IsTombstone;

	public bool IsOwn => Item.IsOwn && !IsTombstone;

	public bool IsNotOwn => !IsOwn;

	public int RemainingReplies => _repliesExhausted ? 0 : CommentDisplay.RemainingReplyCount(Item.ReplyCount, Replies.Count);

	public bool HasMoreReplies => RemainingReplies > 0;

	public string MoreRepliesText => string.Format(
		System.Globalization.CultureInfo.CurrentCulture,
		Localizer.Instance.GetString("CommentShowMoreRepliesFormat"),
		RemainingReplies);

	public string UpvoteCountText => UpvoteCount > 0 ? UpvoteCount.ToString(System.Globalization.CultureInfo.CurrentCulture) : "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(UpvoteCountText))]
	public partial int UpvoteCount { get; set; }

	[ObservableProperty]
	public partial bool HasUpvoted { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ShowBody))]
	[NotifyPropertyChangedFor(nameof(ShowEditor))]
	public partial bool IsEditing { get; set; }

	[ObservableProperty]
	public partial string EditText { get; set; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsNotBusy))]
	public partial bool IsBusy { get; set; }

	public bool IsNotBusy => !IsBusy;

	/// <summary>
	/// Refreshes the observable fields from a server response. Replies are kept as they are.
	/// </summary>
	public void Apply(CommentDto updated)
	{
		Item = updated;
		UpvoteCount = updated.UpvoteCount;
		HasUpvoted = updated.HasUpvoted;
		if (IsTombstone)
		{
			IsEditing = false;
		}

		OnPropertyChanged(string.Empty);
	}

	public void AddReply(CommentItemViewModel reply)
	{
		if (Replies.All(r => r.Id != reply.Id))
		{
			Replies.Add(reply);
		}

		Item = Item with { ReplyCount = Math.Max(Item.ReplyCount, Replies.Count) };
		RaiseRepliesChanged();
	}

	public void RemoveReply(CommentItemViewModel reply)
	{
		if (Replies.Remove(reply))
		{
			Item = Item with { ReplyCount = Math.Max(0, Item.ReplyCount - 1) };
			RaiseRepliesChanged();
		}
	}

	/// <summary>
	/// Merges a fetched replies page. Returns false when it brought nothing new, so the caller can stop paging.
	/// </summary>
	public bool MergeReplies(IEnumerable<CommentDto> page, bool isLastPage)
	{
		var added = false;
		foreach (var dto in page)
		{
			if (Replies.All(r => r.Id != dto.Id))
			{
				Replies.Add(new CommentItemViewModel(dto));
				added = true;
			}
		}

		NextRepliesPage++;
		_repliesExhausted = isLastPage || !added;
		RaiseRepliesChanged();
		return added;
	}

	private void RaiseRepliesChanged()
	{
		OnPropertyChanged(nameof(RemainingReplies));
		OnPropertyChanged(nameof(HasMoreReplies));
		OnPropertyChanged(nameof(MoreRepliesText));
	}
}
