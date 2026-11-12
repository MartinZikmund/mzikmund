using MZikmund.Shared.Extensions;

namespace MZikmund.Shared.Blog;

public static class PostRouteNames
{
	/// <summary>
	/// A route still follows the title while it is empty or exactly what the previous title generated,
	/// i.e. nobody has customized it by hand.
	/// </summary>
	public static bool FollowsTitle(string route, string previousTitle) =>
		string.IsNullOrEmpty(route) || string.Equals(route, previousTitle.GenerateRouteName(), StringComparison.OrdinalIgnoreCase);
}
