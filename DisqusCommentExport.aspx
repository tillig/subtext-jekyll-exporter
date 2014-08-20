<%@ Page Language="C#" Debug="true" EnableTheming="false" Title="Subtext Admin - Disqus Comment Export" AutoEventWireup="True" Inherits="System.Web.UI.Page" %>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="Microsoft.ApplicationBlocks.Data" %>
<%@ Import Namespace="Subtext.Framework.Providers" %>

<script runat="server">
// The Disqus format is documented here:
// https://help.disqus.com/customer/portal/articles/472150

// This should be the number of hours to ADD
// to a date/time to convert to GMT. For example,
// PST is GMT-0800, so ADD 8 to make it GMT.
// Yeah, there's some daylight saving challenge here
// but it's a one-timer. Does it matter?
const int HoursToConvertToGMT = 8;

// Blog URL with trailing slash for generating links
// to entries.
const string Host = "http://www.paraesthesia.com/";

private static readonly string ExportSql = @"WITH XMLNAMESPACES
(
    'http://purl.org/rss/1.0/modules/content/' AS content,
	'http://www.disqus.com/' AS dsq,
    'http://purl.org/dc/elements/1.1/' AS dc,
    'http://wordpress.org/export/1.0/' AS wp
)

SELECT (
        SELECT Title AS 'title',
			link = '" + Host + @"' + (
				CASE PostType
				WHEN 2 THEN 'articles/' + EntryName + '.aspx'
				ELSE 'archive/' + SUBSTRING(CONVERT(VARCHAR, DateAdded, 111), 1, 10)
					+ '/' + EntryName + '.aspx'
				END),
			Text as 'content:encoded',
            ID AS 'dsq:thread_identifier',
            CONVERT(VARCHAR, DATEADD(hour," + HoursToConvertToGMT + @", DateAdded), 120) AS 'wp:post_date_gmt',
			'closed' AS 'wp:comment_status',
            (
                SELECT Id AS 'wp:comment_id',
                    Author AS 'wp:comment_author',
                    Email AS 'wp:comment_author_email',
                    IpAddress AS 'wp:comment_author_IP',
                    CONVERT(VARCHAR, DATEADD(hour, " + HoursToConvertToGMT + @", DateCreated), 120) AS 'wp:comment_date_gmt',
                    Body AS 'wp:comment_content',
					1 AS 'wp:comment_approved'
                FROM subtext_FeedBack
                WHERE FeedbackType = 1 AND EntryId = content.ID
                FOR XML PATH('wp:comment'), TYPE
            )
        FROM subtext_Content AS content
        WHERE PostType = 1 AND PostConfig <> 6
        FOR XML PATH('item'), TYPE
    )
FROM subtext_Config
WHERE BlogId = 0
FOR XML PATH('channel'), ROOT('rss')";

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
        }
        catch (Exception ex)
        {
            this.Response.ContentType = "text/plain";
            this.Response.Write(ex.ToString());
        }
    }
</script>