﻿using MediatR;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Core.Extensions;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, PostEntity>
{
	private readonly IRepository<PostEntity> _postRepository;
	private readonly IRepository<CategoryEntity> _categoryRepository;
	private readonly ILogger<CreatePostHandler> _logger;
	private readonly IDateProvider _dateProvider;
	private readonly IRepository<TagEntity> _tagRepo;
	private readonly ISiteConfiguration _siteConfiguration;

	public CreatePostHandler(
		IRepository<PostEntity> postRepository,
		IRepository<CategoryEntity> categoryRepository,
		IRepository<TagEntity> tagRepository,
		ISiteConfiguration siteConfiguration,
		IDateProvider dateProvider,
		ILogger<CreatePostHandler> logger)
	{
		_postRepository = postRepository;
		_categoryRepository = categoryRepository;
		_tagRepo = tagRepository;
		_siteConfiguration = siteConfiguration;
		_dateProvider = dateProvider;
		_logger = logger;
	}

	public async Task<PostEntity> Handle(CreatePostCommand request, CancellationToken ct)
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

		var categories = await _categoryRepository.ListAsync(ct);
		var selectedCategories = categories.Where(c => request.NewPost.CategoryIds.Contains(c.Id));

		foreach (var category in selectedCategories)
		{
			post.Categories.Add(category);
		}

		// add tags
		var tags = string.IsNullOrWhiteSpace(request.NewPost.Tags) ?
			Array.Empty<string>() :
			request.NewPost.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToArray();

		foreach (var tag in tags)
		{
			if (!Tag.IsValid(tag))
			{
				continue;
			}

			var tagEntity = await _tagRepo.GetAsync(q => q.DisplayName == tag) ?? await CreateTag(tag); // TODO: Add post tags more efficiently
			post.Tags.Add(tagEntity);
		}

		await _postRepository.AddAsync(post, ct);

		return post;
	}

	private async Task<TagEntity> CreateTag(string item)
	{
		var newTag = new TagEntity
		{
			DisplayName = item,
			RouteName = item.GenerateRouteName()
		};

		var tag = await _tagRepo.AddAsync(newTag);
		return tag;
	}
}
