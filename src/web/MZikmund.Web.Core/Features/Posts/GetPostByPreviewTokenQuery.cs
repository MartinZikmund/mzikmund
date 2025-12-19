using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Posts;

public record GetPostByPreviewTokenQuery(Guid PreviewToken) : IRequest<Post>;
