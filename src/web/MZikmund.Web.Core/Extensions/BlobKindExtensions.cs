using MZikmund.DataContracts.Storage;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Data.Extensions;

internal static class BlobKindExtensions
{
	internal static StorageItemType ToStorageItemType(this BlobKind kind) =>
		kind switch
		{
			BlobKind.Image => StorageItemType.Image,
			BlobKind.File => StorageItemType.File,
			_ => throw new InvalidOperationException("Unknown storage item type")
		};
}
