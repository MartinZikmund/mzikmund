using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Interfaces;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace MZikmund.HeroImageGenerator.Services;

/// <summary>
/// Image generation service using OpenAI DALL-E.
/// </summary>
public class OpenAIImageGenerationService : IImageGenerationService
{
    private readonly OpenAIService _openAIService;
    private readonly ILogger<OpenAIImageGenerationService> _logger;
    private readonly OpenAIOptions _options;

    public OpenAIImageGenerationService(
        IOptions<OpenAIOptions> options,
        ILogger<OpenAIImageGenerationService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _openAIService = new OpenAIService(new OpenAI.OpenAiOptions
        {
            ApiKey = _options.ApiKey
        });
    }

    public async Task<byte[]> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating image with prompt: {Prompt}", prompt);

            var imageResult = await _openAIService.Image.CreateImage(new ImageCreateRequest
            {
                Prompt = prompt,
                N = 1,
                Size = _options.ImageSize ?? StaticValues.ImageStatics.Size.Size1024,
                ResponseFormat = StaticValues.ImageStatics.ResponseFormat.Url,
                Model = _options.Model ?? Models.Dall_e_3,
                Quality = _options.Quality ?? StaticValues.ImageStatics.Quality.Standard
            }, cancellationToken);

            if (!imageResult.Successful)
            {
                var errorMessage = imageResult.Error?.Message ?? "Unknown error occurred";
                _logger.LogError("Failed to generate image: {Error}", errorMessage);
                throw new InvalidOperationException($"Failed to generate image: {errorMessage}");
            }

            var imageUrl = imageResult.Results.FirstOrDefault()?.Url;
            if (string.IsNullOrEmpty(imageUrl))
            {
                throw new InvalidOperationException("No image URL returned from OpenAI");
            }

            _logger.LogInformation("Image generated successfully. Downloading from URL: {Url}", imageUrl);

            // Download the image
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl, cancellationToken);

            _logger.LogInformation("Image downloaded successfully. Size: {Size} bytes", imageBytes.Length);

            return imageBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image with prompt: {Prompt}", prompt);
            throw;
        }
    }
}
