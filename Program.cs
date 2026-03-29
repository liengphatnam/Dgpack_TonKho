using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (IConfiguration config) =>
{
    var connStr = config.GetConnectionString("Default");

    if (string.IsNullOrWhiteSpace(connStr))
    {
        return Results.Content("<h2>Lỗi: chưa cấu hình Connection String 'Default'.</h2>", "text/html; charset=utf-8");
    }

    var rows = new List<string>();

    try
    {
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(@"
            SELECT TOP 50
                MaGiay,
                TenGiay,
                KhoGiay,
                SoLuongCon
            FROM gc.vw_TonKhoGiayCuon_Chuahet
        ", conn);

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var maGiay = reader["MaGiay"]?.ToString() ?? "";
            var tenGiay = reader["TenGiay"]?.ToString() ?? "";
            var khoGiay = reader["KhoGiay"]?.ToString() ?? "";
            var soLuongCon = reader["SoLuongCon"]?.ToString() ?? "";

            rows.Add($"<tr><td>{maGiay}</td><td>{tenGiay}</td><td>{khoGiay}</td><td>{soLuongCon}</td></tr>");
        }

        var html = $"""
        <html>
        <head>
            <meta charset="utf-8" />
            <title>DGPack - Tồn kho thật</title>
        </head>
        <body>
            <h1>DGPack - Tồn kho thật</h1>
            <table border="1" cellspacing="0" cellpadding="8">
                <tr>
                    <th>Mã giấy</th>
                    <th>Tên giấy</th>
                    <th>Kho giấy</th>
                    <th>Số lượng còn</th>
                </tr>
                {string.Join("", rows)}
            </table>
        </body>
        </html>
        """;

        return Results.Content(html, "text/html; charset=utf-8");
    }
    catch (Exception ex)
    {
        var html = $"<h2>Lỗi SQL/App</h2><pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
        return Results.Content(html, "text/html; charset=utf-8");
    }
});

app.Run();
