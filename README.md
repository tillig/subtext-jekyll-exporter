Subtext Jekyll Exporter
=======================

A combination of a database admin page and console app that exports a Subtext database to Jekyll (Octopress) formatted post files.

This is a fork of [Phil Haack's Subtext Jekyll Exporter](https://github.com/Haacked/subtext-jekyll-exporter) updated to handle some quirks I have that weren't in Haack's Subtext deployment:

	* I don't have direct SQL Management Console access to my database so I use a small ASPX page to export the blog content in XML format and the console app works off the XML.
	* Haack had a later database format than Subtext 2.5.2.0 and his exporter referred to columns that I don't have. I have stock Subtext 2.5.2.0.

## USAGE

Drop the `JekyllExport.aspx` in your Subtext install where you can get to it from a browser. Hit the page and save the output as XML. Run the console app against the XML to do the conversion to markdown.

## LICENSE

Subtext Jekyll Exporter is licened under an [MIT License](LICENSE).

However, it shells out to Pandoc which is licensed under [the GPL](http://www.gnu.org/copyleft/gpl.html).
