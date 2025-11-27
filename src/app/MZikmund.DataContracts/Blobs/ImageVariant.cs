using MZikmund.DataContracts.Extensions;

namespace MZikmund.DataContracts.Blobs;

public record ImageVariant(string Label, Uri Url, uint? Width = null, long? Size = null)
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
			
			if (Size.HasValue)
			{
				parts.Add(Size.Value.ToFileSizeString());
			}
			
			return parts.Count > 1 ? $"{parts[0]} ({string.Join(", ", parts.Skip(1))})" : parts[0];
		}
	}
}
