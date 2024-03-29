﻿namespace MZikmund.DataContracts.Blog;

public class Tag
{
	public Guid Id { get; set; }

	public string DisplayName { get; set; } = "";

	public string? Description { get; set; }

	public string RouteName { get; set; } = "";

	public static bool IsValid(string tagDisplayName) => !string.IsNullOrWhiteSpace(tagDisplayName);
}
