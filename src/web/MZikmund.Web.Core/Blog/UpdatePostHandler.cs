using MediatR;
using MZikmund.Web.Configuration;
using MZikmund.DataContracts.Blog;
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

		if (postEditModel.IsPublished && post.Status != PostStatus.Published)
		{
			post.Status = PostStatus.Published;
			post.PublishedDate = _dateProvider.UtcNow;
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
				var tagEntity = await _tagRepository.GetAsync(t => t.DisplayName == tag.DisplayName);
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

		return post;
	}
}
