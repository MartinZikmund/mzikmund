using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

internal sealed class CommentAuthorConfiguration : IEntityTypeConfiguration<CommentAuthorEntity>
{
	public void Configure(EntityTypeBuilder<CommentAuthorEntity> builder)
	{
		builder.Property(a => a.Id).ValueGeneratedNever();
		builder.Property(a => a.DisplayName).IsRequired().HasMaxLength(256);
		builder.Property(a => a.AvatarUrl).HasMaxLength(2048);
		builder.Property(a => a.Email).IsRequired().HasMaxLength(320);
		builder.Property(a => a.CreatedOn).HasConversion(new UtcDateTimeConverter());
		builder.Property(a => a.LastSeenOn).HasConversion(new UtcDateTimeConverter());
	}
}
