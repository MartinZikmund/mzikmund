using MZikmund.Business.Models;
using MZikmund.DataContracts.Blobs;
using MZikmund.DataContracts.Extensions;

namespace MZikmund.ViewModels.Items;

public class StorageItemInfoViewModel
{
	private readonly StorageItemInfo _info;
	private readonly AppConfig _appConfig;

	public StorageItemInfoViewModel(StorageItemInfo info, AppConfig appConfig)
	{
		_info = info;
		_appConfig = appConfig;
	}

	public string FileName => _info.FileName;

	public string Extension => _info.Extension;

	public DateTimeOffset? LastModified => _info.LastModified;

	public string BlobPath => _info.BlobPath;

	public Uri? Url => _info.Url;

	public long Size => _info.Size;

	public string FormattedSize => Size.ToFileSizeString();

	public bool IsImage
	{
		get
		{
			var extension = Extension.ToLowerInvariant();
			return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".svg";
		}
	}
}
