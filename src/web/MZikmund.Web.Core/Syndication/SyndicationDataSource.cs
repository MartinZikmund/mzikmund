// Based on https://github.com/EdiWang/Moonglade/blob/cf5571b0db09c7722b310ca9922cdcd542e93a51/src/Moonglade.Syndication/SyndicationDataSource.cs

using Microsoft.AspNetCore.Http;
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
	private readonly IRepository<TagEntity> _tagsRepository;
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IPostContentProcessor _postContentProcessor;
	private readonly ISiteConfiguration _siteConfiguration;

	public SyndicationDataSource(
		IHttpContextAccessor httpContextAccessor,
		IRepository<CategoryEntity> categoriesRepository,
		IRepository<TagEntity> tagsRepository,
		IRepository<PostEntity> postsRepository,
		IPostContentProcessor postContentProcessor,
		ISiteConfiguration siteConfiguration)
	{
		_categoriesRepository = categoriesRepository;
		_tagsRepository = tagsRepository;
		_postsRepository = postsRepository;
		_postContentProcessor = postContentProcessor;
		_siteConfiguration = siteConfiguration;
		var acc = httpContextAccessor;
		_baseUrl = $"{acc.HttpContext.Request.Scheme}://{acc.HttpContext.Request.Host}";
	}

	public async Task<IReadOnlyList<FeedEntry>?> GetFeedDataAsync(string? categoryRouteName = null, string? tagRouteName = null)
	{
		IReadOnlyList<FeedEntry> itemCollection;
		if (!string.IsNullOrWhiteSpace(categoryRouteName))
		{
			var category = await _categoriesRepository.GetAsync(c => c.RouteName == categoryRouteName);
			if (category is null)
			{
				return null;
			}

			itemCollection = await GetFeedEntriesAsync(category.Id);
		}
		else if (!string.IsNullOrWhiteSpace(tagRouteName))
		{
			var tag = await _tagsRepository.GetAsync(c => c.RouteName == tagRouteName);
			if (tag is null)
			{
				return null;
			}

			itemCollection = await GetFeedEntriesAsync(null, tag.Id);
		}
		else
		{
			itemCollection = await GetFeedEntriesAsync();
		}

		return itemCollection;
	}

	private async Task<IReadOnlyList<FeedEntry>> GetFeedEntriesAsync(Guid? categoryId = null, Guid? tagId = null)
	{
		var specification = new GetPostsSpecification(1, 15, categoryId, tagId);
		var posts = await _postsRepository.SelectAsync(specification, post => new FeedEntry
		{
			Id = post.Id.ToString(),
			Title = post.Title,
			PublishedDate = post.PublishedDate ?? DateTimeOffset.UtcNow,
			UpdatedDate = post.LastModifiedDate ?? DateTimeOffset.UtcNow,
			Description = post.Content,
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
