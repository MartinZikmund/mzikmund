using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

internal sealed class CommentConfiguration : IEntityTypeConfiguration<CommentEntity>
{
	public void Configure(EntityTypeBuilder<CommentEntity> builder)
	{
		builder.Property(c => c.Id).ValueGeneratedNever();
		builder.Property(c => c.Content).IsRequired().HasMaxLength(4000);
		builder.Property(c => c.ContentHtml).IsRequired();
		builder.Property(c => c.MentionedDisplayName).HasMaxLength(256);
		builder.Property(c => c.CreatedOn).HasConversion(new UtcDateTimeConverter());
		builder.Property(c => c.EditedOn).HasConversion(new UtcDateTimeConverter());

		builder.HasIndex(c => new { c.PostId, c.ParentId, c.CreatedOn });
		builder.HasIndex(c => c.AuthorId);

		builder
			.HasOne(c => c.Post)
			.WithMany()
			.HasForeignKey(c => c.PostId)
			.OnDelete(DeleteBehavior.Restrict);

		builder
			.HasOne(c => c.Parent)
			.WithMany(c => c.Replies)
			.HasForeignKey(c => c.ParentId)
			.OnDelete(DeleteBehavior.Restrict);

		builder
			.HasOne(c => c.Author)
			.WithMany()
			.HasForeignKey(c => c.AuthorId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
