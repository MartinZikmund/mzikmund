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
	/// True when GoTrue refused the code, token or OTP (400, 401 or 403). Rate limits (429), timeouts (408)
	/// and server errors are transient and not a rejection.
	/// </summary>
	public bool IsRejected => StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
}
