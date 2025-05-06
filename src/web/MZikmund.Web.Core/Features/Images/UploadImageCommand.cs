using MediatR;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Images;

public record UploadImageCommand(string FileName, Stream Stream) : IRequest<BlobInfo>;
