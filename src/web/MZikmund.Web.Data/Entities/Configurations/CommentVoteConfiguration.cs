using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

internal sealed class CommentVoteConfiguration : IEntityTypeConfiguration<CommentVoteEntity>
{
	public void Configure(EntityTypeBuilder<CommentVoteEntity> builder)
	{
		builder.HasKey(v => new { v.CommentId, v.AuthorId });
		builder.Property(v => v.CreatedOn).HasConversion(new UtcDateTimeConverter());

		builder
			.HasOne<CommentEntity>()
			.WithMany(c => c.Votes)
			.HasForeignKey(v => v.CommentId)
			.OnDelete(DeleteBehavior.Restrict);

		builder
			.HasOne<CommentAuthorEntity>()
			.WithMany()
			.HasForeignKey(v => v.AuthorId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
