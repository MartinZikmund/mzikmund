using System.Globalization;
using MZikmund.Services.Localization;

namespace MZikmund.ViewModels.Items;

public static class PublishCountdownText
{
	public static string Format(TimeSpan remaining)
	{
		// Rounded up, so it never reads zero while the post is still pending.
		var time = TimeSpan.FromSeconds(Math.Ceiling(remaining.TotalSeconds));
		var (key, first, second) = time switch
		{
			{ Days: > 0 } => ("CountdownDays", time.Days, time.Hours),
			{ Hours: > 0 } => ("CountdownHours", time.Hours, time.Minutes),
			{ Minutes: > 0 } => ("CountdownMinutes", time.Minutes, time.Seconds),
			_ => ("CountdownSeconds", time.Seconds, 0),
		};

		var countdown = string.Format(CultureInfo.CurrentCulture, Localizer.Instance.GetString(key), first, second);
		return string.Format(CultureInfo.CurrentCulture, Localizer.Instance.GetString("GoesLiveInFormat"), countdown);
	}
}
