using MediatR;

namespace MZikmund.Web.Core.Syndication;

public record GetRssQuery(string? CategoryName) : IRequest<string?>;
