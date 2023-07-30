using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

internal sealed class PostCategoryConfiguration : IEntityTypeConfiguration<PostCategoryEntity>
{
	public void Configure(EntityTypeBuilder<PostCategoryEntity> builder)
	{
	}
}
