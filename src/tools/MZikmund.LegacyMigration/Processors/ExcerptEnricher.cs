using System.Diagnostics;
using System.Text.Json;
using MZikmund.LegacyMigration.Json;
using MZikmund.Web.Data.Entities;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace MZikmund.LegacyMigration.Processors;

public class ExcerptEnricher
{
	private readonly OpenAIService _openAIService;

	public ExcerptEnricher(string openAiApiKey)
	{
		_openAIService = new OpenAIService(new OpenAiOptions() { ApiKey = openAiApiKey });
	}

	internal async Task EnrichAsync(
		IList<Post> posts,
		IList<Term> terms,
		IList<TermRelationship> termRelationships,
		IList<TermTaxonomy> termTaxonomies)
	{
		var tempFile = Path.GetTempFileName();
		Debug.WriteLine(tempFile);
		foreach (var post in posts.Where(p => p.PostType is "post" or "revision" && p.PostStatus == "publish" && string.IsNullOrEmpty(p.PostExcerpt)))
		{
			string language = "en-US";
			foreach (var termTaxonomy in termRelationships.Where(t => t.ObjectId == post.Id).Select(tr => termTaxonomies.Single(t => t.TermTaxonomyId == tr.TermTaxonomyId)))
			{
				if (termTaxonomy.Taxonomy.Equals("language", StringComparison.OrdinalIgnoreCase))
				{
					var languageCode = terms.Single(t => t.TermId == termTaxonomy.TermId);
					language = FromWordpressLanguage(languageCode.Name);
				}
			}
			await EnrichAsync(post, language);

			File.WriteAllText(tempFile, JsonSerializer.Serialize(posts));
		}
	}

	private string FromWordpressLanguage(string language)
	{
		switch (language.ToLowerInvariant())
		{
			case "cs_cz": return "Czech";
			case "en_us": return "US English";
			default: throw new InvalidOperationException("Invalid language");
		}
	}

	private async Task EnrichAsync(Post post, string language)
	{
		try
		{
			var completionRequest = new ChatCompletionCreateRequest
			{
				Messages = new ChatMessage[] {
				new ChatMessage(
					"user",
					"Given the blog post text following this description, please generate a short excerpt (max 280 characters) in ***" + language + "*** language. " +
					"Make it interesting so that the reader wants to read the full article, but avoid literal prompts like \"Check out the full article!\" or \"Read the full article\", keep it informative. Avoid giving away spoilers too much! " +
					"Ensure to use the same tone and style as the blog post. Write the response as plain text, with optional Markdown syntax (for code, emphasis for example). Avoid wrapping the response in double quotes. " +
					"Here is the blog post: \n\n" + post.PostContent)
			}
			};
			var completionResult = await _openAIService.ChatCompletion.CreateCompletion(completionRequest, Models.Gpt_3_5_Turbo_16k);
			if (completionResult.Successful)
			{
				post.PostExcerpt = completionResult.Choices.First().Message.Content;
			}
			else
			{
				post.PostExcerpt = "";
			}
		}
		catch
		{
			post.PostExcerpt = "";
		}
	}
}
