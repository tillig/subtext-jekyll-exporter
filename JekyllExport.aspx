<%@ Page Language="C#" Debug="true" EnableTheming="false" Title="Subtext Admin - Jekyll Export" AutoEventWireup="True" Inherits="System.Web.UI.Page" %>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="Microsoft.ApplicationBlocks.Data" %>
<%@ Import Namespace="Subtext.Framework.Providers" %>

<script runat="server">
const string ExportSql = @"SELECT FilePath = (
    CASE c.PostType
    WHEN 2 THEN 'articles/' + c.EntryName + '.aspx.markdown'
    ELSE SUBSTRING(CONVERT(VARCHAR, c.DateAdded, 120), 1, 10)
        + '-' + c.EntryName + '.aspx.markdown'
    END),
c.[Text],
Layout = (CASE c.PostType WHEN 2 THEN 'page' ELSE 'post' END),
Title = REPLACE(c.Title, '""', '&quot;'),
[Date] = SUBSTRING(CONVERT(VARCHAR, c.DateAdded, 120), 1, 10) + ' -0800',
Categories = '[' +
 ISNULL(SUBSTRING(
   (SELECT ',' + cat.Title AS [text()]
    FROM subtext_LinkCategories cat
    INNER JOIN subtext_Links l
    ON l.CategoryID = cat.CategoryID
    WHERE c.ID = l.PostID
    FOR XML PATH ('')), 2, 1000), '') + ']',
Id,
c.EntryName,
UrlDate = SUBSTRING(CONVERT(VARCHAR, c.DateAdded, 111), 1, 10)
FROM subtext_Content c
INNER JOIN subtext_Config blog
ON c.BlogID = blog.BlogID
WHERE blog.BlogID = 0
 AND c.PostConfig & 1 = 1
 FOR XML AUTO";

  // Timeout has to be longer than the default because these ops can take a while.
  // Add this to the end of your connection string:
  // ";Connection Timeout=120"

	protected void Page_Load(object sender, EventArgs e)
	{
        this.Response.Filter = new System.IO.Compression.GZipStream(Response.Filter, System.IO.Compression.CompressionMode.Compress);
        this.Response.AppendHeader("Content-Encoding", "gzip");
		this.Response.ContentType = "text/xml";
		try
		{
			this.Response.Write("<content>");
			using (SqlConnection connection = new SqlConnection(Subtext.Framework.Configuration.Config.ConnectionString))
			{
				using (var reader = SqlHelper.ExecuteXmlReader(connection, CommandType.Text, ExportSql))
				{
					reader.Read();
					while (reader.ReadState != System.Xml.ReadState.EndOfFile)
					{
						this.Response.Write(reader.ReadOuterXml());
					}
					reader.Close();
				}
			}
			this.Response.Write("</content>");
		}
		catch (Exception ex)
		{
			this.Response.ContentType = "text/plain";
			this.Response.Write(ex.ToString());
		}
	}
</script>