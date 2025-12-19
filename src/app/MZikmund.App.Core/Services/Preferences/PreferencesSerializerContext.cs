using System.Text.Json.Serialization;
using MZikmund.Services.Theming;

namespace MZikmund.Services.Preferences;

/// <summary>
/// JSON serializer context for app preferences.
/// This ensures AOT compatibility for all types used in preferences serialization.
/// </summary>
[JsonSerializable(typeof(AppTheme))]
[JsonSerializable(typeof(DateTimeOffset?))]
[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.Never,
	WriteIndented = false)]
public partial class PreferencesSerializerContext : JsonSerializerContext
{
}
