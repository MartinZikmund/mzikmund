using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Configuration;
using MZikmund.Web.Configuration.Connections;

namespace MZikmund.Web.Core.Services.Blobs;

public class BlobStorage : IBlobStorage
{
	private readonly ILogger<BlobStorage> _logger;

	private readonly BlobContainerClient _mediaContainer;
	private readonly BlobContainerClient _filesContainer;

	public BlobStorage(ISiteConfiguration siteConfiguration, IConnectionStringProvider connectionStringProvider, ILogger<BlobStorage> logger)
	{
		_logger = logger;

		_mediaContainer = new(connectionStringProvider.AzureBlobStorage, siteConfiguration.BlobStorage.MediaContainerName);
		_filesContainer = new(connectionStringProvider.AzureBlobStorage, siteConfiguration.BlobStorage.FilesContainerName);
	}

	public async Task InitializeAsync()
	{
		await _mediaContainer.CreateIfNotExistsAsync();
		await _mediaContainer.SetAccessPolicyAsync(PublicAccessType.Blob);
		await _filesContainer.CreateIfNotExistsAsync();
		await _filesContainer.SetAccessPolicyAsync(PublicAccessType.Blob);
	}

	public async Task<BlobInfo> AddAsync(BlobKind blobKind, string blobPath, byte[] imageBytes)
	{
		await using var stream = new MemoryStream(imageBytes);
		return await AddAsync(blobKind, blobPath, stream);
	}

	public async Task<BlobInfo> AddAsync(BlobKind blobKind, string blobPath, Stream stream)
	{
		if (string.IsNullOrWhiteSpace(blobPath))
		{
			throw new ArgumentNullException(nameof(blobPath));
		}

		_logger.LogInformation($"Uploading {blobPath} to Azure Blob Storage.");

		var containerClient = GetBlobContainerClient(blobKind);
		var blobClient = containerClient.GetBlobClient(blobPath);

		var uploadedBlob = await blobClient.UploadAsync(stream);

		_logger.LogInformation($"Uploaded blob '{blobPath}' to Azure Blob Storage, ETag '{uploadedBlob.Value.ETag}'");
		var modified = uploadedBlob.Value.LastModified;
		return new(blobPath, modified);
	}

	public async Task DeleteAsync(BlobKind blobKind, string fileName)
	{
		var containerClient = GetBlobContainerClient(blobKind);
		await containerClient.DeleteBlobIfExistsAsync(fileName);
	}

	public async Task<BlobInfo[]> ListAsync(BlobKind blobKind, string? prefix = null)
	{
		var containerClient = GetBlobContainerClient(blobKind);
		var blobs = new List<BlobInfo>();
		await foreach (var blob in containerClient.GetBlobsAsync(prefix: prefix))
		{
			var blobName = blob.Name;
			var modified = blob.Properties.LastModified;
			blobs.Add(new(blobName, modified));
		}

		return blobs.ToArray();
	}

	private BlobContainerClient GetBlobContainerClient(BlobKind blobKind)
	{
		var containerClient = blobKind switch
		{
			BlobKind.Image => _mediaContainer,
			BlobKind.File => _filesContainer,
			_ => throw new ArgumentOutOfRangeException(nameof(blobKind), blobKind, null)
		};

		return containerClient;
	}
}
