using MZikmund.DataContracts.Videos;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/videos")]
	Task<ApiResponse<List<VideoDto>>> GetVideosAsync();

	[Get("/v1/videos/latest")]
	Task<ApiResponse<List<VideoDto>>> GetLatestVideosAsync([AliasAs("count")] int? count = null);
}
