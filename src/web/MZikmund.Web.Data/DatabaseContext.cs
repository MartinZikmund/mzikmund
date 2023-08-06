using MZikmund.Web.Data.Entities.Configurations;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Data;

public class DatabaseContext : DbContext
{
	public DatabaseContext()
	{
	}

	public DatabaseContext(DbContextOptions options)
		: base(options)
	{
	}

	public virtual DbSet<CategoryEntity> Category { get; set; }
	public virtual DbSet<PostEntity> Post { get; set; }
	public virtual DbSet<PostCategoryEntity> PostCategory { get; set; }
	public virtual DbSet<PostTagEntity> PostTag { get; set; }
	public virtual DbSet<TagEntity> Tag { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfiguration(new CategoryConfiguration());
		modelBuilder.ApplyConfiguration(new TagConfiguration());

		modelBuilder
			.Entity<PostEntity>()
			.HasMany(p => p.Tags)
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
