using MediatR;
using MZikmund.DataContracts.Videos;

namespace MZikmund.Web.Core.Features.Videos.Queries;

/// <summary>
/// Query to fetch videos from YouTube RSS feed (with caching).
/// </summary>
public class GetVideosQuery : IRequest<List<VideoDto>?>
{
	/// <summary>
	/// Optional: limit to the N most recent videos.
	/// If null, returns all available videos.
	/// </summary>
	public int? Count { get; set; }
}
