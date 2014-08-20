using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SubtextJekyllExporter
{
	public static class ContentStringExtensions
	{
		private static readonly Regex CodeRegex = new Regex(@"~~~~ \{\.csharpcode\}(?<code>.*?)~~~~", RegexOptions.Compiled | RegexOptions.Singleline);

		private static readonly Regex EscapedNewlineRegex = new Regex(@"\\\s*$", RegexOptions.Multiline);

		private static readonly Regex SuperscriptRegex = new Regex(@"\^([^\s]+)\^");

		// Strip the DIVs for tags that Windows Live Writer inserts.
		//   <div class="tags">Technorati Tags:...</div>
		//   <div class="tags clear">Technorati Tags:...</div>
		//   <div style="..." id="scid:..." class="wlWriterEditableSmartContent">
		private static readonly Regex TagsRegex = new Regex(@"<div[^>]+?class=""(tags(\s*clear)?|wlWriterEditableSmartContent)"">.*?</div>", RegexOptions.Compiled | RegexOptions.Singleline);

		public static string ConvertHtmlToMarkdown(this string source)
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

		public static string EscapeJekyllTags(this string content)
		{
			return content
				.Replace("{{", "{{ \"{{\" }}")
				.Replace("{%", "{{ \"{%\" }}");
		}

		public static string FixEscapedNewlines(this string content)
		{
			// <br/><br/> results in pandoc issuing an odd
			// "escaped newline" construct.
			return EscapedNewlineRegex.Replace(content, "");
		}

		public static string FixSuperscript(this string content)
		{
			// pandoc makes superscript ^xyz^ and Jekyll
			// doesn't like that. Put the tags back.
			return SuperscriptRegex.Replace(content, "<sup>$1</sup>");
		}

		public static string FormatCode(this string content)
		{
			return CodeRegex.Replace(content, match =>
			{
				var code = match.Groups["code"].Value;
				return "```" + GetLanguage(code) + code + "```";
			});
		}

		public static string StripTagsDiv(this string content)
		{
			return TagsRegex.Replace(content, "");
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
	}
}
