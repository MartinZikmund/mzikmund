using System.Collections.Immutable;
using MZikmund.App.DataContracts;

namespace MZikmund.App.Services.Caching
{
	public interface IWeatherCache
	{
		ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
	}
}