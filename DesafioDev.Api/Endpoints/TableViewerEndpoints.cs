using Npgsql;
using System.Text;

namespace DesafioDev.Api.Endpoints;

public static class TableViewerEndpoints
{
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "stores",
        "transactions"
    };

    public static void MapTableViewerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/database/tables")
            .WithTags("Table Viewer")
            .WithOpenApi();

        group.MapGet("/", GetTableList)
            .WithName("GetTableList")
            .WithSummary("List available tables")
            .Produces<string>(200, "text/html");

        group.MapGet("/{tableName}", GetTableHtml)
            .WithName("GetTableHtml")
            .WithSummary("Get table data as raw HTML")
            .Produces<string>(200, "text/html");
    }

    private static IResult GetTableList()
    {
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html><head>");
        html.Append("<meta charset=\"UTF-8\">");
        html.Append("<title>Database Tables</title>");
        html.Append("<style>");
        html.Append("body{font-family:'Courier New',monospace;background:#1e1e1e;color:#d4d4d4;padding:20px}");
        html.Append("a{color:#569cd6;text-decoration:none}a:hover{text-decoration:underline}");
        html.Append("h1{color:#4ec9b0}ul{list-style:none;padding:0}li{padding:5px 0}");
        html.Append("</style></head><body>");
        html.Append("<h1>FinanceDB Tables</h1>");
        html.Append("<ul>");
        foreach (var table in AllowedTables.OrderBy(t => t))
        {
            html.Append($"<li><a href=\"/api/database/tables/{table}\">&#128203; {table}</a></li>");
        }
        html.Append("</ul>");
        html.Append("</body></html>");

        return Results.Content(html.ToString(), "text/html");
    }

    private static async Task<IResult> GetTableHtml(
        string tableName,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        if (!AllowedTables.Contains(tableName))
        {
            return Results.Content(
                $"<html><body><h1>Error</h1><p>Table '{tableName}' not allowed.</p></body></html>",
                "text/html",
                statusCode: 400);
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            return Results.Content(
                "<html><body><h1>Error</h1><p>Database not configured.</p></body></html>",
                "text/html",
                statusCode: 500);
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var sql = tableName.ToLower() switch
            {
                "stores" => "SELECT id, name, owner_name, created_at FROM stores ORDER BY id",
                "transactions" => "SELECT id, type, date, time, value, cpf, card_number, store_id FROM transactions ORDER BY id",
                _ => throw new ArgumentException("Invalid table")
            };

            await using var cmd = new NpgsqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            var html = new StringBuilder();
            html.Append("<!DOCTYPE html><html><head>");
            html.Append("<meta charset=\"UTF-8\">");
            html.Append($"<title>{tableName} - Database Viewer</title>");
            html.Append("<style>");
            html.Append("body{font-family:'Courier New',monospace;background:#1e1e1e;color:#d4d4d4;padding:20px;margin:0}");
            html.Append("h1{color:#4ec9b0;margin-bottom:5px}");
            html.Append(".nav{margin-bottom:20px}");
            html.Append(".nav a{color:#569cd6;margin-right:15px;text-decoration:none}");
            html.Append(".nav a:hover{text-decoration:underline}");
            html.Append("table{border-collapse:collapse;width:100%;margin-top:10px}");
            html.Append("th{background:#264f78;color:#fff;padding:10px;text-align:left;border:1px solid #3c3c3c}");
            html.Append("td{padding:8px 10px;border:1px solid #3c3c3c}");
            html.Append("tr:nth-child(even){background:#2d2d2d}");
            html.Append("tr:hover{background:#3e3e3e}");
            html.Append(".count{color:#6a9955;margin-top:10px}");
            html.Append("</style></head><body>");

            html.Append("<div class=\"nav\">");
            html.Append("<a href=\"/api/database/tables\">← Tables</a>");
            foreach (var t in AllowedTables.OrderBy(x => x))
            {
                var active = t.Equals(tableName, StringComparison.OrdinalIgnoreCase) ? " style=\"font-weight:bold\"" : "";
                html.Append($"<a href=\"/api/database/tables/{t}\"{active}>{t}</a>");
            }
            html.Append("</div>");

            html.Append($"<h1>{tableName}</h1>");
            html.Append("<table><tr>");

            for (int i = 0; i < reader.FieldCount; i++)
            {
                html.Append($"<th>{reader.GetName(i)}</th>");
            }
            html.Append("</tr>");

            int rowCount = 0;
            while (await reader.ReadAsync())
            {
                rowCount++;
                html.Append("<tr>");
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? "<null>" : reader.GetValue(i)?.ToString() ?? "";
                    html.Append($"<td>{System.Web.HttpUtility.HtmlEncode(value)}</td>");
                }
                html.Append("</tr>");
            }

            html.Append("</table>");
            html.Append($"<p class=\"count\">({rowCount} rows)</p>");
            html.Append("</body></html>");

            return Results.Content(html.ToString(), "text/html");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching table {TableName}", tableName);
            return Results.Content(
                $"<html><body><h1>Error</h1><p>{System.Web.HttpUtility.HtmlEncode(ex.Message)}</p></body></html>",
                "text/html",
                statusCode: 500);
        }
    }
}
