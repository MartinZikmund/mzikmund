using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MZikmund.Web.Data.Entities.Configurations;

public class BlobMetadataConfiguration : IEntityTypeConfiguration<BlobMetadataEntity>
{
	public void Configure(EntityTypeBuilder<BlobMetadataEntity> builder)
	{
		builder.ToTable("BlobMetadata");
		builder.HasKey(b => b.Id);
		builder.Property(b => b.BlobPath).IsRequired().HasMaxLength(500);
		builder.Property(b => b.FileName).IsRequired().HasMaxLength(255);
		builder.Property(b => b.ContentType).HasMaxLength(100);
		builder.HasIndex(b => new { b.Kind, b.LastModified });
		builder.HasIndex(b => b.BlobPath).IsUnique();
	}
}
