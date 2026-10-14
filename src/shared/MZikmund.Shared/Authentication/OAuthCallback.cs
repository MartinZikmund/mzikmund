namespace MZikmund.Shared.Authentication;

public sealed record OAuthCallbackResult(string? Code, string? Error, string? ErrorDescription = null);

public static class OAuthCallback
{
	public static OAuthCallbackResult Parse(Uri callbackUri, string expectedState)
	{
		var query = ParseQuery(callbackUri.Query);

		if (query.TryGetValue("error", out var error))
		{
			query.TryGetValue("error_description", out var description);
			return new(null, error, description);
		}

		if (!query.TryGetValue("state", out var state) || state != expectedState)
		{
			return new(null, "state_mismatch");
		}

		return query.TryGetValue("code", out var code) && !string.IsNullOrEmpty(code) ?
			new(code, null) :
			new(null, "missing_code");
	}

	private static Dictionary<string, string> ParseQuery(string query)
	{
		Dictionary<string, string> result = new(StringComparer.Ordinal);
		foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
			var key = separatorIndex < 0 ? pair : pair[..separatorIndex];
			var value = separatorIndex < 0 ? "" : pair[(separatorIndex + 1)..];
			result[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value.Replace('+', ' '));
		}

		return result;
	}
}
