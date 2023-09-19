﻿// Based on https://github.com/EdiWang/Moonglade/blob/cf5571b0db09c7722b310ca9922cdcd542e93a51/src/Moonglade.Syndication/GetOpmlQuery.cs

using System.Text;
using System.Xml;
using MediatR;

namespace MZikmund.Web.Core.Syndication;

public class GetOpmlHandler : IRequestHandler<GetOpmlQuery, string>
{
	public async Task<string> Handle(GetOpmlQuery request, CancellationToken ct)
	{
		var opmlDoc = request.OpmlDoc;

		var sb = new StringBuilder();

		var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
		await using (var writer = XmlWriter.Create(sb, writerSettings))
		{
			// open OPML
			writer.WriteStartElement("opml");

			await writer.WriteAttributeStringAsync("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
			await writer.WriteAttributeStringAsync("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
			writer.WriteAttributeString("version", "1.0");

			// open HEAD
			writer.WriteStartElement("head");
			writer.WriteStartElement("title");
			writer.WriteValue(opmlDoc.SiteTitle);
			await writer.WriteEndElementAsync();
			await writer.WriteEndElementAsync();

			// open BODY
			writer.WriteStartElement("body");

			// allrss
			writer.WriteStartElement("outline");
			writer.WriteAttributeString("title", "All Posts");
			writer.WriteAttributeString("text", "All Posts");
			writer.WriteAttributeString("type", "rss");
			writer.WriteAttributeString("xmlUrl", opmlDoc.RssUrl);
			writer.WriteAttributeString("htmlUrl", opmlDoc.BlogUrl);
			await writer.WriteEndElementAsync();

			// categories
			foreach (var category in opmlDoc.ContentInfo)
			{
				// open OUTLINE
				writer.WriteStartElement("outline");

				writer.WriteAttributeString("title", category.Key);
				writer.WriteAttributeString("text", category.Value);
				writer.WriteAttributeString("type", "rss");
				writer.WriteAttributeString("xmlUrl", opmlDoc.RssCategoryTemplate.Replace("[catTitle]", category.Value).ToLowerInvariant());
				writer.WriteAttributeString("htmlUrl", opmlDoc.BlogCategoryTemplate.Replace("[catTitle]", category.Value).ToLowerInvariant());

				// close OUTLINE
				await writer.WriteEndElementAsync();
			}

			// close BODY
			await writer.WriteEndElementAsync();

			// close OPML
			await writer.WriteEndElementAsync();
		}

		var xml = sb.ToString();
		return xml;
	}
}
