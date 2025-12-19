using System;
using System.Collections.Generic;
using System.Text;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Services.Blobs;

public interface IBlobUrlProvider
{
	public Uri GetUrl(BlobKind blobKind, string blobPath);
}
