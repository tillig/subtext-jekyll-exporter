using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
categories: {4}
---
{5}
";

		private static readonly Regex CodeRegex = new Regex(@"~~~~ \{\.csharpcode\}(?<code>.*?)~~~~", RegexOptions.Compiled | RegexOptions.Singleline);

		// Strip the DIVs for tags that Windows Live Writer inserts.
		//   <div class="tags">Technorati Tags:...</div>
		//   <div class="tags clear">Technorati Tags:...</div>
		//   <div style="..." id="scid:..." class="wlWriterEditableSmartContent">
		private static readonly Regex TagsRegex = new Regex(@"<div[^>]+?class=""(tags(\s*clear)?|wlWriterEditableSmartContent)"">.*?</div>", RegexOptions.Compiled | RegexOptions.Singleline);

		private static async Task<bool> CheckPostExistence(Uri uri)
		{
			var httpClient = new HttpClient();
			var request = new HttpRequestMessage { Method = HttpMethod.Head, RequestUri = uri };
			var response = await httpClient.SendAsync(request);
			return response.StatusCode == HttpStatusCode.OK;
		}

		private static string ConvertHtmlToMarkdown(string source)
		{
			var startInfo = new ProcessStartInfo("pandoc.exe", "-r html -t markdown")
												{
													RedirectStandardOutput = true,
													RedirectStandardInput = true,
													UseShellExecute = false
												};

			var process = new Process { StartInfo = startInfo };
			process.Start();

			var inputBuffer = Encoding.UTF8.GetBytes(source);
			process.StandardInput.BaseStream.Write(inputBuffer, 0, inputBuffer.Length);
			process.StandardInput.Close();

			process.WaitForExit(2000);
			using (var sr = new StreamReader(process.StandardOutput.BaseStream))
			{
				return sr.ReadToEnd();
			}
		}

		private static void EnsurePath(string path)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
		}

		private static string EscapeJekyllTags(string content)
		{
			return content
				.Replace("{{", "{{ \"{{\" }}")
				.Replace("{%", "{{ \"{%\" }}");
		}

		private static string FormatCode(string content)
		{
			return CodeRegex.Replace(content, match =>
			{
				var code = match.Groups["code"].Value;
				return "```" + GetLanguage(code) + code + "```";
			});
		}

		private static string GetLanguage(string code)
		{
			var trimmedCode = code.Trim();
			if (trimmedCode.Contains("<%= ") || trimmedCode.Contains("<%: "))
			{
				return "aspx-cs";
			}
			if (trimmedCode.StartsWith("<script") || trimmedCode.StartsWith("<table"))
			{
				return "html";
			}
			return "csharp";
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
					var content = FormatCode(EscapeJekyllTags(ConvertHtmlToMarkdown(StripTagsDiv(entry.Text))));
					var formattedContent = String.Format(PostFormat, entry.Layout, entry.Title, entry.Date, entry.Id, entry.Categories, content);
					var postUrl = new Uri("http://" + host + "/archive/" + entry.UrlDate + "/" + entry.EntryName + ".aspx");
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

		private static string StripTagsDiv(string content)
		{
			return TagsRegex.Replace(content, "");
		}
	}
}