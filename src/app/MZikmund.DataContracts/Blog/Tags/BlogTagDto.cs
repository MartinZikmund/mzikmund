using System;

namespace MZikmund.DataContracts.Blog.Tags;

public class BlogTagDto
{
	public int Id { get; set; }

	public string DisplayName { get; set; } = "";

	public string RouteName { get; set; } = "";
}
