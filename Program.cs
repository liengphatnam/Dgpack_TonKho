using Microsoft.Data.SqlClient;
using ClosedXML.Excel;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string BuildTableHtml(List<string> rows, string keyword, string kho, string khoHtml)
{
    return ""
        + "<html>"
        + "<head>"
        + "<meta charset='utf-8' />"
        + "<meta name='viewport' content='width=device-width, initial-scale=1' />"
        + "<title>DGPack - Tồn kho</title>"
        + "<style>"
        + "body{font-family:Arial;padding:10px;}"
        + "table{border-collapse:collapse;width:100%;}"
        + "th,td{border:1px solid #ccc;padding:8px;text-align:left;}"
        + "th{background:#eee;}"
        + "input,select,button,a.btn{padding:6px 10px;margin:4px 0;text-decoration:none;display:inline-block;border:1px solid #999;border-radius:4px;background:#f5f5f5;color:#000;}"
        + "@media print{button,.no-print,a.btn{display:none !important;}}"
        + "</style>"
        + "</head>"
        + "<body>"
        + "<h2>DGPack - Tồn kho</h2>"
        + "<form method='get'>"
        + "<input name='q' placeholder='Tìm mã giấy hoặc mã cuộn...' value='" + WebUtility.HtmlEncode(keyword) + "' />"
        + "<select name='kho'>" + khoHtml + "</select><br/>"
        + "<button type='submit'>Lọc</button> "
        + "<a class='btn no-print' href='/export-excel?q=" + Uri.EscapeDataString(keyword) + "&kho=" + Uri.EscapeDataString(kho) + "'>Xuất Excel</a> "
        + "<button type='button' class='no-print' onclick='window.print()'>In / Lưu PDF</button>"
        + "</form>"
        + "<table>"
        + "<tr><th>Mã số cuộn</th><th>Mã giấy</th><th>Trọng lượng còn</th><th>Kho</th></tr>"
        + string.Join("", rows)
        + "</table>"
        + "</body>"
        + "</html>";
}

app.MapGet("/", async (HttpRequest request, IConfiguration config) =>
{
    var connStr = config.GetConnectionString("Default");

    if (string.IsNullOrWhiteSpace(connStr))
    {
        return Results.Content("<h2>Lỗi: chưa cấu hình Connection String 'Default'.</h2>", "text/html; charset=utf-8");
    }

    var keyword = request.Query["q"].ToString();
    var kho = request.Query["kho"].ToString();

    var rows = new List<string>();
    var khoOptions = new List<string>();

    try
    {
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        using (var cmdKho = new SqlCommand(
            "SELECT DISTINCT CAST(KhoGiay AS nvarchar(50)) AS KhoGiay FROM gc.vw_TonKhoGiayCuon_Chuahet ORDER BY CAST(KhoGiay AS nvarchar(50))",
            conn))
        using (var readerKho = await cmdKho.ExecuteReaderAsync())
        {
            while (await readerKho.ReadAsync())
            {
                khoOptions.Add(readerKho["KhoGiay"]?.ToString() ?? "");
            }
        }

        var sql = @"
            SELECT TOP 100
                MaSoCuon,
                MaGiay,
                TrongLuongCon,
                KhoGiay
            FROM gc.vw_TonKhoGiayCuon_Chuahet
            WHERE (@kw = '' OR MaGiay LIKE '%' + @kw + '%' OR MaSoCuon LIKE '%' + @kw + '%')
              AND (@kho = '' OR CAST(KhoGiay AS nvarchar(50)) = @kho)
            ORDER BY MaGiay, MaSoCuon";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@kw", keyword ?? "");
        cmd.Parameters.AddWithValue("@kho", kho ?? "");

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var maSoCuon = reader["MaSoCuon"]?.ToString() ?? "";
            var maGiay = reader["MaGiay"]?.ToString() ?? "";
            var trongLuongCon = reader["TrongLuongCon"]?.ToString() ?? "";
            var khoGiay = reader["KhoGiay"]?.ToString() ?? "";

            rows.Add("<tr><td>" + WebUtility.HtmlEncode(maSoCuon) + "</td><td>"
                + WebUtility.HtmlEncode(maGiay) + "</td><td>"
                + WebUtility.HtmlEncode(trongLuongCon) + "</td><td>"
                + WebUtility.HtmlEncode(khoGiay) + "</td></tr>");
        }

        var khoHtml = "<option value=''>-- Tất cả kho --</option>";
        foreach (var k in khoOptions)
        {
            var selected = k == kho ? " selected" : "";
            khoHtml += "<option value='" + WebUtility.HtmlEncode(k) + "'" + selected + ">"
                    + WebUtility.HtmlEncode(k) + "</option>";
        }

        var html = BuildTableHtml(rows, keyword, kho, khoHtml);
        return Results.Content(html, "text/html; charset=utf-8");
    }
    catch (Exception ex)
    {
        var html = "<h2>Lỗi SQL/App</h2><pre>" + WebUtility.HtmlEncode(ex.ToString()) + "</pre>";
        return Results.Content(html, "text/html; charset=utf-8");
    }
});

app.MapGet("/export-excel", async (HttpRequest request, IConfiguration config) =>
{
    var connStr = config.GetConnectionString("Default");

    if (string.IsNullOrWhiteSpace(connStr))
    {
        return Results.Content("<h2>Lỗi: chưa cấu hình Connection String 'Default'.</h2>", "text/html; charset=utf-8");
    }

    var keyword = request.Query["q"].ToString();
    var kho = request.Query["kho"].ToString();

    try
    {
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var sql = @"
            SELECT
                MaSoCuon,
                MaGiay,
                TrongLuongCon,
                KhoGiay
            FROM gc.vw_TonKhoGiayCuon_Chuahet
            WHERE (@kw = '' OR MaGiay LIKE '%' + @kw + '%' OR MaSoCuon LIKE '%' + @kw + '%')
              AND (@kho = '' OR CAST(KhoGiay AS nvarchar(50)) = @kho)
            ORDER BY MaGiay, MaSoCuon";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@kw", keyword ?? "");
        cmd.Parameters.AddWithValue("@kho", kho ?? "");

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

        ws.Range(1, 1, Math.Max(row - 1, 1), 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(1, 1, Math.Max(row - 1, 1), 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        var bytes = stream.ToArray();

        return Results.File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "TonKho.xlsx"
        );
    }
    catch (Exception ex)
    {
        var html = "<h2>Lỗi xuất Excel</h2><pre>" + WebUtility.HtmlEncode(ex.ToString()) + "</pre>";
        return Results.Content(html, "text/html; charset=utf-8");
    }
});

app.Run();
