using MediatR;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Blog;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Core.Extensions;
using MZikmund.Web.Core.Utilities;
using WilderMinds.MetaWeblog;
using PostDto = MZikmund.Web.Core.Dtos.Post;
using WeblogPost = WilderMinds.MetaWeblog.Post;
using WeblogTag = WilderMinds.MetaWeblog.Tag;

namespace MZikmund.Web.Core.Services;

public class MetaWeblogProvider : IMetaWeblogProvider
{
	private readonly ISiteConfiguration _siteConfiguration;
	private readonly IMediaBlobPathGenerator _mediaBlobPathGenerator;
	private readonly IBlobStorage _blobStorage;
	private readonly ILogger<MetaWeblogProvider> _logger;
	private readonly IMediator _mediator;

	public MetaWeblogProvider(
		ISiteConfiguration blogConfig,
		IMediaBlobPathGenerator mediaBlobPathGenerator,
		IBlobStorage blobStorage,
		IMediator mediator,
		ILogger<MetaWeblogProvider> logger)
	{
		_siteConfiguration = blogConfig;
		_mediaBlobPathGenerator = mediaBlobPathGenerator;
		_blobStorage = blobStorage;
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

	public Task<string> AddPostAsync(string blogid, string username, string password, WeblogPost post, bool publish) => TryExecuteAsync(async () =>
	{
		ValidateUser(username, password);

		var categoryIds = await GetCategoryIds(post.categories);
		if (categoryIds.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(post.categories));
		}

		var req = new PostEditModel
		{
			Title = post.title,
			RouteName = post.wp_slug ?? post.title.GenerateRouteName(),
			Abstract = post.mt_excerpt,
			Content = post.description,
			HeroImageUrl = post.wp_post_thumbnail,
			Tags = post.mt_keywords,
			CategoryIds = categoryIds,
			LanguageCode = "en-us",
			IsPublished = publish,
		};

		var p = await _mediator.Send(new CreatePostCommand(req));
		return p.Id.ToString();
	});

	public Task<bool> EditPostAsync(string postid, string username, string password, WeblogPost post, bool publish) => TryExecuteAsync(async () =>
	{
		ValidateUser(username, password);

		if (!Guid.TryParse(postid.Trim(), out var id))
		{
			throw new ArgumentException("Invalid ID", nameof(postid));
		}

		var categoryIds = await GetCategoryIds(post.categories);
		if (categoryIds.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(post.categories));
		}

		var req = new PostEditModel
		{
			Id = new Guid(postid),
			Title = post.title,
			RouteName = post.wp_slug ?? post.title.GenerateRouteName(),
			Abstract = post.mt_excerpt,
			Content = post.description,
			HeroImageUrl = post.wp_post_thumbnail,
			Tags = post.mt_keywords,
			CategoryIds = categoryIds,
			LanguageCode = "en-us",
			IsPublished = publish,
		};

		await _mediator.Send(new UpdatePostCommand(id, req));
		return true;
	});

	public Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish) => TryExecuteAsync(async () =>
	{
		ValidateUser(username, password);

		if (!Guid.TryParse(postid.Trim(), out var guid))
		{
			throw new ArgumentException("Invalid ID", nameof(postid));
		}

		await _mediator.Send(new DeletePostCommand(guid, softDelete: publish));
		return true;
	});

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

	public Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject) => TryExecuteAsync(async () =>
	{
		ValidateUser(username, password);

		var data = Convert.FromBase64String(mediaObject.bits);

		var blobPath = _mediaBlobPathGenerator.GenerateBlobPath(Path.GetFileName(mediaObject.name));
		var filename = await _blobStorage.AddAsync(blobPath, data);

		var imageUrl = new Uri(_siteConfiguration.General.CdnUrl, $"{_siteConfiguration.BlobStorage.MediaContainerName}/{blobPath}");

		var objectInfo = new MediaObjectInfo { url = imageUrl.OriginalString };
		return objectInfo;
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

	/* Pages - not used */

	public Task<Page> GetPageAsync(string blogid, string pageid, string username, string password) => Task.FromResult(new Page());

	public Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages) => Task.FromResult(Array.Empty<Page>());

	public Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish) => Task.FromResult(string.Empty);

	public Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish) => Task.FromResult(false);

	public Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid) => Task.FromResult(false);

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

		var weblogPost = new ExtendedWeblogPost
		{
			postid = post.Id.ToString(),
			categories = post.Categories.Select(p => p.DisplayName).ToArray(),
			dateCreated = post.LastModifiedDate?.DateTime ?? new DateTime(2000, 1, 1, 0, 0, 0),
			//description = post.Content,
			link = link,
			permalink = new Uri(_siteConfiguration.General.Url, link).AbsoluteUri,
			title = post.Title,
			wp_slug = post.RouteName,
			mt_keywords = string.Join(',', post.Tags.Select(p => p.DisplayName)),
			mt_excerpt = post.Abstract,
			userid = _siteConfiguration.Author.Username,
			custom_fields =
			{
				new CustomField
				{
					key = "mt_markdown",
					value = post.Content
				}
			}
		};

		return weblogPost;
	}

	private WeblogPost ToWeblogPost(PostListItem post)
	{
		var pubDate = post.PublishedDate.GetValueOrDefault();
		var link = $"blog/{post.RouteName.ToLowerInvariant()}";

		var weblogPost = new ExtendedWeblogPost
		{
			postid = post.Id.ToString(),
			categories = post.Categories.Select(p => p.DisplayName).ToArray(),
			dateCreated = post.LastModifiedDate?.DateTime ?? new DateTime(2000, 1, 1, 0, 0, 0),
			//description = post.Content,
			link = link,
			permalink = new Uri(_siteConfiguration.General.Url, link).AbsoluteUri,
			title = post.Title,
			wp_slug = post.RouteName,
			mt_keywords = string.Join(',', post.Tags.Select(p => p.DisplayName)),
			mt_excerpt = post.Abstract,
			userid = _siteConfiguration.Author.Username,
			custom_fields =
			{
				new CustomField
				{
					key = "mt_markdown",
					value = post.Content
				}
			}
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

	private async Task<Guid[]> GetCategoryIds(string[] postCategories)
	{
		var allCategories = await _mediator.Send(new GetCategoriesQuery());
		var categoryIds = (
			from postCategory in postCategories
			select allCategories.FirstOrDefault(category => category.DisplayName == postCategory)
			into category
			where category is not null
			select category.Id).ToArray();

		return categoryIds;
	}
}
