using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Tags;

public record CreateTagCommand(Tag NewTag) : IRequest<Tag>;
