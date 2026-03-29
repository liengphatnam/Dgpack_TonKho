using Microsoft.Data.SqlClient;
using ClosedXML.Excel;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", async (HttpContext context) =>
{
    var connStr = builder.Configuration.GetConnectionString("Default");

    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();

    var cmd = new SqlCommand(@"
        SELECT MaSoCuon, MaGiay, TrongLuongCon, Kho
        FROM gc.vw_TonKhoGiayCuon_Chuahet
    ", conn);

    var reader = await cmd.ExecuteReaderAsync();

    var html = "<h2>DGPack - Tồn kho</h2>";
    html += "<button onclick='window.location=\"/export-excel\"'>Xuất Excel</button>";
    html += "<button onclick='window.print()'>In PDF</button>";

    html += "<table border='1' cellpadding='5'>";
    html += "<tr><th>Mã cuộn</th><th>Mã giấy</th><th>Trọng lượng</th><th>Kho</th></tr>";

    while (await reader.ReadAsync())
    {
        html += "<tr>";
        html += $"<td>{reader["MaSoCuon"]}</td>";
        html += $"<td>{reader["MaGiay"]}</td>";
        html += $"<td>{reader["TrongLuongCon"]}</td>";
        html += $"<td>{reader["Kho"]}</td>";
        html += "</tr>";
    }

    html += "</table>";

    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(html);
});

app.MapGet("/export-excel", async (HttpContext context) =>
{
    var connStr = builder.Configuration.GetConnectionString("Default");

    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();

    var cmd = new SqlCommand(@"
        SELECT MaSoCuon, MaGiay, TrongLuongCon, Kho
        FROM gc.vw_TonKhoGiayCuon_Chuahet
    ", conn);

    var reader = await cmd.ExecuteReaderAsync();

    using var wb = new XLWorkbook();
    var ws = wb.Worksheets.Add("TonKho");

    ws.Cell(1, 1).Value = "Mã cuộn";
    ws.Cell(1, 2).Value = "Mã giấy";
    ws.Cell(1, 3).Value = "Trọng lượng";
    ws.Cell(1, 4).Value = "Kho";

    int row = 2;

    while (await reader.ReadAsync())
    {
        ws.Cell(row, 1).Value = reader["MaSoCuon"]?.ToString();
        ws.Cell(row, 2).Value = reader["MaGiay"]?.ToString();
        ws.Cell(row, 3).Value = reader["TrongLuongCon"]?.ToString();
        ws.Cell(row, 4).Value = reader["Kho"]?.ToString();
        row++;
    }

    using var stream = new MemoryStream();
    wb.SaveAs(stream);

    context.Response.Headers.Append("Content-Disposition", "attachment; filename=TonKho.xlsx");
    context.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    await context.Response.Body.WriteAsync(stream.ToArray());
});

app.Run();
