using MZikmund.Web.Core.Features.Posts;

namespace MZikmund.Web.Core.Tests.Features.Posts;

public class GetPostsQueryTests
{
	[Fact]
	public void GetPostsQuery_WithSearchTerm_StoresSearchTerm()
	{
		// Arrange & Act
		var searchTerm = "test search";
		var query = new GetPostsQuery(1, 10, SearchTerm: searchTerm);

		// Assert
		Assert.Equal(searchTerm, query.SearchTerm);
		Assert.Equal(1, query.Page);
		Assert.Equal(10, query.PageSize);
	}

	[Fact]
	public void GetPostsQuery_WithoutSearchTerm_HasNullSearchTerm()
	{
		// Arrange & Act
		var query = new GetPostsQuery(1, 10);

		// Assert
		Assert.Null(query.SearchTerm);
		Assert.Equal(1, query.Page);
		Assert.Equal(10, query.PageSize);
	}

	[Fact]
	public void CountPostsQuery_WithSearchTerm_StoresSearchTerm()
	{
		// Arrange & Act
		var searchTerm = "test search";
		var query = new CountPostsQuery(SearchTerm: searchTerm);

		// Assert
		Assert.Equal(searchTerm, query.SearchTerm);
	}

	[Fact]
	public void CountPostsQuery_WithoutSearchTerm_HasNullSearchTerm()
	{
		// Arrange & Act
		var query = new CountPostsQuery();

		// Assert
		Assert.Null(query.SearchTerm);
	}
}
