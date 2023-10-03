using MediatR;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Blog;
using MZikmund.Web.Core.Utilities;
using WilderMinds.MetaWeblog;
using WeblogTag = WilderMinds.MetaWeblog.Tag;

namespace MZikmund.Web.Core.Services;

public class MetaWeblogService : IMetaWeblogProvider
{
	private readonly ISiteConfiguration _siteConfiguration;
	private readonly ILogger<MetaWeblogService> _logger;
	private readonly IMediator _mediator;

	public MetaWeblogService(
		ISiteConfiguration blogConfig,
		ILogger<MetaWeblogService> logger,
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

	public Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish) => throw new NotImplementedException();
	public Task<string> AddPostAsync(string blogid, string username, string password, Post post, bool publish) => throw new NotImplementedException();
	public Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid) => throw new NotImplementedException();
	public Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish) => throw new NotImplementedException();
	public Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish) => throw new NotImplementedException();
	public Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish) => throw new NotImplementedException();
	public Task<Author[]> GetAuthorsAsync(string blogid, string username, string password) => throw new NotImplementedException();
	public Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password) => throw new NotImplementedException();
	public Task<Page> GetPageAsync(string blogid, string pageid, string username, string password) => throw new NotImplementedException();
	public Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages) => throw new NotImplementedException();
	public Task<Post> GetPostAsync(string postid, string username, string password) => throw new NotImplementedException();
	public Task<Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts) => throw new NotImplementedException();
	public Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password) => throw new NotImplementedException();
	public Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject) => throw new NotImplementedException();

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
