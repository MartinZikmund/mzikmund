namespace MZikmund.DataContracts.Blobs;

public record ImageVariant(string Label, string Url, uint? Width = null)
{
public string DisplayText => Width.HasValue ? $"{Label} ({Width}px)" : Label;
}
