using MediatR;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Configuration;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;
using AutoMapper;
using MZikmund.Shared.Extensions;

namespace MZikmund.Web.Core.Blog;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Post>
{
	private readonly IRepository<PostEntity> _postRepository;
	private readonly IRepository<CategoryEntity> _categoryRepository;
	private readonly ILogger<CreatePostHandler> _logger;
	private readonly IDateProvider _dateProvider;
	private readonly IMapper _mapper;
	private readonly IRepository<TagEntity> _tagRepo;

	public CreatePostHandler(
		IRepository<PostEntity> postRepository,
		IRepository<CategoryEntity> categoryRepository,
		IRepository<TagEntity> tagRepository,
		ISiteConfiguration siteConfiguration,
		IDateProvider dateProvider,
		IMapper mapper,
		ILogger<CreatePostHandler> logger)
	{
		_postRepository = postRepository;
		_categoryRepository = categoryRepository;
		_tagRepo = tagRepository;
		_dateProvider = dateProvider;
		_mapper = mapper;
		_logger = logger;
	}

	public async Task<Post> Handle(CreatePostCommand request, CancellationToken ct)
	{
		if (await _postRepository.AnyAsync(p => p.RouteName == request.NewPost.RouteName, ct))
		{
			throw new InvalidOperationException("Post with same route name already exists.");
		}

		var post = new PostEntity
		{
			Id = Guid.NewGuid(),
			Abstract = request.NewPost.Abstract,
			Content = request.NewPost.Content,
			CreatedDate = _dateProvider.UtcNow,
			LastModifiedDate = _dateProvider.UtcNow,
			RouteName = request.NewPost.RouteName.Trim(),
			Title = request.NewPost.Title.Trim(),
			LanguageCode = request.NewPost.LanguageCode,
			PublishedDate = request.NewPost.IsPublished ? _dateProvider.UtcNow : request.NewPost.PublishedDate,
			Status = request.NewPost.IsPublished ? PostStatus.Published : PostStatus.Draft,
			HeroImageUrl = string.IsNullOrWhiteSpace(request.NewPost.HeroImageUrl) ? null : request.NewPost.HeroImageUrl, // TODO: Generate hero image from content via Dall-E
		};

		foreach (var category in request.NewPost.Categories)
		{
			var existingCategory = await _categoryRepository.GetAsync(c => c.Id == category.Id);

			post.Categories.Add(existingCategory!);
		}

		// add tags
		var tags = request.NewPost.Tags.ToArray();

		foreach (var tag in tags)
		{
			if (!Tag.IsValid(tag.DisplayName))
			{
				continue;
			}

			var tagEntity = await _tagRepo.GetAsync(q => q.DisplayName == tag.DisplayName) ?? await CreateTag(tag); // TODO: Add post tags more efficiently
			post.Tags.Add(tagEntity);
		}

		await _postRepository.AddAsync(post, ct);

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

		var tag = await _tagRepo.AddAsync(newTag);
		return tag;
	}
}

