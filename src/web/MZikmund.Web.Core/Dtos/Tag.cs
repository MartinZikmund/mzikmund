﻿using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Dtos;

public class Tag
{
	public Guid Id { get; set; }

	public string DisplayName { get; set; } = "";

	public string? Description { get; set; }

	public string RouteName { get; set; } = "";
}