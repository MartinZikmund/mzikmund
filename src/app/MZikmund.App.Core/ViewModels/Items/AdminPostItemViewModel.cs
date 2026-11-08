using MZikmund.DataContracts.Blog;
using MZikmund.Shared.Blog;

namespace MZikmund.ViewModels.Items;

public sealed partial class AdminPostItemViewModel : ObservableObject
{
	public AdminPostItemViewModel(PostListItem post, DateTimeOffset now)
	{
		Post = post;
		UpdateSchedule(now);
	}

	public PostListItem Post { get; }

	[ObservableProperty]
	public partial bool IsScheduled { get; private set; }

	[ObservableProperty]
	public partial string Countdown { get; private set; } = "";

	public void UpdateSchedule(DateTimeOffset now)
	{
		IsScheduled = PostPublishDates.IsScheduled(Post.IsPublished, Post.PublishedDate, now);
		Countdown = IsScheduled ? PublishCountdownText.Format(Post.PublishedDate!.Value - now) : "";
	}
}
