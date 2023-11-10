// Based on https://github.com/EdiWang/Moonglade/blob/cf5571b0db09c7722b310ca9922cdcd542e93a51/src/Moonglade.Syndication/FeedGenerator.cs

using Edi.SyndicationFeed.ReaderWriter;
using Edi.SyndicationFeed.ReaderWriter.Atom;
using Edi.SyndicationFeed.ReaderWriter.Rss;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Xml;

namespace MZikmund.Web.Core.Syndication;

public class FeedGenerator : IFeedGenerator
{
	public FeedGenerator(IHttpContextAccessor httpContextAccessor)
	{
		//TODO: Move all values into configuration
		var baseUrl = httpContextAccessor.HttpContext.Request.GetBaseUrl();
		HostUrl = baseUrl!;
		HeadTitle = "Martin Zikmund";
		HeadDescription = "Open-source enthusiast and Microsoft MVP. Passionate speaker, avid climber, and Lego aficionado.";
		Copyright = "©2023 Martin Zikmund";
		Generator = "MZikmund";
		GeneratorVersion = "0.1";
		TrackBackUrl = baseUrl!;
		Language = "en-US";
	}

	//TODO: Hide properties, use a private feed configuration class instead
	public string HostUrl { get; set; }

	public string HeadTitle { get; set; }

	public string HeadDescription { get; set; }

	public string Copyright { get; set; }

	public string Generator { get; set; }

	public string TrackBackUrl { get; set; }

	public string GeneratorVersion { get; set; }

	public string Language { get; set; }

	public async Task<string> GetRssAsync(IEnumerable<FeedEntry> feedEntries)
	{
		// TODO: Generate additional properties (image, etc.)
		var feed = GetItemCollection(feedEntries);

		var sw = new StringWriterWithEncoding(Encoding.UTF8);
		await using (var xmlWriter = XmlWriter.Create(sw, new() { Async = true }))
		{
			var writer = new RssFeedWriter(xmlWriter);

			await writer.WriteTitle(HeadTitle);
			await writer.WriteDescription(HeadDescription);
			await writer.Write(new SyndicationLink(new(TrackBackUrl)));
			await writer.WritePubDate(DateTimeOffset.UtcNow);
			await writer.WriteCopyright(Copyright);
			await writer.WriteGenerator(Generator);
			await writer.WriteLanguage(new(Language));

			foreach (var item in feed)
			{
				await writer.Write(item);
			}

			await xmlWriter.FlushAsync();
			xmlWriter.Close();
		}

		var xml = sw.ToString();
		return xml;
	}

	public async Task<string> GetAtomAsync(IEnumerable<FeedEntry> feedEntries)
	{
		var feed = GetItemCollection(feedEntries);

		var sw = new StringWriterWithEncoding(Encoding.UTF8);
		await using (var xmlWriter = XmlWriter.Create(sw, new() { Async = true }))
		{
			var writer = new AtomFeedWriter(xmlWriter);

			await writer.WriteTitle(HeadTitle);
			await writer.WriteSubtitle(HeadDescription);
			await writer.WriteRights(Copyright);
			await writer.WriteUpdated(DateTime.UtcNow);
			await writer.WriteGenerator(Generator, HostUrl, GeneratorVersion);

			foreach (var item in feed)
			{
				await writer.Write(item);
			}

			await xmlWriter.FlushAsync();
			xmlWriter.Close();
		}

		var xml = sw.ToString();
		return xml;
	}

	private static List<SyndicationItem> GetItemCollection(IEnumerable<FeedEntry> itemCollection)
	{
		var synItemCollection = new List<SyndicationItem>();
		if (null == itemCollection) return synItemCollection;

		foreach (var item in itemCollection)
		{
			// create rss item
			var sItem = new SyndicationItem
			{
				Id = item.Id,
				Title = item.Title,
				Description = item.Description,
				LastUpdated = item.PubDate.ToUniversalTime(),
				Published = item.PubDate.ToUniversalTime()
			};

			sItem.AddLink(new SyndicationLink(new(item.Link)));

			// add author
			if (!string.IsNullOrWhiteSpace(item.Author) && !string.IsNullOrWhiteSpace(item.AuthorEmail))
			{
				sItem.AddContributor(new SyndicationPerson(item.Author, item.AuthorEmail));
			}

			// add categories
			if (item.Categories is { Length: > 0 })
			{
				foreach (var itemCategory in item.Categories)
				{
					sItem.AddCategory(new SyndicationCategory(itemCategory));
				}
			}
			synItemCollection.Add(sItem);
		}
		return synItemCollection;
	}
}

public class StringWriterWithEncoding : StringWriter
{
	public StringWriterWithEncoding(Encoding encoding)
	{
		Encoding = encoding;
	}

	public override Encoding Encoding { get; }
}
