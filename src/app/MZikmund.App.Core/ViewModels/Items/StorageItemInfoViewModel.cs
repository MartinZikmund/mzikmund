using System;
using System.Collections.Generic;
using System.Text;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blobs;

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
}
