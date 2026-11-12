using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace MZikmund.Collections;

/// <summary>
/// One page handed back by the loader. A page flagged <c>IsFailure</c> leaves the page cursor where
/// it is, so a retry re-requests the same page instead of skipping it.
/// </summary>
public readonly record struct IncrementalPage<T>(IReadOnlyList<T> Items, bool HasMoreItems, bool IsFailure = false)
{
	/// <summary>
	/// Gets an empty page that stays armed, so the caller can retry the page that just failed.
	/// </summary>
	public static IncrementalPage<T> Failure { get; } = new(Array.Empty<T>(), HasMoreItems: true, IsFailure: true);
}

/// <summary>
/// An observable collection that pulls one page at a time from a loader delegate.
/// <see cref="ItemsRepeater"/> does not consume <see cref="ISupportIncrementalLoading"/> on its own,
/// so the view calls <see cref="LoadMoreItemsAsync"/> when the scroller nears the end.
/// </summary>
public sealed class IncrementalLoadingCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
{
	private readonly Func<int, CancellationToken, Task<IncrementalPage<T>>> _pageLoader;
	private CancellationTokenSource _generation = new();
	private int _nextPage = 1;
	private bool _hasMoreItems = true;
	private bool _isLoading;

	public IncrementalLoadingCollection(Func<int, CancellationToken, Task<IncrementalPage<T>>> pageLoader)
	{
		_pageLoader = pageLoader ?? throw new ArgumentNullException(nameof(pageLoader));
	}

	public bool HasMoreItems => _hasMoreItems && !_isLoading;

	/// <summary>
	/// Cancels whatever is in flight and rewinds to the first page. Safe to call mid load: the
	/// orphaned page is discarded instead of landing in the cleared collection.
	/// </summary>
	public void Reset()
	{
		var previous = _generation;
		_generation = new CancellationTokenSource();
		previous.Cancel();
		previous.Dispose();

		_isLoading = false;
		_nextPage = 1;
		_hasMoreItems = true;
		Clear();
	}

	public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) =>
		AsyncInfo.Run(LoadMoreCoreAsync);

	private async Task<LoadMoreItemsResult> LoadMoreCoreAsync(CancellationToken cancellationToken)
	{
		if (_isLoading || !_hasMoreItems)
		{
			return new LoadMoreItemsResult { Count = 0 };
		}

		var generation = _generation;
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(generation.Token, cancellationToken);
		_isLoading = true;
		try
		{
			var page = await _pageLoader(_nextPage, linked.Token);
			if (!ReferenceEquals(generation, _generation) || linked.IsCancellationRequested)
			{
				return new LoadMoreItemsResult { Count = 0 };
			}

			foreach (var item in page.Items)
			{
				Add(item);
			}

			if (!page.IsFailure)
			{
				_nextPage++;
			}

			_hasMoreItems = page.HasMoreItems;
			return new LoadMoreItemsResult { Count = (uint)page.Items.Count };
		}
		finally
		{
			if (ReferenceEquals(generation, _generation))
			{
				_isLoading = false;
			}
		}
	}
}
