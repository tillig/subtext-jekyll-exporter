using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SubtextJekyllExporter
{
	internal class Program
	{
		private const string PostFormat = @"---
layout: {0}
title: ""{1}""
date: {2}
comments: true
disqus_identifier: {3}
tags: {4}
---
{5}
";

		private static async Task<bool> CheckPostExistence(Uri uri)
		{
			var httpClient = new HttpClient();
			var request = new HttpRequestMessage { Method = HttpMethod.Head, RequestUri = uri };
			var response = await httpClient.SendAsync(request);
			return response.StatusCode == HttpStatusCode.OK;
		}

		private static void EnsurePath(string path)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
		}

		private static void Main(string[] args)
		{
			if (args.Length != 3)
			{
				Console.WriteLine("Please pass an XML export filename, an export directory for the markdown output, and your current blog host.");
				return;
			}

			var exportFileName = args[0];
			var rootDirectory = args[1];
			var host = args[2];

			var serializer = new XmlSerializer(typeof(BlogEntry));
			var entries = new List<BlogEntry>();
			using (var importStream = File.OpenRead(exportFileName))
			using (var importReader = XmlReader.Create(importStream))
			{
				// Get past the root element.
				importReader.MoveToElement();
				importReader.MoveToContent();
				importReader.Read();

				// Read the children - each child is an entry to deserialize.
				string xml = "";
				while ((xml = importReader.ReadOuterXml()) != "")
				{
					if (String.IsNullOrWhiteSpace(xml))
					{
						continue;
					}
					using (var serializedReader = new StringReader(xml))
					{
						entries.Add((BlogEntry)serializer.Deserialize(serializedReader));
					}
				}
			}

			Directory.CreateDirectory(rootDirectory);
			using (var mismatches = new StreamWriter(Path.Combine(rootDirectory, ".mismatches")))
			{
				foreach (var entry in entries)
				{
					var filePath = entry.FilePath.Replace(Environment.NewLine, "");
					var content = entry.Text
						.StripTagsDiv()
						.MakeInternalLinksRelative(host)
						.ConvertHtmlToMarkdown()
						.EscapeJekyllTags()
						.FormatCode()
						.FixEscapedNewlines()
						.FixSuperscript();

					var formattedContent = String.Format(PostFormat, entry.Layout, entry.Title, entry.Date, entry.Id, entry.Categories, content);
					var postUrl = new Uri(String.Format("http://{0}/archive/{1}/{2}.aspx", host, entry.UrlDate, entry.EntryName));
					try
					{
						var exists = CheckPostExistence(postUrl).Result;
						if (!exists)
						{
							mismatches.WriteLine(postUrl);
						}
					}
					catch (Exception ex)
					{
						mismatches.WriteLine("EXCEPTION: " + postUrl);
						mismatches.WriteLine(ex);
					}

					var path = Path.Combine(rootDirectory, filePath);
					EnsurePath(path);
					Console.WriteLine("Writing: " + entry.Title);
					File.WriteAllText(path, formattedContent, new UTF8Encoding(false));
				}
			}
		}
	}
}