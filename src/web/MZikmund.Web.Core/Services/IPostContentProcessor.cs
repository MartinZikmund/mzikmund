﻿using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Services;

public interface IPostContentProcessor
{
	Task<string> ProcessAsync(string postContent);
}
