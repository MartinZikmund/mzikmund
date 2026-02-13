using MZikmund.DataContracts.Videos;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/videos")]
	Task<ApiResponse<VideoResponse>> GetVideosAsync();

	[Get("/v1/videos/latest")]
	Task<ApiResponse<VideoResponse>> GetLatestVideosAsync([AliasAs("count")] int? count = null);
}
