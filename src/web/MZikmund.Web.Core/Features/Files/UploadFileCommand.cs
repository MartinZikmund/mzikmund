using MediatR;
using Microsoft.AspNetCore.Http;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public record UploadFileCommand(string FileName, Stream Stream) : IRequest<BlobInfo>;
