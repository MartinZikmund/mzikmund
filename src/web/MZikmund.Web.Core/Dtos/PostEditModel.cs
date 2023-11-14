using System.ComponentModel.DataAnnotations;

namespace MZikmund.Web.Core.Dtos;

public class PostEditModel
{
	public Guid Id { get; set; } = Guid.Empty;

	[Required]
	[RegularExpression(@"[a-z0-9\-]+")]
	[MaxLength(256)]
	public string RouteName { get; set; } = null!;

	[Required]
	[MaxLength(256)]
	public string Title { get; set; } = null!;

	public Guid[] CategoryIds { get; set; } = null!;

	[MaxLength(512)]
	public string Tags { get; set; } = null!;

	[Required]
	[DataType(DataType.MultilineText)]
	[MaxLength(2048)]
	public string Abstract { get; set; } = null!;

	[Required]
	[MaxLength(10000)]
	[DataType(DataType.MultilineText)]
	public string Content { get; set; } = null!;

	[Required]
	public bool IsPublished { get; set; }

	[Required]
	[RegularExpression("^[a-z]{2}-[a-zA-Z]{2,4}$")]
	public string LanguageCode { get; set; } = null!;

	[DataType(DataType.Date)]
	public DateTime? PublishedDate { get; set; }

	public bool ChangePublishDate { get; set; }

	[DataType(DataType.Url)]
	public string HeroImageUrl { get; set; } = null!;

	public string HeroImageAlt { get; set; } = null!;
}
