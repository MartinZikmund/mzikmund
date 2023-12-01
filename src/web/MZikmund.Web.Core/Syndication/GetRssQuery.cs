using MediatR;

namespace MZikmund.Web.Core.Syndication;

public record GetRssQuery(string? CategoryName, string? TagName) : IRequest<string?>;
