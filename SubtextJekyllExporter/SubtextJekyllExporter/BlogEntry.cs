using System.Xml.Serialization;

namespace SubtextJekyllExporter
{
	// The SQL query FOR XML AUTO writes this out as a "c" element.
	[XmlRoot(ElementName = "c")]
	public class BlogEntry
	{
		[XmlAttribute]
		public string Categories { get; set; }

		[XmlAttribute]
		public string Date { get; set; }

		[XmlAttribute]
		public string EntryName { get; set; }

		[XmlAttribute]
		public string FilePath { get; set; }

		[XmlAttribute]
		public int Id { get; set; }

		[XmlAttribute]
		public string Layout { get; set; }

		[XmlAttribute]
		public string Text { get; set; }

		[XmlAttribute]
		public string Title { get; set; }

		[XmlAttribute]
		public string UrlDate { get; set; }
	}
}
