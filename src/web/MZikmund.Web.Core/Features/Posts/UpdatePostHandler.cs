using AutoMapper;
using MediatR;
using MZikmund.DataContracts.Blog;
using MZikmund.Shared.Extensions;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Posts;

public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, Post>
{
	private readonly IRepository<PostCategoryEntity> _postCategoryRepository;
	private readonly IRepository<PostTagEntity> _postTagRepository;
	private readonly IRepository<TagEntity> _tagRepository;
	private readonly IRepository<CategoryEntity> _categoryRepository;
	private readonly IRepository<PostEntity> _postRepository;
	private readonly IMapper _mapper;
	private readonly IDateProvider _dateProvider;
	private readonly ISiteConfiguration _siteConfiguration;

	public UpdatePostCommandHandler(
		IRepository<PostCategoryEntity> postCategoryRepository,
		IRepository<PostTagEntity> postTagRepositoryy,
		IRepository<TagEntity> tagRepository,
		IRepository<CategoryEntity> categoryRepository,
		IRepository<PostEntity> postRepository,
		IMapper mapper,
		IDateProvider dateProvider,
		ISiteConfiguration siteConfiguration)
	{
		_postTagRepository = postTagRepositoryy;
		_postCategoryRepository = postCategoryRepository;
		_tagRepository = tagRepository;
		_categoryRepository = categoryRepository;
		_postRepository = postRepository;
		_mapper = mapper;
		_dateProvider = dateProvider;
		_siteConfiguration = siteConfiguration;
	}

	public async Task<Post> Handle(UpdatePostCommand request, CancellationToken ct)
	{
		if (await _postRepository.AnyAsync(
			p => p.RouteName == request.UpdatedPost.RouteName &&
			p.Id != request.UpdatedPost.Id, ct))
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

		// Ensure the post has a preview token
		if (post.PreviewToken == null)
		{
			post.PreviewToken = Guid.NewGuid();
		}

		if (postEditModel.IsPublished && post.Status != PostStatus.Published)
		{
			post.Status = PostStatus.Published;
			// Use the provided published date, or default to now if not provided
			post.PublishedDate = postEditModel.PublishedDate ?? _dateProvider.UtcNow;
		}
		else if (postEditModel.IsPublished && post.Status == PostStatus.Published)
		{
			// If already published, update the published date if provided
			if (postEditModel.PublishedDate.HasValue)
			{
				post.PublishedDate = postEditModel.PublishedDate.Value;
			}
		}

		// If the post is already published, we shouldn't change its route name
		if (post.Status != PostStatus.Published)
		{
			post.RouteName = postEditModel.RouteName.Trim();
		}
		post.Title = postEditModel.Title.Trim();
		post.LastModifiedDate = _dateProvider.UtcNow;
		post.LanguageCode = postEditModel.LanguageCode;
		post.HeroImageUrl = string.IsNullOrWhiteSpace(postEditModel.HeroImageUrl) ? null : postEditModel.HeroImageUrl;

		// 1. Add new tags to tag lib
		var tags = postEditModel.Tags.ToArray();

		foreach (var tag in tags.Where(t => t.Id != Guid.Empty))
		{
			if (!Tag.IsValid(tag.DisplayName))
			{
				continue;
			}

			if (!await _tagRepository.AnyAsync(p => p.DisplayName == tag.DisplayName, ct))
			{
				await _tagRepository.AddAsync(new()
				{
					DisplayName = tag.DisplayName,
					RouteName = string.IsNullOrEmpty(tag.RouteName) ? tag.DisplayName.GenerateRouteName() : tag.RouteName,
				}, ct);
			}
		}

		post.Tags.Clear();
		if (tags.Length != 0)
		{
			foreach (var tag in tags)
			{
				var tagEntity = await _tagRepository.GetAsync(t => t.DisplayName == tag.DisplayName) ?? await CreateTag(tag);
				if (tagEntity is not null) post.Tags.Add(tagEntity);
			}
		}

		// 3. update categories

		post.Categories.Clear();

		foreach (var category in request.UpdatedPost.Categories)
		{
			var categoryEntity = await _categoryRepository.GetAsync(c => c.Id == category.Id);
			if (categoryEntity is not null) post.Categories.Add(categoryEntity);
		}

		await _postRepository.UpdateAsync(post, ct);

		return _mapper.Map<Post>(post);
	}

	private async Task<TagEntity> CreateTag(Tag item)
	{
		var newTag = new TagEntity
		{
			Id = Guid.NewGuid(),
			DisplayName = item.DisplayName,
			RouteName = string.IsNullOrEmpty(item.RouteName) ? item.DisplayName.GenerateRouteName() : item.RouteName,
		};

		var tag = await _tagRepository.AddAsync(newTag);
		return tag;
	}
}
