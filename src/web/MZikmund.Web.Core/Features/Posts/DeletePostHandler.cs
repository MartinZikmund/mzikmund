using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Posts;

public class DeletePostHandler : IRequestHandler<DeletePostCommand>
{
	private readonly IRepository<PostEntity> _postRepository;
	private readonly DatabaseContext _dbContext;

	public DeletePostHandler(IRepository<PostEntity> postRepo, DatabaseContext dbContext)
	{
		_postRepository = postRepo;
		_dbContext = dbContext;
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
			// Comment.PostId is Restrict, so the post's comments have to go first.
			await _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
			{
				await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

				await _dbContext.CommentVote
					.Where(v => _dbContext.Comment.Any(c => c.Id == v.CommentId && c.PostId == guid))
					.ExecuteDeleteAsync(cancellationToken);
				await _dbContext.CommentReport
					.Where(r => _dbContext.Comment.Any(c => c.Id == r.CommentId && c.PostId == guid))
					.ExecuteDeleteAsync(cancellationToken);
				await _dbContext.Comment
					.Where(c => c.PostId == guid)
					.ExecuteDeleteAsync(cancellationToken);

				_dbContext.ChangeTracker.Clear();
				await _postRepository.DeleteAsync(guid, cancellationToken);

				await transaction.CommitAsync(cancellationToken);
			});
		}
	}
}
