using System;
using MediatR;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Blog;
using MZikmund.Web.Core.Utilities;
using WilderMinds.MetaWeblog;
using WeblogTag = WilderMinds.MetaWeblog.Tag;
using WeblogPost = WilderMinds.MetaWeblog.Post;
using PostDto = MZikmund.Web.Core.Dtos.Post;
using CategoryDto = MZikmund.Web.Core.Dtos.Category;
using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Services;

public class MetaWeblogProvider : IMetaWeblogProvider
{
	private readonly ISiteConfiguration _siteConfiguration;
	private readonly ILogger<MetaWeblogProvider> _logger;
	private readonly IMediator _mediator;

	public MetaWeblogProvider(
		ISiteConfiguration blogConfig,
		ILogger<MetaWeblogProvider> logger,
		IMediator mediator)
	{
		_siteConfiguration = blogConfig;
		_logger = logger;
		_mediator = mediator;
	}

	public Task<int> AddCategoryAsync(string key, string username, string password, NewCategory newCategory) => TryExecuteAsync(async () =>
	{
		ValidateUser(username, password);

		var category = await _mediator.Send(new CreateCategoryCommand
		{
			DisplayName = newCategory.name.Trim(),
			RouteName = newCategory.slug.ToLowerInvariant(),
			Description = newCategory.description.Trim()
		});

		return category.Id.GetHashCode();
	});

	public Task<WeblogTag[]> GetTagsAsync(string blogid, string username, string password) => TryExecuteAsync(async () =>
	{
		ValidateUser(username, password);

		var tags = await _mediator.Send(new GetTagDisplayNamesQuery());

		var weblogTags = tags
			.Select(tagDisplayName => new WeblogTag()
			{
				name = tagDisplayName,
			})
			.ToArray();

		return weblogTags;
	});

	public Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password) => TryExecute(() =>
	{
		ValidateUser(username, password);

		return TryExecute(() =>
		{
			var blog = new BlogInfo
			{
				blogid = _siteConfiguration.General.Url.ToString(),
				blogName = _siteConfiguration.General.DefaultTitle,
				url = "/"
			};

			return Task.FromResult(new[] { blog });
		});
	});

	public Task<string> AddPostAsync(string blogid, string username, string password, WeblogPost post, bool publish) => Task.FromResult("");

	public Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish) => Task.FromResult(false);

	public Task<bool> EditPostAsync(string postid, string username, string password, WeblogPost post, bool publish) => Task.FromResult(false);

	public Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password) => TryExecuteAsync(async () =>
	{
		ValidateUser(username, password);

		var categories = await _mediator.Send(new GetCategoriesQuery());
		return categories
			.Select(c => new CategoryInfo()
			{
				categoryid = c.Id.ToString(),
				description = c.Description,
				title = c.DisplayName,
				htmlUrl = new Uri(_siteConfiguration.General.Url, "blog/category/" + c.RouteName).ToString()
			})
			.ToArray();
	});

	public Task<Page> GetPageAsync(string blogid, string pageid, string username, string password) => Task.FromResult(new Page());

	public Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages) => Task.FromResult(Array.Empty<Page>());

	public Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish) => Task.FromResult(string.Empty);

	public Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish) => Task.FromResult(false);

	public Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid) => Task.FromResult(false);

	public async Task<WeblogPost> GetPostAsync(string postid, string username, string password)
	{
		ValidateUser(username, password);

		var post = await _mediator.Send(new GetPostByIdQuery(new Guid(postid)));
		return ToWeblogPost(post);
	}

	public Task<WeblogPost[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts) => TryExecuteAsync(async () =>
	{
		ValidateUser(username, password);

		var posts = await _mediator.Send(new ListPostsQuery(1, numberOfPosts));
		return posts.Select(p => ToWeblogPost(p)).ToArray();
	});

	public Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject) => TryExecute(() =>
	{
		ValidateUser(username, password);

		// TODO: Add support for Storage upload.

		return Task.FromResult(new MediaObjectInfo());
	});

	public Task<Author[]> GetAuthorsAsync(string blogid, string username, string password) => TryExecute(() =>
	{
		var author = new Author()
		{
			display_name = _siteConfiguration.Author.Username,
			user_id = _siteConfiguration.Author.Username,
			user_login = _siteConfiguration.Author.Username,
		};

		return Task.FromResult(new[] { author });
	});

	public Task<UserInfo> GetUserInfoAsync(string key, string username, string password) => TryExecute(() =>
	{
		ValidateUser(username, password);

		var user = new UserInfo
		{
			email = _siteConfiguration.Author.Email,
			firstname = _siteConfiguration.Author.FirstName,
			lastname = _siteConfiguration.Author.LastName,
			nickname = _siteConfiguration.Author.Username,
			url = _siteConfiguration.General.Url.AbsoluteUri,
			userid = key
		};

		return Task.FromResult(user);
	});

	private void ValidateUser(string username, string password)
	{
		if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
		{
			throw new ArgumentNullException(nameof(password));
		}

		if (string.IsNullOrEmpty(_siteConfiguration.MetaWeblog.Username) ||
			string.IsNullOrEmpty(_siteConfiguration.MetaWeblog.PasswordHash))
		{
			throw new MetaWeblogException("MetaWeblog authentication is not configured.");
		}

		var passwordHash = Hasher.HashPassword(password.Trim());

		if (string.Compare(username.Trim(), _siteConfiguration.MetaWeblog.Username, StringComparison.OrdinalIgnoreCase) == 0 &&
			string.Compare(passwordHash, _siteConfiguration.MetaWeblog.PasswordHash.Trim(), StringComparison.Ordinal) == 0) return;

		throw new MetaWeblogException("Authentication failed.");
	}

	private WeblogPost ToWeblogPost(PostDto post)
	{
		var pubDate = post.PublishedDate.GetValueOrDefault();
		var link = $"blog/{post.RouteName.ToLowerInvariant()}";

		var weblogPost = new WeblogPost
		{
			postid = post.Id,
			categories = post.Categories.Select(p => p.DisplayName).ToArray(),
			dateCreated = post.LastModifiedDate?.DateTime ?? new DateTime(2000, 1, 1, 0, 0, 0),
			description = post.Abstract,
			link = link,
			permalink = new Uri(_siteConfiguration.General.Url, link).AbsoluteUri,
			title = post.Title,
			wp_slug = post.RouteName,
			mt_keywords = string.Join(',', post.Tags.Select(p => p.DisplayName)),
			mt_excerpt = post.Abstract,
			userid = _siteConfiguration.Author.Username
		};

		return weblogPost;
	}

	private WeblogPost ToWeblogPost(PostListItem post)
	{
		var pubDate = post.PublishedDate.GetValueOrDefault();
		var link = $"blog/{post.RouteName.ToLowerInvariant()}";

		var weblogPost = new WeblogPost
		{
			postid = post.Id,
			categories = post.Categories.Select(p => p.DisplayName).ToArray(),
			dateCreated = post.LastModifiedDate?.DateTime ?? new DateTime(2000, 1, 1, 0, 0, 0),
			description = post.Abstract,
			link = link,
			permalink = new Uri(_siteConfiguration.General.Url, link).AbsoluteUri,
			title = post.Title,
			wp_slug = post.RouteName,
			mt_keywords = string.Join(',', post.Tags.Select(p => p.DisplayName)),
			mt_excerpt = post.Abstract,
			userid = _siteConfiguration.Author.Username
		};

		return weblogPost;
	}

	private T TryExecute<T>(Func<T> action)
	{
		try
		{
			return action();
		}
		catch (Exception e)
		{
			_logger.LogError(e, e.Message);
			throw new MetaWeblogException(e.Message);
		}
	}

	private async Task<T> TryExecuteAsync<T>(Func<Task<T>> asyncAction)
	{
		try
		{
			return await asyncAction();
		}
		catch (Exception e)
		{
			_logger.LogError(e, e.Message);
			throw new MetaWeblogException(e.Message);
		}
	}
}
