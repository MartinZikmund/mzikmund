using MediatR;
using MZikmund.DataContracts.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public record UploadFileCommand(string FileName, Stream Stream) : IRequest<StorageItemInfo>;
