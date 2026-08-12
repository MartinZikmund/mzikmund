using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Tests.Infrastructure;

/// <summary>
/// Hosts <see cref="DatabaseContext"/> on a private in-memory SQLite database, giving handler tests
/// a real relational database instead of a mocked repository.
/// </summary>
/// <remarks>
/// <para>
/// This is the repository's first database-backed test seam. Comment handlers inject
/// <see cref="DatabaseContext"/> directly rather than <c>IRepository&lt;T&gt;</c>, because that
/// abstraction calls <c>SaveChangesAsync</c> inside every write and so cannot put a comment and its
/// denormalized counts in one transaction. A mocked <c>IQueryable</c> cannot back EF's async
/// operators, so the database has to be real.
/// </para>
/// <para>
/// SQLite rather than the InMemory provider specifically because the InMemory provider does not
/// enforce keys, and the vote-idempotency tests depend on a composite primary key actually being
/// enforced.
/// </para>
/// <para>
/// Lazy-loading proxies are enabled to match the production registration in
/// <c>AddDataContext</c>. Navigations must therefore be <c>virtual</c>, exactly as in production.
/// </para>
/// <para>
/// Use it per test - <c>using CommentsDbFixture fixture = new();</c> - for complete isolation, or as
/// an <c>IClassFixture&lt;CommentsDbFixture&gt;</c> when a class wants one shared database.
/// </para>
/// </remarks>
public sealed class CommentsDbFixture : IDisposable
{
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<DatabaseContext> _options;

	public CommentsDbFixture()
	{
		// An in-memory SQLite database lives exactly as long as its connection, so this one stays
		// open for the fixture's lifetime while every DatabaseContext borrows it.
		_connection = new SqliteConnection("Filename=:memory:");
		_connection.Open();

		_options = new DbContextOptionsBuilder<DatabaseContext>()
			.UseLazyLoadingProxies()
			.UseSqlite(_connection)
			.Options;

		using var context = new DatabaseContext(_options);
		context.Database.EnsureCreated();
	}

	/// <summary>
	/// Creates a new context over this fixture's database. Callers own the returned instance.
	/// </summary>
	public DatabaseContext CreateContext() => new(_options);

	public void Dispose() => _connection.Dispose();
}
