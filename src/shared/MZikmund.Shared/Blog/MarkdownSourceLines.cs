namespace MZikmund.Web.Core.Services;

public static class MarkdownSourceLines
{
	/// <summary>
	/// Zero-based source line of a caret position, counting \r\n, \r and \n as one line break each, like Markdig.
	/// </summary>
	public static int LineAt(string text, int index)
	{
		ArgumentNullException.ThrowIfNull(text);

		var end = Math.Clamp(index, 0, text.Length);
		var line = 0;
		for (var i = 0; i < end; i++)
		{
			if (text[i] == '\n' || (text[i] == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
			{
				line++;
			}
		}

		return line;
	}
}
