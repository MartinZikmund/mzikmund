using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

internal sealed class CommentReportConfiguration : IEntityTypeConfiguration<CommentReportEntity>
{
	public void Configure(EntityTypeBuilder<CommentReportEntity> builder)
	{
		builder.Property(r => r.Id).ValueGeneratedNever();
		builder.Property(r => r.Note).HasMaxLength(500);
		builder.Property(r => r.CreatedOn).HasConversion(new UtcDateTimeConverter());
		builder.Property(r => r.ResolvedOn).HasConversion(new UtcDateTimeConverter());

		builder.HasIndex(r => new { r.CommentId, r.ResolvedOn });

		builder
			.HasOne<CommentEntity>()
			.WithMany()
			.HasForeignKey(r => r.CommentId)
			.OnDelete(DeleteBehavior.Restrict);

		builder
			.HasOne<CommentAuthorEntity>()
			.WithMany()
			.HasForeignKey(r => r.ReporterId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
