using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using System.Text;

namespace MZikmund.HeroImageGenerator.Services;

/// <summary>
/// Service for generating and applying hero images to posts.
/// </summary>
public class HeroImageGeneratorService
{
    private readonly DatabaseContext _dbContext;
    private readonly IImageGenerationService _imageGenerationService;
    private readonly IBlobStorage _blobStorage;
    private readonly ISiteConfiguration _siteConfiguration;
    private readonly ILogger<HeroImageGeneratorService> _logger;

    public HeroImageGeneratorService(
        DatabaseContext dbContext,
        IImageGenerationService imageGenerationService,
        IBlobStorage blobStorage,
        ISiteConfiguration siteConfiguration,
        ILogger<HeroImageGeneratorService> logger)
    {
        _dbContext = dbContext;
        _imageGenerationService = imageGenerationService;
        _blobStorage = blobStorage;
        _siteConfiguration = siteConfiguration;
        _logger = logger;
    }

    /// <summary>
    /// Processes all posts without hero images.
    /// </summary>
    /// <param name="maxPosts">Optional maximum number of posts to process. If null, processes all posts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ProcessPostsAsync(int? maxPosts = null, CancellationToken cancellationToken = default)
    {
        await _blobStorage.InitializeAsync();

        var query = _dbContext.Post
            .Where(p => string.IsNullOrEmpty(p.HeroImageUrl))
            .OrderByDescending(p => p.PublishedDate ?? p.CreatedDate);

        var postsWithoutHeroImage = maxPosts.HasValue 
            ? await query.Take(maxPosts.Value).ToListAsync(cancellationToken)
            : await query.ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} posts without hero images{Limit}", 
            postsWithoutHeroImage.Count,
            maxPosts.HasValue ? $" (processing up to {maxPosts.Value})" : "");

        foreach (var post in postsWithoutHeroImage)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Operation cancelled");
                break;
            }

            await ProcessPostAsync(post, cancellationToken);
        }

        _logger.LogInformation("Completed processing posts");
    }

    /// <summary>
    /// Processes a single post to generate and apply a hero image.
    /// </summary>
    private async Task ProcessPostAsync(PostEntity post, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing post: {Title} (ID: {Id})", post.Title, post.Id);

            // Generate prompt from post title and content
            var prompt = GeneratePrompt(post);
            _logger.LogInformation("Generated prompt: {Prompt}", prompt);

            // Generate image using AI
            var imageBytes = await _imageGenerationService.GenerateImageAsync(prompt, cancellationToken);

            // Upload to Azure Storage
            var fileName = $"hero-images/{post.Id}.png";
            var blobInfo = await _blobStorage.AddAsync(BlobKind.Image, fileName, imageBytes);

            // Construct the full URL
            var cdnUrl = _siteConfiguration.General.CdnUrl;
            var heroImageUrl = $"{cdnUrl}/{_siteConfiguration.BlobStorage.MediaContainerName}/{fileName}";

            // Update post with hero image URL
            post.HeroImageUrl = heroImageUrl;
            post.HeroImageAlt = $"Hero image for {post.Title}";
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully processed post {Title}. Hero image URL: {Url}", post.Title, heroImageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process post {Title} (ID: {Id})", post.Title, post.Id);
            // Continue processing other posts even if one fails
        }
    }

    /// <summary>
    /// Generates an AI prompt from the post's title and content.
    /// </summary>
    private string GeneratePrompt(PostEntity post)
    {
        // Extract first few sentences from content (stripped of markdown)
        var contentPreview = GetContentPreview(post.Content, maxLength: 200);

        var promptBuilder = new StringBuilder();
        promptBuilder.Append("Create a professional, modern hero image for a blog post titled '");
        promptBuilder.Append(post.Title);
        promptBuilder.Append('\'');

        if (!string.IsNullOrWhiteSpace(contentPreview))
        {
            promptBuilder.Append(". The post is about: ");
            promptBuilder.Append(contentPreview);
        }

        promptBuilder.Append(". The image should be visually appealing, relevant to the topic, and suitable for a technology/software development blog.");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Extracts a preview of content, removing markdown syntax.
    /// </summary>
    private string GetContentPreview(string content, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        // Basic markdown stripping - remove common markdown syntax
        var preview = content
            .Replace("```", "")
            .Replace("**", "")
            .Replace("__", "")
            .Replace("*", "")
            .Replace("_", "")
            .Replace("#", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("(", "")
            .Replace(")", "")
            .Trim();

        // Take first maxLength characters
        if (preview.Length > maxLength)
        {
            preview = string.Concat(preview.AsSpan(0, maxLength), "...");
        }

        return preview;
    }
}
