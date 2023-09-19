using MediatR;

namespace MZikmund.Web.Core.Syndication;

public record GetAtomQuery(string? CategoryName) : IRequest<string?>;
