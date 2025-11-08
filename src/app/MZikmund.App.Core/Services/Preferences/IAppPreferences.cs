namespace MZikmund.Services.Preferences;

public interface IAppPreferences
{
	AppTheme Theme { get; set; }

	string? AccessToken { get; set; }
	
	string? RefreshToken { get; set; }
	
	string? UserId { get; set; }
	
	string? DisplayName { get; set; }
	
	DateTimeOffset? TokenExpiresOn { get; set; }
}
