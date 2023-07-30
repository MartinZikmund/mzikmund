using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MZikmund.LegacyMigration.Json;
using MZikmund.Web.Data.Entities;

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

	private readonly ReverseMarkdown.Converter _reverseMarkdownConverter = new();

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

			var postEntity = new PostEntity()
			{
				Id = Guid.NewGuid(),
				Title = post.PostTitle,
				Content = FromWordpressContent(post.PostContent),
				CreatedDate = FromWordpressDate(post.PostDateGmt),
				LastModifiedDate = FromWordpressDate(post.PostModifiedGmt),
				PublishedDate = FromWordpressDate(post.PostDateGmt),
				RouteName = post.PostName,
				Status = PostStatus.Published,
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
		// TODO: Convert [caption] sections
		return _reverseMarkdownConverter.Convert(content);
	}
}
