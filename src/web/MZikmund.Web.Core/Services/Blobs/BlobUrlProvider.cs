using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;
using MZikmund.Web.Configuration;
using MZikmund.Web.Configuration.ConfigSections;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Services.Blobs;

public class BlobUrlProvider : IBlobUrlProvider
{
	private readonly ISiteConfiguration _configuration;

	public BlobUrlProvider(ISiteConfiguration configuration)
	{
		_configuration = configuration;
	}

	public Uri GetUrl(BlobKind blobKind, string blobPath)
	{
		var containerName = blobKind switch
		{
			BlobKind.Image => _configuration.BlobStorage.MediaContainerName,
			BlobKind.File => _configuration.BlobStorage.FilesContainerName,
			_ => throw new ArgumentOutOfRangeException(nameof(blobKind), blobKind, null)
		};
		return new Uri(_configuration.General.CdnUrl, $"{containerName}/{blobPath}");
	}
}
