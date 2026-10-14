using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MZikmund.Web.Core.Tests.Infrastructure;

/// <summary>
/// Lets a test play the part of a concurrent request: once, right after the first query whose SQL
/// contains <c>fragment</c> has run, it executes <c>competitor</c> (typically another context
/// committing a write). The handler under test then acts on state that is already stale.
/// </summary>
public sealed class RaceInterceptor : DbCommandInterceptor
{
	private readonly string _fragment;
	private readonly Func<Task> _competitor;
	private bool _fired;

	public RaceInterceptor(string fragment, Func<Task> competitor)
	{
		_fragment = fragment;
		_competitor = competitor;
	}

	public bool Fired => _fired;

	public override async ValueTask<DbDataReader> ReaderExecutedAsync(
		DbCommand command,
		CommandExecutedEventData eventData,
		DbDataReader result,
		CancellationToken cancellationToken = default)
	{
		if (!_fired && command.CommandText.Contains(_fragment, StringComparison.Ordinal))
		{
			_fired = true;
			await _competitor();
		}

		return result;
	}
}
