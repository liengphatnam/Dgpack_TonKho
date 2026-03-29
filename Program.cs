using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (HttpRequest request, IConfiguration config) =>
{
    var connStr = config.GetConnectionString("Default");

    var keyword = request.Query["q"].ToString();

    var rows = new List<string>();

    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();

    var sql = @"
        SELECT TOP 100
            MaGiay,
            TrongLuongCon
        FROM gc.vw_TonKhoGiayCuon_Chuahet
        WHERE (@kw = '' OR MaGiay LIKE '%' + @kw + '%')
        ORDER BY MaGiay
    ";

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@kw", keyword ?? "");

    using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        var maGiay = reader["MaGiay"]?.ToString() ?? "";
        var trongLuongCon = reader["TrongLuongCon"]?.ToString() ?? "";

        rows.Add($"<tr><td>{maGiay}</td><td>{trongLuongCon}</td></tr>");
    }

    var html = $"""
    <html>
    <head>
        <meta charset="utf-8" />
        <title>DGPack - Tồn kho</title>
    </head>
    <body>
        <h1>DGPack - Tồn kho</h1>

        <form method="get">
            <input name="q" placeholder="Tìm mã giấy..." value="{keyword}" />
            <button type="submit">Tìm</button>
        </form>

        <br/>

        <table border="1" cellspacing="0" cellpadding="8">
            <tr>
                <th>Mã giấy</th>
                <th>Trọng lượng còn</th>
            </tr>
            {string.Join("", rows)}
        </table>
    </body>
    </html>
    """;

    return Results.Content(html, "text/html; charset=utf-8");
});

app.Run();
