using System.Net;

namespace MZikmund.Shared.Authentication;

public class SupabaseAuthException : Exception
{
	public SupabaseAuthException(string message, HttpStatusCode statusCode)
		: base(message)
	{
		StatusCode = statusCode;
	}

	public HttpStatusCode StatusCode { get; }

	/// <summary>
	/// True when GoTrue refused the code, token or OTP (HTTP 4xx); false for server errors.
	/// </summary>
	public bool IsRejected => (int)StatusCode is >= 400 and < 500;
}
