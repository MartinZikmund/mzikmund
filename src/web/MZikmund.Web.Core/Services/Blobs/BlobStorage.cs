using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Configuration;
using MZikmund.Web.Configuration.Connections;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Services.Blobs;

public class BlobStorage : IBlobStorage
{
	private readonly ILogger<BlobStorage> _logger;
	private readonly DatabaseContext _dbContext;

	private readonly BlobContainerClient _mediaContainer;
	private readonly BlobContainerClient _filesContainer;

	private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider = new();

	public BlobStorage(
		ISiteConfiguration siteConfiguration, 
		IConnectionStringProvider connectionStringProvider, 
		ILogger<BlobStorage> logger,
		DatabaseContext dbContext)
	{
		_logger = logger;
		_dbContext = dbContext;

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

	public async Task<StorageItemInfo> AddAsync(BlobKind blobKind, string blobPath, byte[] imageBytes)
	{
		await using var stream = new MemoryStream(imageBytes);
		return await AddAsync(blobKind, blobPath, stream);
	}

	public async Task<StorageItemInfo> AddAsync(BlobKind blobKind, string blobPath, Stream stream)
	{
		if (string.IsNullOrWhiteSpace(blobPath))
		{
			throw new ArgumentNullException(nameof(blobPath));
		}

		_logger.LogInformation($"Uploading {blobPath} to Azure Blob Storage.");

		var containerClient = GetBlobContainerClient(blobKind);
		var blobClient = containerClient.GetBlobClient(blobPath);
		var contentType = GetContentType(blobPath);
		var blobHttpHeader = new BlobHttpHeaders
		{
			ContentType = contentType
		};

		// Get stream length before upload
		var streamLength = stream.CanSeek ? stream.Length : 0;

		var uploadedBlob = await blobClient.UploadAsync(stream, blobHttpHeader);

		_logger.LogInformation($"Uploaded blob '{blobPath}' to Azure Blob Storage, ETag '{uploadedBlob.Value.ETag}'");
		var modified = uploadedBlob.Value.LastModified;

		// Save metadata to database
		var fileName = Path.GetFileName(blobPath);
		var metadata = new BlobMetadataEntity
		{
			Id = Guid.NewGuid(),
			Kind = (MZikmund.Web.Data.Entities.BlobKind)blobKind,
			BlobPath = blobPath,
			FileName = fileName,
			LastModified = modified,
			Size = streamLength,
			ContentType = contentType
		};

		_dbContext.BlobMetadata.Add(metadata);
		await _dbContext.SaveChangesAsync();

		return new(blobPath, modified);
	}

	private string GetContentType(string path)
	{
		if (_fileExtensionContentTypeProvider.TryGetContentType(path, out var contentType))
		{
			return contentType;
		}

		return "application/octet-stream";
	}

	public async Task DeleteAsync(BlobKind blobKind, string fileName)
	{
		var containerClient = GetBlobContainerClient(blobKind);
		await containerClient.DeleteBlobIfExistsAsync(fileName);

		// Delete metadata from database
		var dbBlobKind = (MZikmund.Web.Data.Entities.BlobKind)blobKind;
		var metadata = await _dbContext.BlobMetadata
			.FirstOrDefaultAsync(b => b.BlobPath == fileName && b.Kind == dbBlobKind);
		
		if (metadata != null)
		{
			_dbContext.BlobMetadata.Remove(metadata);
			await _dbContext.SaveChangesAsync();
		}
	}

	public async Task<StorageItemInfo[]> ListAsync(BlobKind blobKind, string? prefix = null)
	{
		var containerClient = GetBlobContainerClient(blobKind);
		var blobs = new List<StorageItemInfo>();
		await foreach (var blob in containerClient.GetBlobsAsync(prefix: prefix))
		{
			var blobName = blob.Name;
			var modified = blob.Properties.LastModified;
			blobs.Add(new(blobName, modified));
		}

		return blobs.ToArray();
	}

	public async Task<(StorageItemInfo[] Items, int TotalCount)> ListPagedAsync(BlobKind blobKind, int pageNumber, int pageSize, string? prefix = null)
	{
		if (pageNumber < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 1.");
		}

		if (pageSize <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0.");
		}

		// Query from database with efficient pagination
		var dbBlobKind = (MZikmund.Web.Data.Entities.BlobKind)blobKind;
		var query = _dbContext.BlobMetadata.Where(b => b.Kind == dbBlobKind);

		if (!string.IsNullOrEmpty(prefix))
		{
			query = query.Where(b => b.BlobPath.StartsWith(prefix));
		}

		var totalCount = await query.CountAsync();

		var skip = (pageNumber - 1) * pageSize;
		var items = await query
			.OrderByDescending(b => b.LastModified)
			.Skip(skip)
			.Take(pageSize)
			.Select(b => new StorageItemInfo(b.BlobPath, b.LastModified))
			.ToArrayAsync();

		return (items, totalCount);
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
