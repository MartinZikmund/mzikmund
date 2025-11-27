namespace MZikmund.DataContracts.Extensions;

/// <summary>
/// Extension methods for formatting file sizes.
/// </summary>
public static class FileSizeExtensions
{
	/// <summary>
	/// Formats a file size in bytes to a human-readable string (B, KB, MB, GB).
	/// </summary>
	/// <param name="bytes">The file size in bytes.</param>
	/// <returns>A formatted string representation of the file size.</returns>
	public static string ToFileSizeString(this long bytes)
	{
		string[] sizes = { "B", "KB", "MB", "GB" };
		double len = bytes;
		int order = 0;
		while (len >= 1024 && order < sizes.Length - 1)
		{
			order++;
			len = len / 1024;
		}
		return $"{len:0.##} {sizes[order]}";
	}
}
