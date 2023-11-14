using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public record CreateTagCommand(Tag NewTag) : IRequest<Tag>;
