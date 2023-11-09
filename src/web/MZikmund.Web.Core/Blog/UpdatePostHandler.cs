using MediatR;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Core.Extensions;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostEntity>
{
	private readonly IRepository<PostCategoryEntity> _postCategoryRepository;
	private readonly IRepository<PostTagEntity> _postTagRepository;
	private readonly IRepository<TagEntity> _tagRepository;
	private readonly IRepository<CategoryEntity> _categoryRepository;
	private readonly IRepository<PostEntity> _postRepository;
	private readonly IDateProvider _dateProvider;
	private readonly ISiteConfiguration _siteConfiguration;

	public UpdatePostCommandHandler(
		IRepository<PostCategoryEntity> postCategoryRepository,
		IRepository<PostTagEntity> postTagRepositoryy,
		IRepository<TagEntity> tagRepository,
		IRepository<CategoryEntity> categoryRepository,
		IRepository<PostEntity> postRepository,
		IDateProvider dateProvider,
		ISiteConfiguration siteConfiguration)
	{
		_postTagRepository = postTagRepositoryy;
		_postCategoryRepository = postCategoryRepository;
		_tagRepository = tagRepository;
		_categoryRepository = categoryRepository;
		_postRepository = postRepository;
		_dateProvider = dateProvider;
		_siteConfiguration = siteConfiguration;
	}

	public async Task<PostEntity> Handle(UpdatePostCommand request, CancellationToken ct)
	{
		if (await _postRepository.AnyAsync(p => p.RouteName == request.UpdatedPost.RouteName, ct))
		{
			throw new InvalidOperationException("Post with same route name already exists.");
		}

		var (guid, postEditModel) = request;
		var post = await _postRepository.GetAsync(guid, ct);
		if (null == post)
		{
			throw new InvalidOperationException($"Post {guid} is not found.");
		}

		post.Abstract = postEditModel.Abstract;
		post.Content = postEditModel.Content;

		if (postEditModel.IsPublished && post.Status != PostStatus.Published)
		{
			post.Status = PostStatus.Published;
			post.PublishedDate = _dateProvider.UtcNow;
		}

		// #325: Allow changing publish date for published posts
		if (postEditModel.PublishedDate is not null && post.PublishedDate.HasValue)
		{
			post.PublishedDate = postEditModel.PublishedDate;
		}

		post.RouteName = postEditModel.RouteName.Trim();
		post.Title = postEditModel.Title.Trim();
		post.LastModifiedDate = _dateProvider.UtcNow;
		post.LanguageCode = postEditModel.LanguageCode;
		post.HeroImageUrl = string.IsNullOrWhiteSpace(postEditModel.HeroImageUrl) ? null : postEditModel.HeroImageUrl;

		// 1. Add new tags to tag lib
		var tags = string.IsNullOrWhiteSpace(postEditModel.Tags) ?
			Array.Empty<string>() :
			postEditModel.Tags.Split(',').ToArray();

		foreach (var tag in tags)
		{
			if (!Tag.IsValid(tag))
			{
				continue;
			}

			if (!await _tagRepository.AnyAsync(p => p.DisplayName == tag, ct))
			{
				await _tagRepository.AddAsync(new()
				{
					DisplayName = tag,
					RouteName = tag.GenerateRouteName()
				}, ct);
			}
		}

		post.Tags.Clear();
		if (tags.Any())
		{
			foreach (var tagName in tags)
			{
				if (!Tag.IsValid(tagName))
				{
					continue;
				}

				var tag = await _tagRepository.GetAsync(t => t.DisplayName == tagName);
				if (tag is not null) post.Tags.Add(tag);
			}
		}

		// 3. update categories

		post.Categories.Clear();

		foreach (var categoryId in request.UpdatedPost.CategoryIds)
		{
			var category = await _categoryRepository.GetAsync(c => c.Id == categoryId);
			if (category is not null) post.Categories.Add(category);
		}

		await _postRepository.UpdateAsync(post, ct);

		return post;
	}
}
