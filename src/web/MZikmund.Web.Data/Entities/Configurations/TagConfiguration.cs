using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

internal sealed class TagConfiguration : IEntityTypeConfiguration<TagEntity>
{
	public void Configure(EntityTypeBuilder<TagEntity> builder)
	{
		builder.Property(c => c.Id).ValueGeneratedNever();
		builder.Property(c => c.DisplayName).HasMaxLength(64);
		builder.Property(c => c.Description).HasMaxLength(256);
		builder.Property(c => c.RouteName).HasMaxLength(64);
	}
}
