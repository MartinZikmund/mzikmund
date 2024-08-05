namespace MZikmund.Services.Loading;

public interface ILoadingIndicator
{
	IDisposable BeginLoading();

	bool IsLoading { get; }

	string StatusMessage { get; set; }
}
