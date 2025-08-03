using Microsoft.AspNetCore.Http;

namespace MZikmund.Web.Core.Features.Files;

public static class FormFileNameHelper
{
	public static string GetFileName(IFormFile formFile, string? desiredFileName = null)
	{
		if (formFile == null || formFile.Length == 0)
		{
			throw new ArgumentException("Form file is empty or null.", nameof(formFile));
		}

		if (string.IsNullOrEmpty(desiredFileName))
		{
			return formFile.FileName;
		}

		var extension = Path.GetExtension(desiredFileName);
		if (string.IsNullOrEmpty(extension))
		{
			// Append form file extension to the desired file name
			extension = Path.GetExtension(formFile.FileName);
			desiredFileName += extension;
		}

		return desiredFileName;
	}
}
