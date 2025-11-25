using System.Collections.Immutable;
using System.Text.Json.Serialization;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using MZikmund.DataContracts.Blog;
using Refit;

namespace MZikmund.Api.Serialization;

/// <summary>
/// Generated class for System.Text.Json Serialization for MZikmund API models
/// </summary>
/// <remarks>
/// When using the JsonSerializerContext you must add the JsonSerializableAttribute
/// for each type that you may need to serialize / deserialize including both the
/// concrete type and any interface that the concrete type implements.
/// For more information on the JsonSerializerContext see:
/// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?WT.mc_id=DT-MVP-5002924
/// </remarks>
// Post types
[JsonSerializable(typeof(Post))]
[JsonSerializable(typeof(Post[]))]
[JsonSerializable(typeof(IEnumerable<Post>))]
[JsonSerializable(typeof(IImmutableList<Post>))]
[JsonSerializable(typeof(ImmutableList<Post>))]
[JsonSerializable(typeof(ApiResponse<Post>))]

// PostListItem types
[JsonSerializable(typeof(PostListItem))]
[JsonSerializable(typeof(PostListItem[]))]
[JsonSerializable(typeof(IEnumerable<PostListItem>))]
[JsonSerializable(typeof(IImmutableList<PostListItem>))]
[JsonSerializable(typeof(ImmutableList<PostListItem>))]
[JsonSerializable(typeof(PagedResponse<PostListItem>))]
[JsonSerializable(typeof(ApiResponse<PagedResponse<PostListItem>>))]

// Category types
[JsonSerializable(typeof(Category))]
[JsonSerializable(typeof(Category[]))]
[JsonSerializable(typeof(IEnumerable<Category>))]
[JsonSerializable(typeof(IImmutableList<Category>))]
[JsonSerializable(typeof(ImmutableList<Category>))]
[JsonSerializable(typeof(ApiResponse<Category>))]
[JsonSerializable(typeof(ApiResponse<Category[]>))]

// CategoryWithPostCount types
[JsonSerializable(typeof(CategoryWithPostCount))]
[JsonSerializable(typeof(CategoryWithPostCount[]))]
[JsonSerializable(typeof(IEnumerable<CategoryWithPostCount>))]
[JsonSerializable(typeof(IImmutableList<CategoryWithPostCount>))]
[JsonSerializable(typeof(ImmutableList<CategoryWithPostCount>))]

// Tag types
[JsonSerializable(typeof(Tag))]
[JsonSerializable(typeof(Tag[]))]
[JsonSerializable(typeof(IEnumerable<Tag>))]
[JsonSerializable(typeof(IImmutableList<Tag>))]
[JsonSerializable(typeof(ImmutableList<Tag>))]
[JsonSerializable(typeof(ApiResponse<Tag>))]
[JsonSerializable(typeof(ApiResponse<Tag[]>))]

// TagWithPostCount types
[JsonSerializable(typeof(TagWithPostCount))]
[JsonSerializable(typeof(TagWithPostCount[]))]
[JsonSerializable(typeof(IEnumerable<TagWithPostCount>))]
[JsonSerializable(typeof(IImmutableList<TagWithPostCount>))]
[JsonSerializable(typeof(ImmutableList<TagWithPostCount>))]

// Storage items
[JsonSerializable(typeof(StorageItemInfo))]

// Common types
[JsonSerializable(typeof(ApiResponse<object?>))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(DateTimeOffset?))]

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.Never,
	WriteIndented = false)]
public partial class MZikmundApiSerializerContext : JsonSerializerContext
{
}
