using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Entities.Configurations;

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

	public virtual DbSet<BlobMetadataEntity> BlobMetadata { get; set; }
	public virtual DbSet<CategoryEntity> Category { get; set; }
	public virtual DbSet<CommentEntity> Comment { get; set; }
	public virtual DbSet<CommentAuthorEntity> CommentAuthor { get; set; }
	public virtual DbSet<CommentReportEntity> CommentReport { get; set; }
	public virtual DbSet<CommentVoteEntity> CommentVote { get; set; }
	public virtual DbSet<PostEntity> Post { get; set; }
	public virtual DbSet<PostCategoryEntity> PostCategory { get; set; }
	public virtual DbSet<PostTagEntity> PostTag { get; set; }
	public virtual DbSet<TagEntity> Tag { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfiguration(new BlobMetadataConfiguration());
		modelBuilder.ApplyConfiguration(new PostCategoryConfiguration());
		modelBuilder.ApplyConfiguration(new PostConfiguration());
		modelBuilder.ApplyConfiguration(new CategoryConfiguration());
		modelBuilder.ApplyConfiguration(new TagConfiguration());
		modelBuilder.ApplyConfiguration(new CommentConfiguration());
		modelBuilder.ApplyConfiguration(new CommentAuthorConfiguration());
		modelBuilder.ApplyConfiguration(new CommentVoteConfiguration());
		modelBuilder.ApplyConfiguration(new CommentReportConfiguration());
	}
}
