// Based on https://github.com/EdiWang/Moonglade/blob/cf5571b0db09c7722b310ca9922cdcd542e93a51/src/Moonglade.Syndication/SyndicationDataSource.cs

using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Configuration;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;
using MZikmund.Web.Data.Specifications;

namespace MZikmund.Web.Core.Syndication;

public class SyndicationDataSource : ISyndicationDataSource
{
	private readonly string _baseUrl;
	private readonly IRepository<CategoryEntity> _categoriesRepository;
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IPostContentProcessor _postContentProcessor;
	private readonly ISiteConfiguration _siteConfiguration;

	public SyndicationDataSource(
		IHttpContextAccessor httpContextAccessor,
		IRepository<CategoryEntity> categoriesRepository,
		IRepository<PostEntity> postsRepository,
		IPostContentProcessor postContentProcessor,
		ISiteConfiguration siteConfiguration)
	{
		_categoriesRepository = categoriesRepository;
		_postsRepository = postsRepository;
		_postContentProcessor = postContentProcessor;
		_siteConfiguration = siteConfiguration;
		var acc = httpContextAccessor;
		_baseUrl = $"{acc.HttpContext.Request.Scheme}://{acc.HttpContext.Request.Host}";
	}

	public async Task<IReadOnlyList<FeedEntry>?> GetFeedDataAsync(string? categoryRouteName = null)
	{
		IReadOnlyList<FeedEntry> itemCollection;
		if (!string.IsNullOrWhiteSpace(categoryRouteName))
		{
			var cat = await _categoriesRepository.GetAsync(c => c.RouteName == categoryRouteName);
			if (cat is null)
			{
				return null;
			}

			itemCollection = await GetFeedEntriesAsync(cat.Id);
		}
		else
		{
			itemCollection = await GetFeedEntriesAsync();
		}

		return itemCollection;
	}

	private async Task<IReadOnlyList<FeedEntry>> GetFeedEntriesAsync(Guid? categoryId = null)
	{
		var specification = new ListPostsSpecification(1, 15, categoryId, null);
		var posts = await _postsRepository.SelectAsync(specification, post => new FeedEntry
		{
			Id = post.Id.ToString(),
			Title = post.Title,
			PublishedDate = post.PublishedDate ?? DateTimeOffset.UtcNow,
			UpdatedDate = post.LastModifiedDate ?? DateTimeOffset.UtcNow,
			Description = post.Content, // TODO: Do we want full content here?
			Link = $"{_baseUrl}/blog/{post.RouteName}",
			Author = $"{_siteConfiguration.Author.FirstName} {_siteConfiguration.Author.LastName}",
			AuthorEmail = _siteConfiguration.Author.Email,
			Categories = post.Categories.Select(pc => pc.DisplayName).ToArray()
		});

		foreach (var post in posts)
		{
			post.Description = await _postContentProcessor.ProcessAsync(post.Description);
		}

		return posts;
	}
}
