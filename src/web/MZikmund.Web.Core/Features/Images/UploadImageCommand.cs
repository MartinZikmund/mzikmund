using MediatR;
using MZikmund.DataContracts.Blobs;

namespace MZikmund.Web.Core.Features.Images;

public record UploadImageCommand(string FileName, Stream Stream) : IRequest<StorageItemInfo>;
