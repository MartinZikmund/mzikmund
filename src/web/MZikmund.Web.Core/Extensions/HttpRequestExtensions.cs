using Microsoft.AspNetCore.Http;

public static class HttpRequestExtensions
{
	public static string? GetBaseUrl(this HttpRequest req)
	{
		if (req == null)
		{
			return null;
		}

		var uriBuilder = new UriBuilder(req.Scheme, req.Host.Host, req.Host.Port ?? -1);
		if (uriBuilder.Uri.IsDefaultPort)
		{
			uriBuilder.Port = -1;
		}

		var fullUri = uriBuilder.Uri.AbsoluteUri;
		return fullUri.TrimEnd('/');
	}
}
