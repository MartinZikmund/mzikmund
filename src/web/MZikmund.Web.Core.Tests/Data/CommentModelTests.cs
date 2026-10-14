using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Tests.Data;

public class CommentModelTests
{
	private static DatabaseContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<DatabaseContext>()
			.UseLazyLoadingProxies()
			.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MZikmundModelOnly")
			.Options;

		return new DatabaseContext(options);
	}

	[Fact]
	public void Model_CommentTables_UseSingularNames()
	{
		// Arrange
		using var context = CreateContext();

		// Act
		var commentTable = context.Model.FindEntityType(typeof(CommentEntity))!.GetTableName();
		var authorTable = context.Model.FindEntityType(typeof(CommentAuthorEntity))!.GetTableName();
		var voteTable = context.Model.FindEntityType(typeof(CommentVoteEntity))!.GetTableName();
		var reportTable = context.Model.FindEntityType(typeof(CommentReportEntity))!.GetTableName();

		// Assert
		Assert.Equal("Comment", commentTable);
		Assert.Equal("CommentAuthor", authorTable);
		Assert.Equal("CommentVote", voteTable);
		Assert.Equal("CommentReport", reportTable);
	}

	[Fact]
	public void Model_CommentContent_IsCappedAt4000Characters()
	{
		// Arrange
		using var context = CreateContext();
		var comment = context.Model.FindEntityType(typeof(CommentEntity))!;

		// Act
		var maxLength = comment.FindProperty(nameof(CommentEntity.Content))!.GetMaxLength();

		// Assert
		Assert.Equal(4000, maxLength);
	}

	[Fact]
	public void Model_CommentContentHtml_HasNoMaxLength()
	{
		// Arrange
		using var context = CreateContext();
		var comment = context.Model.FindEntityType(typeof(CommentEntity))!;

		// Act
		var property = comment.FindProperty(nameof(CommentEntity.ContentHtml))!;

		// Assert
		Assert.Null(property.GetMaxLength());
		Assert.False(property.IsNullable);
	}

	[Fact]
	public void Model_CommentReportNote_IsCappedAt500Characters()
	{
		// Arrange
		using var context = CreateContext();
		var report = context.Model.FindEntityType(typeof(CommentReportEntity))!;

		// Act
		var maxLength = report.FindProperty(nameof(CommentReportEntity.Note))!.GetMaxLength();

		// Assert
		Assert.Equal(500, maxLength);
	}

	[Fact]
	public void Model_CommentIds_AreNeverGeneratedByTheDatabase()
	{
		// Arrange
		using var context = CreateContext();

		// Act
		var commentId = context.Model.FindEntityType(typeof(CommentEntity))!.FindProperty(nameof(CommentEntity.Id))!;
		var authorId = context.Model.FindEntityType(typeof(CommentAuthorEntity))!.FindProperty(nameof(CommentAuthorEntity.Id))!;
		var reportId = context.Model.FindEntityType(typeof(CommentReportEntity))!.FindProperty(nameof(CommentReportEntity.Id))!;

		// Assert
		Assert.Equal(ValueGenerated.Never, commentId.ValueGenerated);
		Assert.Equal(ValueGenerated.Never, authorId.ValueGenerated);
		Assert.Equal(ValueGenerated.Never, reportId.ValueGenerated);
	}

	[Fact]
	public void Model_Comment_HasPostParentCreatedOnIndex()
	{
		// Arrange
		using var context = CreateContext();
		var comment = context.Model.FindEntityType(typeof(CommentEntity))!;
		var expected = new[] { "PostId", "ParentId", "CreatedOn" };

		// Act
		var indexes = comment.GetIndexes();

		// Assert
		Assert.Contains(indexes, index => index.Properties.Select(p => p.Name).SequenceEqual(expected));
	}

	[Fact]
	public void Model_Comment_HasAuthorIdIndex()
	{
		// Arrange
		using var context = CreateContext();
		var comment = context.Model.FindEntityType(typeof(CommentEntity))!;
		var expected = new[] { "AuthorId" };

		// Act
		var indexes = comment.GetIndexes();

		// Assert
		Assert.Contains(indexes, index => index.Properties.Select(p => p.Name).SequenceEqual(expected));
	}

	[Fact]
	public void Model_CommentReport_HasCommentResolvedOnIndex()
	{
		// Arrange
		using var context = CreateContext();
		var report = context.Model.FindEntityType(typeof(CommentReportEntity))!;
		var expected = new[] { "CommentId", "ResolvedOn" };

		// Act
		var indexes = report.GetIndexes();

		// Assert
		Assert.Contains(indexes, index => index.Properties.Select(p => p.Name).SequenceEqual(expected));
	}

	[Fact]
	public void Model_CommentVote_HasCompositePrimaryKey()
	{
		// Arrange
		using var context = CreateContext();
		var vote = context.Model.FindEntityType(typeof(CommentVoteEntity))!;

		// Act
		var keyProperties = vote.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray();

		// Assert
		Assert.Equal(2, keyProperties.Length);
		Assert.Contains("CommentId", keyProperties);
		Assert.Contains("AuthorId", keyProperties);
	}

	[Fact]
	public void Model_CommentRelationships_AllRestrictDelete()
	{
		// Arrange
		using var context = CreateContext();
		var comment = context.Model.FindEntityType(typeof(CommentEntity))!;
		var vote = context.Model.FindEntityType(typeof(CommentVoteEntity))!;
		var report = context.Model.FindEntityType(typeof(CommentReportEntity))!;

		// Act
		var foreignKeys = comment.GetForeignKeys()
			.Concat(vote.GetForeignKeys())
			.Concat(report.GetForeignKeys())
			.ToArray();

		// Assert
		Assert.Equal(3, comment.GetForeignKeys().Count());
		Assert.Equal(2, vote.GetForeignKeys().Count());
		Assert.Equal(2, report.GetForeignKeys().Count());
		Assert.All(foreignKeys, foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
	}

	[Fact]
	public void Model_CommentEnums_AreStoredAsIntColumns()
	{
		// Arrange
		using var context = CreateContext();
		var comment = context.Model.FindEntityType(typeof(CommentEntity))!;
		var report = context.Model.FindEntityType(typeof(CommentReportEntity))!;

		// Act
		var statusColumnType = comment.FindProperty(nameof(CommentEntity.Status))!.GetColumnType();
		var reasonColumnType = report.FindProperty(nameof(CommentReportEntity.Reason))!.GetColumnType();

		// Assert
		Assert.Equal("int", statusColumnType);
		Assert.Equal("int", reasonColumnType);
	}
}
