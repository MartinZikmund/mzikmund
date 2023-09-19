using MediatR;

namespace MZikmund.Web.Core.Syndication;

public record GetOpmlQuery(OpmlConfig OpmlDoc) : IRequest<string>;
