using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (IConfiguration config) =>
{
    var connStr = config.GetConnectionString("Default");

    var list = new List<dynamic>();

    using (SqlConnection conn = new SqlConnection(connStr))
    {
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
            SELECT TOP 50 
                MaGiay,
                TenGiay,
                KhoGiay,
                SoLuongTon
            FROM gc.vw_TonKhoGiayCuon_Chuahet
        ", conn);

        var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new
            {
                MaGiay = reader["MaGiay"].ToString(),
                TenGiay = reader["TenGiay"].ToString(),
                Kho = reader["KhoGiay"].ToString(),
                Ton = reader["SoLuongTon"].ToString()
            });
        }
    }

    var html = "<h1>DGPack - Tồn kho thật</h1><table border='1'>";

    html += "<tr><th>Mã</th><th>Tên</th><th>Khổ</th><th>Tồn</th></tr>";

    foreach (var item in list)
    {
        html += $"<tr><td>{item.MaGiay}</td><td>{item.TenGiay}</td><td>{item.Kho}</td><td>{item.Ton}</td></tr>";
    }

    html += "</table>";

    return Results.Content(html, "text/html");
});

app.Run();