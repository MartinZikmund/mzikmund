# Hero Image Generator Tool

This tool automatically generates hero images for blog posts that don't have one using AI image generation.

## Features

- Connects to the database and finds all posts without hero images
- Generates an AI prompt based on the post title and content
- Uses OpenAI's DALL-E to generate a hero image
- Uploads the generated image to Azure Blob Storage
- Updates the post with the hero image URL

## Configuration

The tool uses `appsettings.json` for configuration. You need to provide:

### Required Settings

1. **DatabaseConnection**: Connection string to the SQL Server database
2. **AzureBlobStorage**: Connection string to Azure Blob Storage
3. **OpenAI.ApiKey**: Your OpenAI API key for DALL-E access

### Example Configuration

```json
{
  "ConnectionStrings": {
    "DatabaseConnection": "Server=localhost;Database=MZikmundBlog;...",
    "AzureBlobStorage": "DefaultEndpointsProtocol=https;AccountName=..."
  },
  "OpenAI": {
    "ApiKey": "sk-...",
    "Model": "dall-e-3",
    "ImageSize": "1024x1024",
    "Quality": "standard"
  },
  "General": {
    "CdnUrl": "https://cdn.mzikmund.dev"
  },
  "BlobStorage": {
    "MediaContainerName": "media"
  }
}
```

## Usage

1. Configure `appsettings.json` with your connection strings and API keys
2. Run the tool:

```bash
# Process all posts without hero images
dotnet run

# Process only a specific number of posts (e.g., 5 posts for testing)
dotnet run 5
```

The tool will:
- Query posts without hero images (ordered by publication/creation date, newest first)
- For each post:
  - Generate a prompt from the post title and content
  - Call OpenAI DALL-E to generate an image
  - Upload the image to Azure Blob Storage
  - Update the post with the hero image URL
- Log progress and any errors

## Notes

- The tool processes posts sequentially to avoid overwhelming the OpenAI API
- Each image is saved as `hero-images/{postId}.png` in the blob storage
- If a post fails to process, the error is logged and the tool continues with the next post
- Generated images are 1024x1024 PNG files by default

## OpenAI Costs

Be aware that generating images with DALL-E incurs costs:
- DALL-E 3 (1024x1024, standard quality): ~$0.04 per image
- The tool will process ALL posts without hero images unless you specify a limit

**Recommendations:**
- Start with a small number (e.g., `dotnet run 1`) to test the configuration
- Review the generated images before processing all posts
- Consider which posts actually need hero images
