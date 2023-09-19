using MediatR;
using MZikmund.Syndication;

namespace MZikmund.Web.Core.Syndication;

public record GetOpmlQuery(OpmlConfig OpmlDoc) : IRequest<string>;
