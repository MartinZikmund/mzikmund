using MZikmund.DataContracts.Extensions;

namespace MZikmund.DataContracts.Blobs;

public record ImageVariant(string Label, Uri Url, uint? Width = null, long Size = 0)
{
	public string DisplayText
	{
		get
		{
			var parts = new List<string> { Label };

			if (Width.HasValue)
			{
				parts.Add($"{Width}px");
			}

			if (Size > 0)
			{
				parts.Add(Size.ToFileSizeString());
			}

			return parts.Count > 1 ? $"{parts[0]} ({string.Join(", ", parts.Skip(1))})" : parts[0];
		}
	}
}
