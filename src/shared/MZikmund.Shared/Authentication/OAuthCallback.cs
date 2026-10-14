namespace MZikmund.Shared.Authentication;

public sealed record OAuthCallbackResult(string? Code, string? Error, string? ErrorDescription = null);

public static class OAuthCallback
{
	/// <param name="expectedState">The state sent with the request, or null when the provider does not echo one (GoTrue).</param>
	public static OAuthCallbackResult Parse(Uri callbackUri, string? expectedState)
	{
		var query = ParseQuery(callbackUri.Query);

		// GoTrue reports some failures in the fragment instead of the query.
		var fragment = ParseQuery(callbackUri.Fragment.TrimStart('#'));

		if (query.TryGetValue("error", out var error) || fragment.TryGetValue("error", out error))
		{
			if (!query.TryGetValue("error_description", out var description))
			{
				fragment.TryGetValue("error_description", out description);
			}

			return new(null, error, description);
		}

		if (expectedState is not null && (!query.TryGetValue("state", out var state) || state != expectedState))
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
