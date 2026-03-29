using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (IConfiguration config) =>
{
    var connStr = config.GetConnectionString("Default");

    if (string.IsNullOrWhiteSpace(connStr))
    {
        return Results.Content("<h2>Lỗi: chưa cấu hình Connection String 'Default'.</h2>", "text/html");
    }

    var list = new List<dynamic>();

    try
    {
        using SqlConnection conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        using SqlCommand cmd = new SqlCommand(@"
            SELECT TOP 50
                MaGiay,
                TenGiay,
                KhoGiay,
                SoLuongTon
            FROM gc.vw_TonKhoGiayCuon_Chuahet
        ", conn);

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new
            {
                MaGiay = reader["MaGiay"]?.ToString(),
                TenGiay = reader["TenGiay"]?.ToString(),
                KhoGiay = reader["KhoGiay"]?.ToString(),
                SoLuongTon = reader["SoLuongTon"]?.ToString()
            });
        }

        var html = "<h1>DGPack - Tồn kho thật</h1>";
        html += "<table border='1' cellspacing='0' cellpadding='8'>";
        html += "<tr><th>Mã giấy</th><th>Tên giấy</th><th>Kho giấy</th><th>Số lượng tồn</th></tr>";

        foreach (var item in list)
        {
            html += $"<tr><td>{item.MaGiay}</td><td>{item.TenGiay}</td><td>{item.KhoGiay}</td><td>{item.SoLuongTon}</td></tr>";
        }

        html += "</table>";

        return Results.Content(html, "text/html; charset=utf-8");
    }
    catch (Exception ex)
    {
        var html = $"<h2>Lỗi SQL/App</h2><pre>{System.Net.WebUtility.HtmlEncode(ex.Message)}</pre>";
        return Results.Content(html, "text/html; charset=utf-8");
    }
});

app.Run();