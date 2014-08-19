﻿using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubtextJekyllExporter
{
	internal class Program
	{
		private const string postFormat = @"---
layout: {0}
title: ""{1}""
date: {2}
comments: true
disqus_identifier: {3}
categories: {4}
---
{5}
";

		private static readonly Regex _codeRegex = new Regex(@"~~~~ \{\.csharpcode\}(?<code>.*?)~~~~", RegexOptions.Compiled | RegexOptions.Singleline);

		// Strip the DIVs for tags that Windows Live Writer inserts.
		//   <div class="tags">Technorati Tags:...</div>
		//   <div class="tags clear">Technorati Tags:...</div>
		//   <div style="..." id="scid:..." class="wlWriterEditableSmartContent">
		private static readonly Regex _tagsRegex = new Regex(@"<div[^>]+?class=""(tags(\s*clear)?|wlWriterEditableSmartContent)"">.*?</div>", RegexOptions.Compiled | RegexOptions.Singleline);

		private static async Task<bool> CheckPostExistence(Uri uri)
		{
			var httpClient = new HttpClient();
			var request = new HttpRequestMessage { Method = HttpMethod.Head, RequestUri = uri };
			var response = await httpClient.SendAsync(request);
			return response.StatusCode == HttpStatusCode.OK;
		}

		private static string ConvertHtmlToMarkdown(string source)
		{
			var args = "-r html -t markdown";

			var startInfo = new ProcessStartInfo("pandoc.exe", args)
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
			return _codeRegex.Replace(content, match =>
			{
				var code = match.Groups["code"].Value;
				return "```" + GetLanguage(code) + code + "```";
			});
		}

		private static string GetExportSqlScript()
		{
			var assembly = Assembly.GetExecutingAssembly();
			const string resourceName = "SubtextJekyllExporter.select-content-for-jekyll.sql";

			using (var stream = assembly.GetManifestResourceStream(resourceName))
			{
				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
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
				Console.WriteLine("Please pass a database name, an export directory, and your current blog host.");
				return;
			}
			var databaseName = args[0];
			var rootDirectory = args[1];
			var host = args[2];
			var connectionString =
																String.Format(@"Data Source=.\SQLEXPRESS;Initial Catalog={0};Integrated Security=True", databaseName);
			using (var mismatches = new StreamWriter(Path.Combine(rootDirectory, ".mismatches")))
			{
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					var command = new SqlCommand(GetExportSqlScript(), connection);
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var filePath = reader.GetString(0).Replace(Environment.NewLine, "");
							var content = FormatCode(EscapeJekyllTags(ConvertHtmlToMarkdown(StripTagsDiv(reader.GetString(1)))));
							var layout = reader.GetString(2);
							var title = reader.GetString(3);
							var date = reader.GetString(4);
							var categories = reader.GetString(5);
							var postId = reader.GetInt32(6).ToString(CultureInfo.InvariantCulture);
							var slug = reader.GetString(7);
							var urlDate = reader.GetString(8);

							var formattedContent = String.Format(postFormat, layout, title, date, postId, categories, content);

							var postUrl =
																																new Uri("http://" + host + "/archive/" + urlDate + "/" + slug + ".aspx");
							try
							{
								var exists = CheckPostExistence(postUrl).Result;
								if (!exists)
								{
									mismatches.WriteLine(postUrl);
								}
							}
							catch (Exception)
							{
								mismatches.WriteLine("EXCEPTION: " + postUrl);
							}

							var path = Path.Combine(rootDirectory, filePath);
							EnsurePath(path);
							Console.WriteLine("Writing: " + title);
							File.WriteAllText(path, formattedContent, new UTF8Encoding(false));
						}
					}
				}
			}
		}

		private static string StripTagsDiv(string content)
		{
			return _tagsRegex.Replace(content, "");
		}
	}
}