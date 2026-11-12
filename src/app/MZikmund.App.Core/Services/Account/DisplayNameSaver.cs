using System.Globalization;
using System.Net;
using System.Text.Json;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Services.Localization;
using Refit;

namespace MZikmund.Services.Account;

/// <summary>
/// Validates a display name and saves it, turning API failures into a message for the user.
/// </summary>
public static class DisplayNameSaver
{
	public static string? Validate(string? name)
	{
		var length = name?.Trim().Length ?? 0;
		return length is >= CommentLimits.MinDisplayNameLength and <= CommentLimits.MaxDisplayNameLength ?
			null :
			string.Format(
				CultureInfo.CurrentCulture,
				Localizer.Instance.GetString("DisplayNameLengthFormat"),
				CommentLimits.MinDisplayNameLength,
				CommentLimits.MaxDisplayNameLength);
	}

	/// <summary>
	/// Returns null on success, otherwise the message to show.
	/// </summary>
	public static async Task<string?> SaveAsync(IMZikmundApi api, string name)
	{
		if (Validate(name) is { } invalid)
		{
			return invalid;
		}

		try
		{
			var response = await api.UpdateMyCommentProfileAsync(new UpdateCommentProfileRequest { DisplayName = name.Trim() });
			await response.EnsureSuccessfulAsync();
			return null;
		}
		catch (ApiException ex)
		{
			return Describe(ex);
		}
		catch (Exception ex) when (ex is HttpRequestException or AuthenticationFailedException or TaskCanceledException)
		{
			return ex is AuthenticationFailedException ? ex.Message : Localizer.Instance.GetString("CommentGenericError");
		}
	}

	private static string Describe(ApiException ex) => ex.StatusCode switch
	{
		HttpStatusCode.TooManyRequests => Localizer.Instance.GetString("DisplayNameRateLimited"),
		HttpStatusCode.BadRequest => ReadProblem(ex.Content, "detail") ?? Localizer.Instance.GetString(
			ReadProblem(ex.Content, "code") == CommentErrorCodes.DisplayNameReserved ? "DisplayNameReserved" : "DisplayNameInvalid"),
		HttpStatusCode.Forbidden => Localizer.Instance.GetString("CommentBanned"),
		_ => Localizer.Instance.GetString("CommentGenericError"),
	};

	private static string? ReadProblem(string? body, string property)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return null;
		}

		try
		{
			using var document = JsonDocument.Parse(body);
			if (document.RootElement.ValueKind == JsonValueKind.Object &&
				document.RootElement.TryGetProperty(property, out var value) &&
				value.ValueKind == JsonValueKind.String &&
				!string.IsNullOrWhiteSpace(value.GetString()))
			{
				return value.GetString();
			}
		}
		catch (JsonException)
		{
		}

		return null;
	}
}
