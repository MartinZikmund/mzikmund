using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

internal sealed class PostConfiguration : IEntityTypeConfiguration<PostEntity>
{
	public void Configure(EntityTypeBuilder<PostEntity> builder)
	{
		builder.Property(c => c.Id).ValueGeneratedNever();
		builder.Property(c => c.Title).HasMaxLength(256);
		builder.Property(c => c.RouteName).HasMaxLength(256);
		builder.Property(c => c.Abstract).HasMaxLength(2048);
		builder.Property(c => c.Content).HasMaxLength(10000);
		builder.Property(c => c.HeroImageUrl).HasMaxLength(256);
		builder.Property(c => c.LanguageCode).HasMaxLength(12);
		builder.Property(c => c.HeroImageAlt).HasMaxLength(256);

		builder
			.HasMany(c => c.Categories)
			.WithMany(p => p.Posts)
			.UsingEntity<PostCategoryEntity>(
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
			.UsingEntity<PostTagEntity>(
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
