using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Tests.Infrastructure;

public class CommentsDbFixtureTests
{
	[Fact]
	public async Task CreateContext_ForANewFixture_StartsWithAnEmptySchema()
	{
		// Arrange
		using CommentsDbFixture fixture = new();

		// Act
		await using var context = fixture.CreateContext();
		var count = await context.Tag.CountAsync(CancellationToken.None);

		// Assert
		Assert.Equal(0, count);
	}

	[Fact]
	public async Task CreateContext_RoundTripsAnEntityAcrossTwoContexts()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		var tagId = new Guid("11111111-1111-1111-1111-111111111111");

		// Act
		await using (var writeContext = fixture.CreateContext())
		{
			writeContext.Tag.Add(new TagEntity
			{
				Id = tagId,
				DisplayName = "Uno Platform",
				RouteName = "uno-platform"
			});
			await writeContext.SaveChangesAsync(CancellationToken.None);
		}

		TagEntity? roundTripped;
		await using (var readContext = fixture.CreateContext())
		{
			roundTripped = await readContext.Tag
				.SingleOrDefaultAsync(tag => tag.Id == tagId, CancellationToken.None);
		}

		// Assert
		Assert.NotNull(roundTripped);
		Assert.Equal("Uno Platform", roundTripped!.DisplayName);
		Assert.Equal("uno-platform", roundTripped.RouteName);
	}

	[Fact]
	public async Task CreateContext_ForTwoFixtures_UsesIsolatedDatabases()
	{
		// Arrange
		using CommentsDbFixture first = new();
		using CommentsDbFixture second = new();

		await using (var writeContext = first.CreateContext())
		{
			writeContext.Tag.Add(new TagEntity
			{
				Id = new Guid("22222222-2222-2222-2222-222222222222"),
				DisplayName = "Only In The First Database",
				RouteName = "only-in-the-first-database"
			});
			await writeContext.SaveChangesAsync(CancellationToken.None);
		}

		// Act
		await using var secondContext = second.CreateContext();
		var count = await secondContext.Tag.CountAsync(CancellationToken.None);

		// Assert
		Assert.Equal(0, count);
	}

	[Fact]
	public async Task CreateExecutionStrategy_WrapsAnExplicitTransaction()
	{
		// Arrange
		// Phase 2's comment handlers write a comment and its denormalized counts in one
		// transaction, and the production context is configured with EnableRetryOnFailure, which
		// throws unless the transaction is wrapped in an execution strategy. This proves the
		// fixture supports that exact shape.
		using CommentsDbFixture fixture = new();
		await using var context = fixture.CreateContext();
		var strategy = context.Database.CreateExecutionStrategy();

		// Act
		await strategy.ExecuteAsync(async () =>
		{
			await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

			context.Tag.Add(new TagEntity
			{
				Id = new Guid("33333333-3333-3333-3333-333333333333"),
				DisplayName = "Transactional",
				RouteName = "transactional"
			});
			await context.SaveChangesAsync(CancellationToken.None);

			await transaction.CommitAsync(CancellationToken.None);
		});

		// Assert
		Assert.Equal(1, await context.Tag.CountAsync(CancellationToken.None));
	}
}
