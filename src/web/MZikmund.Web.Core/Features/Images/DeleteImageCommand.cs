using MediatR;

namespace MZikmund.Web.Core.Features.Images;

public record DeleteImageCommand(string Path) : IRequest;
