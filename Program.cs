using Microsoft.Data.SqlClient;
using ClosedXML.Excel;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (HttpContext context) =>
{
    try
    {
        var connStr = builder.Configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connStr))
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("<h2>Lỗi: chưa cấu hình Connection String 'Default'.</h2>");
            return;
        }

        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
            SELECT TOP 200
                MaSoCuon,
                MaGiay,
                TrongLuongCon,
                KhoGiay
            FROM gc.vw_TonKhoGiayCuon_Chuahet
            ORDER BY MaGiay, MaSoCuon
        ", conn);

        using var reader = await cmd.ExecuteReaderAsync();

        var html = "";
        html += "<html><head><meta charset='utf-8' />";
        html += "<meta name='viewport' content='width=device-width, initial-scale=1' />";
        html += "<title>DGPack - Tồn kho</title>";
        html += "<style>";
        html += "body{font-family:Arial;padding:12px;background:#f5f7fb;}";
        html += "h2{margin-top:0;}";
        html += "button,a{padding:8px 12px;margin-right:8px;text-decoration:none;}";
        html += "table{border-collapse:collapse;width:100%;background:#fff;}";
        html += "th,td{border:1px solid #ccc;padding:8px;text-align:left;}";
        html += "th{background:#eee;}";
        html += "@media print{button,.no-print{display:none;} body{background:#fff;}}";
        html += "</style></head><body>";

        html += "<h2>DGPack - Tồn kho giấy cuộn</h2>";
        html += "<div class='no-print' style='margin-bottom:12px;'>";
        html += "<a href='/export-excel'>Xuất Excel</a>";
        html += "<button onclick='window.print()'>In / Lưu PDF</button>";
        html += "</div>";

        html += "<table>";
        html += "<tr><th>Mã số cuộn</th><th>Mã giấy</th><th>Trọng lượng còn</th><th>Kho</th></tr>";

        while (await reader.ReadAsync())
        {
            var maSoCuon = reader["MaSoCuon"]?.ToString() ?? "";
            var maGiay = reader["MaGiay"]?.ToString() ?? "";
            var trongLuongCon = reader["TrongLuongCon"]?.ToString() ?? "";
            var khoGiay = reader["KhoGiay"]?.ToString() ?? "";

            html += "<tr>";
            html += "<td>" + WebUtility.HtmlEncode(maSoCuon) + "</td>";
            html += "<td>" + WebUtility.HtmlEncode(maGiay) + "</td>";
            html += "<td>" + WebUtility.HtmlEncode(trongLuongCon) + "</td>";
            html += "<td>" + WebUtility.HtmlEncode(khoGiay) + "</td>";
            html += "</tr>";
        }

        html += "</table></body></html>";

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html);
    }
    catch (Exception ex)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync("<h2>Lỗi SQL/App</h2><pre>" + WebUtility.HtmlEncode(ex.ToString()) + "</pre>");
    }
});

app.MapGet("/export-excel", async (HttpContext context) =>
{
    try
    {
        var connStr = builder.Configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connStr))
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("<h2>Lỗi: chưa cấu hình Connection String 'Default'.</h2>");
            return;
        }

        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
            SELECT
                MaSoCuon,
                MaGiay,
                TrongLuongCon,
                KhoGiay
            FROM gc.vw_TonKhoGiayCuon_Chuahet
            ORDER BY MaGiay, MaSoCuon
        ", conn);

        using var reader = await cmd.ExecuteReaderAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("TonKho");

        ws.Cell(1, 1).Value = "Mã số cuộn";
        ws.Cell(1, 2).Value = "Mã giấy";
        ws.Cell(1, 3).Value = "Trọng lượng còn";
        ws.Cell(1, 4).Value = "Kho";

        int row = 2;
        while (await reader.ReadAsync())
        {
            ws.Cell(row, 1).Value = reader["MaSoCuon"]?.ToString();
            ws.Cell(row, 2).Value = reader["MaGiay"]?.ToString();
            ws.Cell(row, 3).Value = reader["TrongLuongCon"]?.ToString();
            ws.Cell(row, 4).Value = reader["KhoGiay"]?.ToString();
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        var bytes = stream.ToArray();

        context.Response.Headers.Append("Content-Disposition", "attachment; filename=TonKho.xlsx");
        context.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        await context.Response.Body.WriteAsync(bytes);
    }
    catch (Exception ex)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync("<h2>Lỗi xuất Excel</h2><pre>" + WebUtility.HtmlEncode(ex.ToString()) + "</pre>");
    }
});

app.Run();
