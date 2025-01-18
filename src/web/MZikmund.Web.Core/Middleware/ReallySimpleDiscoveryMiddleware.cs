using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Http;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Utilities;

namespace MZikmund.Web.Core.Middleware;

public class ReallySimpleDiscoveryMiddleware
{
	private readonly RequestDelegate _next;

	public ReallySimpleDiscoveryMiddleware(RequestDelegate next) => _next = next;

	public async Task Invoke(HttpContext httpContext, ISiteConfiguration siteConfiguration)
	{
		if (httpContext.Request.Path == "/rsd")
		{
			var xml = await GenerateXmlContentsAsync(siteConfiguration);

			httpContext.Response.ContentType = "text/xml";
			await httpContext.Response.WriteAsync(xml, httpContext.RequestAborted);
		}
		else
		{
			await _next(httpContext);
		}
	}

	private static async Task<string> GenerateXmlContentsAsync(ISiteConfiguration siteConfiguration)
	{
		var sb = new StringBuilder();

		var writerSettings = new XmlWriterSettings
		{
			Encoding = Encoding.UTF8,
			Async = true
		};

		await using (var writer = XmlWriter.Create(sb, writerSettings))
		{
			await writer.WriteStartDocumentAsync();

			// Rsd tag
			writer.WriteStartElement("rsd");
			writer.WriteAttributeString("version", "1.0");

			var engineName = siteConfiguration.General.EngineName;
			var siteUrl = siteConfiguration.General.Url;

			// Service 
			writer.WriteStartElement("service");
			writer.WriteElementString("engineName", $"{engineName} {EngineInfo.Version}");
			writer.WriteElementString("engineLink", siteConfiguration.General.EngineRepositoryUrl.ToString());
			writer.WriteElementString("homePageLink", siteUrl.ToString());

			// End Service
			await writer.WriteEndElementAsync();

			// End Rsd
			await writer.WriteEndElementAsync();

			await writer.WriteEndDocumentAsync();
		}

		var xml = sb.ToString();
		return xml;
	}
}
