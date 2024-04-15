namespace MZikmund.Services.Preferences;

public interface IAppPreferences
{
	AppTheme Theme { get; set; }

	string? AccessToken { get; set; }
}
