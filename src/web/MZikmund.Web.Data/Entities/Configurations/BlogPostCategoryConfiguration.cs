using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

internal class BlogPostCategoryConfiguration : IEntityTypeConfiguration<BlogPostCategoryEntity>
{
	public void Configure(EntityTypeBuilder<BlogPostCategoryEntity> builder)
	{
		builder.HasMany()
	}
}
