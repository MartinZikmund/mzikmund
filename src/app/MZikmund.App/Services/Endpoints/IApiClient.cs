using System.Collections.Immutable;
using MZikmund.App.DataContracts;

namespace MZikmund.App.Services.Endpoints
{
	[Headers("Content-Type: application/json")]
	public interface IApiClient
	{
		[Get("/api/weatherforecast")]
		Task<ApiResponse<IImmutableList<WeatherForecast>>> GetWeather(CancellationToken cancellationToken = default);
	}
}