<%@ Page Language="C#" Debug="true" EnableTheming="false" Title="Subtext Admin - Disqus Comment Export" AutoEventWireup="True" Inherits="System.Web.UI.Page" %>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="Microsoft.ApplicationBlocks.Data" %>
<%@ Import Namespace="Subtext.Framework.Providers" %>

<script runat="server">

const string ExportSql = @"WITH XMLNAMESPACES
(
    'http://purl.org/dc/elements/1.1/' AS dc,
    'http://wordpress.org/export/1.0/excerpt/' AS excerpt,
    'http://purl.org/rss/1.0/modules/content/' AS content,
    'http://wellformedweb.org/CommentAPI/' AS wfw,
    'http://wordpress.org/export/1.0/' AS wp
)

SELECT Title AS 'title',
    '1.0' AS 'wp:wxr_version',
    (
        SELECT Title AS 'title',
            DateAdded AS 'pubDate',
            ID AS 'wp:post_id',
            EntryName AS 'wp:post_name',
            'publish' AS 'wp:status',
            'post' AS 'wp:post_type',
            DateSyndicated AS 'wp:post_date',
            (
                SELECT Id AS 'wp:comment_id',
                    Author AS 'wp:comment_author',
                    Email AS 'wp:comment_author_email',
                    IpAddress AS 'wp:comment_author_IP',
                    DateCreated AS 'wp:comment_date',
                    Body AS 'wp:comment_content'
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