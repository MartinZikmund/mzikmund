using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

internal class BlogPostConfiguration : IEntityTypeConfiguration<BlogPostEntity>
{
	public void Configure(EntityTypeBuilder<BlogPostEntity> builder)
	{
		builder.Property(c => c.Id).ValueGeneratedNever();
		builder.Property(c => c.DisplayName).HasMaxLength(64);
		builder.Property(c => c.Description).HasMaxLength(256);
		builder.Property(c => c.RouteName).HasMaxLength(64);

		builder
			.HasMany(c => c.Categories)
			.WithMany(p => p.Posts)
			.UsingEntity<BlogPostCategoryEntity>(
				j => j
					.HasOne(pt => pt.Category)
					.WithMany()
					.HasForeignKey(pt => pt.CategoryId),
				j => j
					.HasOne(pt => pt.Post)
					.WithMany()
					.HasForeignKey(pt => pt.PostId));

		builder
			.HasMany(c => c.Tags)
			.WithMany(p => p.Posts)
			.UsingEntity<BlogPostTagEntity>(
				j => j
					.HasOne(pt => pt.Tag)
					.WithMany()
					.HasForeignKey(pt => pt.TagId),
				j => j
					.HasOne(pt => pt.Post)
					.WithMany()
					.HasForeignKey(pt => pt.PostId));
	}
}
