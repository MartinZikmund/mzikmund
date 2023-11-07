using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MZikmund.Web.Core.Extensions;

public static class StringExtensions
{
	public static string RemoveDiacritics(this string text)
	{
		var normalizedString = text.Normalize(NormalizationForm.FormD);
		var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

		for (int i = 0; i < normalizedString.Length; i++)
		{
			char c = normalizedString[i];
			var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
			if (unicodeCategory != UnicodeCategory.NonSpacingMark)
			{
				stringBuilder.Append(c);
			}
		}

		return stringBuilder
			.ToString()
			.Normalize(NormalizationForm.FormC);
	}

	public static string GenerateRouteName(this string text)
	{
		string updatedText = text.RemoveDiacritics();
		// invalid chars           
		updatedText = Regex.Replace(updatedText, @"[^a-z0-9\s-]", "");
		// convert multiple spaces into one space   
		updatedText = Regex.Replace(updatedText, @"\s+", " ").Trim();

		// TODO: Shorten if necessary?
		updatedText = Regex.Replace(updatedText, @"\s", "-"); // hyphens
		return updatedText;
	}
}
