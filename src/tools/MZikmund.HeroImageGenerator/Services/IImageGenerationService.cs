namespace MZikmund.HeroImageGenerator.Services;

/// <summary>
/// Service for generating images using AI.
/// </summary>
public interface IImageGenerationService
{
    /// <summary>
    /// Generates an image based on the provided prompt.
    /// </summary>
    /// <param name="prompt">The prompt to generate the image from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated image as a byte array.</returns>
    Task<byte[]> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default);
}
