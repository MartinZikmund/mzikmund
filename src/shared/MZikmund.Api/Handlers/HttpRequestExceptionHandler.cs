using System.Net;

namespace MZikmund.Api.Handlers;

public class HttpRequestExceptionHandler : HttpClientHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request, CancellationToken cancellationToken)
	{
		try
		{
			return await base.SendAsync(request, cancellationToken);
		}
		catch (HttpRequestException exception)
		{
			return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
			{
				Content = new StringContent(exception.Message),
				RequestMessage = request,
			};
		}
	}
}
