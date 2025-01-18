using MediatR;

namespace MZikmund.Web.Core.Features.Files;

public record DeleteFileCommand(string Path) : IRequest;
