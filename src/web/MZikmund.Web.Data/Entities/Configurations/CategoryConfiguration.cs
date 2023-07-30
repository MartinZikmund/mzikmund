using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<CategoryEntity>
{
	public void Configure(EntityTypeBuilder<CategoryEntity> builder)
	{
		builder.Property(c => c.Id).ValueGeneratedNever();
		builder.Property(c => c.DisplayName).HasMaxLength(64);
		builder.Property(c => c.Description).HasMaxLength(256);
		builder.Property(c => c.Icon).HasMaxLength(64);
		builder.Property(c => c.RouteName).HasMaxLength(64);
	}
}
