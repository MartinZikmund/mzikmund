using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Posts;

public class DeletePostHandler : IRequestHandler<DeletePostCommand>
{
	private IRepository<PostEntity> _postRepository;

	public DeletePostHandler(IRepository<PostEntity> postRepo)
	{
		_postRepository = postRepo;
	}

	public async Task Handle(DeletePostCommand request, CancellationToken cancellationToken)
	{
		var (guid, softDelete) = request;
		var post = await _postRepository.GetAsync(guid, cancellationToken);
		if (post is null)
		{
			return;
		}

		if (softDelete)
		{
			post.Status = PostStatus.Deleted;
			await _postRepository.UpdateAsync(post, cancellationToken);
		}
		else
		{
			await _postRepository.DeleteAsync(post, cancellationToken);
		}
	}
}
