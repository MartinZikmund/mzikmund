// Based on https://github.com/EdiWang/Moonglade/blob/cf5571b0db09c7722b310ca9922cdcd542e93a51/src/Moonglade.Syndication/FeedGenerator.cs

using Edi.SyndicationFeed.ReaderWriter.Atom;
using Microsoft.AspNetCore.Http;
using System.ServiceModel.Syndication;
using System;
using System.Text;
using System.Xml;
using EwSyndicationItem = Edi.SyndicationFeed.ReaderWriter.SyndicationItem;
using EwSyndicationLink = Edi.SyndicationFeed.ReaderWriter.SyndicationLink;
using EwSyndicationCategory = Edi.SyndicationFeed.ReaderWriter.SyndicationCategory;
using EwSyndicationPerson = Edi.SyndicationFeed.ReaderWriter.SyndicationPerson;
using Microsoft.Extensions.Options;
using MZikmund.Web.Configuration.ConfigSections;
using MZikmund.Web.Configuration;
using System.Reflection;

namespace MZikmund.Web.Core.Syndication;

public class FeedGenerator : IFeedGenerator
{
	public FeedGenerator(
		IHttpContextAccessor httpContextAccessor,
		ISiteConfiguration siteConfiguration)
	{
		var baseUrl = httpContextAccessor.HttpContext.Request.GetBaseUrl();
		HostUrl = baseUrl!;
		HeadTitle = siteConfiguration.General.DefaultTitle;
		HeadDescription = siteConfiguration.General.DefaultDescription;
		Copyright = $"©{DateTimeOffset.UtcNow.Year} {siteConfiguration.Author.FullName}";
		Generator = siteConfiguration.General.EngineName;
		GeneratorVersion = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "0.1";
		TrackBackUrl = baseUrl!;
		Language = siteConfiguration.General.DefaultCulture;
	}

	//TODO: Hide properties, use a private feed configuration class instead
	public string HostUrl { get; }

	public string HeadTitle { get; }

	public string HeadDescription { get; }

	public string Copyright { get; }

	public string Generator { get; }

	public string TrackBackUrl { get; }

	public string GeneratorVersion { get; }

	public string Language { get; }

	public async Task<string> GetRssAsync(IEnumerable<FeedEntry> feedEntries, string id)
	{
		var feed = new SyndicationFeed(HeadTitle, HeadDescription, new Uri("https://mzikmund.dev/rss"), id, DateTimeOffset.UtcNow);
		feed.Copyright = new TextSyndicationContent(Copyright);

		var items = GetItemCollection(feedEntries);
		feed.Items = items;
		// TODO: Generate additional properties (image, etc.)

		var sw = new StringWriterWithEncoding(Encoding.UTF8);

		var settings = new XmlWriterSettings
		{
			Encoding = Encoding.UTF8,
			NewLineHandling = NewLineHandling.Entitize,
			NewLineOnAttributes = true,
			Indent = true
		};

		await using (var xmlWriter = XmlWriter.Create(sw, new() { Async = true }))
		{
			var rssFormatter = new Rss20FeedFormatter(feed, false);
			rssFormatter.WriteTo(xmlWriter);

			await xmlWriter.FlushAsync();
			xmlWriter.Close();
		}

		var xml = sw.ToString();
		return xml;
	}

	public async Task<string> GetAtomAsync(IEnumerable<FeedEntry> feedEntries)
	{
		var feed = GetEwItemCollection(feedEntries);

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

	private static List<EwSyndicationItem> GetEwItemCollection(IEnumerable<FeedEntry> itemCollection)
	{
		var synItemCollection = new List<EwSyndicationItem>();
		if (null == itemCollection) return synItemCollection;

		foreach (var item in itemCollection)
		{
			// create rss item
			var sItem = new EwSyndicationItem
			{
				Id = item.Id,
				Title = item.Title,
				Description = item.Description,
				LastUpdated = item.UpdatedDate.ToUniversalTime(),
				Published = item.PublishedDate.ToUniversalTime()
			};

			sItem.AddLink(new EwSyndicationLink(new(item.Link)));

			// add author
			if (!string.IsNullOrWhiteSpace(item.Author) && !string.IsNullOrWhiteSpace(item.AuthorEmail))
			{
				sItem.AddContributor(new EwSyndicationPerson(item.Author, item.AuthorEmail));
			}

			// add categories
			if (item.Categories is { Length: > 0 })
			{
				foreach (var itemCategory in item.Categories)
				{
					sItem.AddCategory(new EwSyndicationCategory(itemCategory));
				}
			}
			synItemCollection.Add(sItem);
		}
		return synItemCollection;
	}

	private List<SyndicationItem> GetItemCollection(IEnumerable<FeedEntry> itemCollection)
	{
		var syndicationItems = new List<SyndicationItem>();
		if (null == itemCollection)
		{
			return syndicationItems;
		}

		foreach (var item in itemCollection)
		{
			// create rss item
			var syndicationItem = new SyndicationItem(item.Title, item.Description, new Uri(item.Link), item.Id, item.UpdatedDate);
			syndicationItem.Authors.Add(new SyndicationPerson(item.AuthorEmail, item.Author, HostUrl));

			// add categories
			if (item.Categories is { Length: > 0 })
			{
				foreach (var itemCategory in item.Categories)
				{
					syndicationItem.Categories.Add(new SyndicationCategory(itemCategory));
				}
			}

			syndicationItems.Add(syndicationItem);
		}
		return syndicationItems;
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
