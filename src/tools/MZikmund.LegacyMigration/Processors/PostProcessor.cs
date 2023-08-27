using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.IdentityModel.Protocols;
using MZikmund.LegacyMigration.Json;
using MZikmund.Web.Data.Entities;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;

namespace MZikmund.LegacyMigration.Processors;

internal sealed class PostProcessor
{
	private readonly IList<Post> _posts;
	private readonly IList<PostMeta> _postMetas;
	private readonly IList<Term> _terms;
	private readonly IList<TermRelationship> _termRelationships;
	private readonly IList<TermTaxonomy> _termTaxonomies;
	private readonly IDictionary<long, TagEntity> _tags;
	private readonly IDictionary<long, CategoryEntity> _categories;

	private readonly CultureInfo _czechLanguage = new CultureInfo("cs-CZ");
	private readonly CultureInfo _englishLanguage = new CultureInfo("en-US");

	private readonly ReverseMarkdown.Converter _reverseMarkdownConverter = new(new ReverseMarkdown.Config() { DefaultCodeBlockLanguage = "csharp", GithubFlavored = true });

	public PostProcessor(
		IList<Post> posts,
		IList<PostMeta> postMetas,
		IList<Term> terms,
		IList<TermRelationship> termRelationships,
		IList<TermTaxonomy> termTaxonomies,
		IDictionary<long, TagEntity> tags,
		IDictionary<long, CategoryEntity> categories)
	{
		_posts = posts;
		_postMetas = postMetas;
		_terms = terms;
		_termRelationships = termRelationships;
		_termTaxonomies = termTaxonomies;
		_tags = tags;
		_categories = categories;
	}

	public ProcessedPosts Process()
	{
		var posts = new Dictionary<long, PostEntity>();
		var postCategories = new List<PostCategoryEntity>();
		var postTags = new List<PostTagEntity>();
		foreach (var post in _posts.Where(p => p.PostType is "post" or "revision" && p.PostStatus == "publish"))
		{
			var metas = _postMetas.Where(p => p.PostId == post.Id).ToArray();
			var terms = _termRelationships
				.Where(tr => tr.ObjectId == post.Id)
				.Select(tr => _termTaxonomies.Single(t => t.TermTaxonomyId == tr.TermTaxonomyId))
				.Select(tx => (tx, _terms.Single(t => t.TermId == tx.TermId)))
				.ToArray();

			var content = FromWordpressContent(post.PostContent);
			var excerpt = _reverseMarkdownConverter.Convert(post.PostExcerpt);
			var postEntity = new PostEntity()
			{
				Id = Guid.NewGuid(),
				Title = post.PostTitle,
				Content = content,
				CreatedDate = FromWordpressDate(post.PostDateGmt),
				LastModifiedDate = FromWordpressDate(post.PostModifiedGmt),
				PublishedDate = FromWordpressDate(post.PostDateGmt),
				RouteName = post.PostName,
				Status = PostStatus.Published,
				Abstract = excerpt
			};
			//postEntity.Tags = _termRelationships
			//	.Where(tr => tr.ObjectId == post.ID)
			//	.Select(tr => _tags[tr.TermTaxonomyId])
			//	.Select(t => new TagEntity() { Name = t.Name })
			//	.ToList();

			foreach (var termTaxonomy in _termRelationships.Where(t => t.ObjectId == post.Id).Select(tr => _termTaxonomies.Single(t => t.TermTaxonomyId == tr.TermTaxonomyId)))
			{
				if (termTaxonomy.Taxonomy.Equals("category", StringComparison.OrdinalIgnoreCase))
				{
					var categoryEntity = _categories[termTaxonomy.TermId];
					postCategories.Add(new()
					{
						CategoryId = categoryEntity.Id,
						PostId = postEntity.Id
					});
				}
				else if (termTaxonomy.Taxonomy.Equals("post_tag", StringComparison.OrdinalIgnoreCase))
				{
					var tagEntity = _tags[termTaxonomy.TermId];
					postTags.Add(new()
					{
						TagId = tagEntity.Id,
						PostId = postEntity.Id
					});
				}
				else if (termTaxonomy.Taxonomy.Equals("language", StringComparison.OrdinalIgnoreCase))
				{
					var language = _terms.Single(t => t.TermId == termTaxonomy.TermId);
					postEntity.LanguageCode = FromWordpressLanguage(language.Name);
				}
			}

			if (_postMetas.FirstOrDefault(pm => pm.PostId == post.Id && pm.MetaKey == "mtm_data") is { } heroImageInfo)
			{
				// Extracting twitter:image value
				string twitterImagePattern = @"twitter:image"";s:7:""content"";s:\d+:""([^""]+)""";
				string ogImagePattern = @"og:image"";s:7:""content"";s:\d+:""([^""]+)""";
				Match twitterImageMatch = Regex.Match(heroImageInfo.MetaValue, twitterImagePattern);
				Match ogImageMatch = Regex.Match(heroImageInfo.MetaValue, ogImagePattern);

				// Extracting twitter:image:alt value
				string twitterImageAltPattern = @"twitter:image:alt"";s:7:""content"";s:\d+:""([^""]+)""";
				Match twitterImageAltMatch = Regex.Match(heroImageInfo.MetaValue, twitterImageAltPattern);

				if (twitterImageMatch.Success || ogImageMatch.Success)
				{
					postEntity.HeroImageUrl = twitterImageMatch.Success ? twitterImageMatch.Groups[1].Value : ogImageMatch.Groups[1].Value;
					if (twitterImageAltMatch.Success)
					{
						postEntity.HeroImageAlt = twitterImageAltMatch.Groups[1].Value;
					}
				}
			}

			posts.Add(post.Id, postEntity);
		}
		return new ProcessedPosts(posts, postCategories, postTags, null);
	}

	private DateTimeOffset? FromWordpressDate(string date)
	{
		if (date == "0000-00-00 00:00:00")
		{
			return null;
		}

		return DateTimeOffset.Parse(date, CultureInfo.InvariantCulture);
	}

	private string FromWordpressLanguage(string language)
	{
		switch (language.ToLowerInvariant())
		{
			case "cs_cz": return _czechLanguage.TwoLetterISOLanguageName;
			case "en_us": return _englishLanguage.TwoLetterISOLanguageName;
			default: throw new InvalidOperationException("Invalid language");
		}
	}

	private string FromWordpressContent(string content)
	{
		// TODO: Convert Gist links to custom extension
		// TODO: Convert image URLs to new storage
		content = ReplaceNonBreakingSpaces(content);
		content = AdjustSpacing(content);
		content = _reverseMarkdownConverter.Convert(content);
		content = SpaceImages(content);
		content = FormatYouTuBeEmbeds(content);
		content = FormatYouTubeComEmbeds(content);
		// TODO: Proess galleries
		return TransformWordpressCaption(content);
	}

	private string TransformWordpressCaption(string content)
	{
		// Define the patterns
		string captionWithLinkPattern = @"\[caption id=""[^""]*"" align=""[^""]*"" width=""[^""]*""\]\[(?<imageLinkWithOuterLink>!\[.*?\]\(.*?\))\]\((?<outerLink>.*?)\) (?<captionContent>.*?)\[/caption\]";
		string captionWithoutLinkPattern = @"\[caption id=""[^""]*"" align=""[^""]*"" width=""[^""]*""\](?<imageLink>!\[.*?\]\(.*?\)) (?<captionContent>.*?)\[/caption\]";

		// Transformation logic for captions with links
		string TransformWithLink(Match match)
		{
			var imageLinkWithOuterLink = match.Groups["imageLinkWithOuterLink"].Value;
			var outerLink = match.Groups["outerLink"].Value;
			var captionContent = match.Groups["captionContent"].Value;

			return $"\n^^^\n[{imageLinkWithOuterLink.Trim()}]({outerLink.Trim()})\n^^^ {captionContent}\n";
		}

		// Transformation logic for captions without links
		string TransformWithoutLink(Match match)
		{
			var imageLink = match.Groups["imageLink"].Value;
			var captionContent = match.Groups["captionContent"].Value;

			return $"\n^^^\n{imageLink.Trim()}\n^^^ {captionContent}\n";
		}

		// Apply the transformations
		var transformedContent = Regex.Replace(content, captionWithLinkPattern, TransformWithLink);
		transformedContent = Regex.Replace(transformedContent, captionWithoutLinkPattern, TransformWithoutLink);

		return transformedContent;
	}

	public string ReplaceNonBreakingSpaces(string s)
	{
		return s.Replace("\u00A0", " ");
	}

	private string AdjustSpacing(string content)
	{
		// Move spaces outside for opening tags
		content = Regex.Replace(content, @"(<(?:strong|em|b|i)>) ", " $1");

		// Move spaces outside for closing tags
		content = Regex.Replace(content, @" (</(?:strong|em|b|i)>)", "$1 ");

		return content;
	}

	private string SpaceImages(string content)
	{
		// Regular expression to match markdown images surrounded by pair of spaces
		string pattern = @"  \!\[.*?\]\(.*?\)  ";

		// Replace the matched pattern with newlines
		return Regex.Replace(content, pattern, m => "\n\n" + m.Value.Trim() + "\n\n");
	}

	public static string FormatYouTuBeEmbeds(string input)
	{
		// Regular expression pattern to match '  youtu.be/*link*  ' with two spaces on both sides
		string pattern = @"  (https://youtu\.be/[^\s]+)  ";

		// Replacing the found links with the desired format
		string formattedText = Regex.Replace(input, pattern, "\n\n![youtu.be]($1)\n\n");

		return formattedText;
	}

	public static string FormatYouTubeComEmbeds(string input)
	{
		// Regular expression pattern to match '  youtu.be/*link*  ' with two spaces on both sides
		string pattern = @"  (https://youtube\.com/[^\s]+)  ";

		// Replacing the found links with the desired format
		string formattedText = Regex.Replace(input, pattern, "\n\n![youtube.com]($1)\n\n");

		return formattedText;
	}
}
