namespace MZikmund.Business.Models;

public record AppConfig
{
	public string? Environment { get; init; }

	public string? ApiUrl { get; init; }

	public string? WebUrl { get; init; }
}
