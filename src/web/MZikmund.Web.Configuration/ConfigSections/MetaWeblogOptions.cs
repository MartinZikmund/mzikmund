namespace MZikmund.Web.Configuration.ConfigSections;

// TODO: Add required validation to both properties
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-7.0&viewFallbackFrom=aspnetcore-2.2#options-validation

public class MetaWeblogOptions
{
	public string Username { get; set; } = "";

	public string PasswordHash { get; set; } = "";

	public string Endpoint { get; set; } = "";
}
