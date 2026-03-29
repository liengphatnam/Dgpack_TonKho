using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using ClosedXML.Excel;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string BuildConnectionErrorHtml()
{
    return "<h2>Lỗi: chưa cấu hình Connection String 'Default'.</h2>";
}

string BuildQuery(string includeTop)
{
    return $@"
        SELECT {includeTop}
            MaSoCuon,
            MaGiay,
            TrongLuongCon,
            KhoGiay
        FROM gc.vw_TonKhoGiayCuon_Chuahet
        WHERE (@kw = '' OR MaGiay LIKE '%' + @kw + '%' OR MaSoCuon LIKE '%' + @kw + '%')
          AND (@kho = '' OR CAST(KhoGiay AS nvarchar(50)) = @kho)
        ORDER BY MaGiay, MaSoCuon";
}

async Task<List<string>> GetKhoOptions(SqlConnection conn)
{
    var list = new List<string>();

    using var cmd = new SqlCommand(
        "SELECT DISTINCT CAST(KhoGiay AS nvarchar(50)) AS KhoGiay FROM gc.vw_TonKhoGiayCuon_Chuahet ORDER BY CAST(KhoGiay AS nvarchar(50))",
        conn);

    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(reader["KhoGiay"]?.ToString() ?? "");
    }

    return list;
}

app.MapGet("/", async (HttpRequest request, IConfiguration config) =>
{
    var connStr = config.GetConnectionString("Default");

    if (string.IsNullOrWhiteSpace(connStr))
    {
        return Results.Content(BuildConnectionErrorHtml(), "text/html; charset=utf-8");
    }

    var keyword = request.Query["q"].ToString();
    var kho = request.Query["kho"].ToString();

    var rows = new List<string>();
    var khoOptions = new List<string>();

    try
    {
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        khoOptions = await GetKhoOptions(conn);

        using var cmd = new SqlCommand(BuildQuery("TOP 200"), conn);
        cmd.Parameters.AddWithValue("@kw", keyword ?? "");
        cmd.Parameters.AddWithValue("@kho", kho ?? "");

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var maSoCuon = reader["MaSoCuon"]?.ToString() ?? "";
            var maGiay = reader["MaGiay"]?.ToString() ?? "";
            var trongLuongCon = reader["TrongLuongCon"]?.ToString() ?? "";
            var khoGiay = reader["KhoGiay"]?.ToString() ?? "";

            rows.Add(
                "<tr>"
                + "<td>" + WebUtility.HtmlEncode(maSoCuon) + "</td>"
                + "<td>" + WebUtility.HtmlEncode(maGiay) + "</td>"
                + "<td>" + WebUtility.HtmlEncode(trongLuongCon) + "</td>"
                + "<td>" + WebUtility.HtmlEncode(khoGiay) + "</td>"
                + "</tr>"
            );
        }

        var khoHtml = "<option value=''>-- Tất cả kho --</option>";
        foreach (var k in khoOptions)
        {
            var selected = k == kho ? " selected" : "";
            khoHtml += "<option value='" + WebUtility.HtmlEncode(k) + "'" + selected + ">"
                    + WebUtility.HtmlEncode(k) + "</option>";
        }

        var exportUrl = "/export/excel?q=" + Uri.EscapeDataString(keyword ?? "") + "&kho=" + Uri.EscapeDataString(kho ?? "");

        var html = ""
            + "<html>"
            + "<head>"
            + "<meta charset='utf-8' />"
            + "<meta name='viewport' content='width=device-width, initial-scale=1' />"
            + "<title>DGPack - Tồn kho</title>"
            + "<style>"
            + "body{font-family:Arial;padding:12px;margin:0;background:#f5f7fb;}"
            + ".wrap{max-width:1200px;margin:0 auto;}"
            + ".card{background:#fff;border-radius:12px;padding:16px;box-shadow:0 2px 10px rgba(0,0,0,.08);}"
            + "h2{margin-top:0;}"
            + "form{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:12px;}"
            + "input,select,button,a.btn{padding:10px 12px;border:1px solid #ccc;border-radius:8px;font-size:14px;text-decoration:none;}"
            + "button,a.btn{cursor:pointer;background:#0f4c81;color:#fff;border-color:#0f4c81;}"
            + "button.print-btn{background:#2d7d46;border-color:#2d7d46;}"
            + "table{border-collapse:collapse;width:100%;background:#fff;}"
            + "th,td{border:1px solid #ddd;padding:8px;text-align:left;font-size:14px;}"
            + "th{background:#eef4fb;position:sticky;top:0;}"
            + ".table-wrap{overflow:auto;max-height:75vh;}"
            + ".note{color:#666;font-size:13px;margin-top:8px;}"
            + "@media print{"
            + "body{background:#fff;padding:0;}"
            + "form,.no-print,.note{display:none !important;}"
            + ".card{box-shadow:none;padding:0;}"
            + "table{width:100%;}"
            + "th,td{font-size:12px;padding:6px;}"
            + "}"
            + "</style>"
            + "</head>"
            + "<body>"
            + "<div class='wrap'>"
            + "<div class='card'>"
            + "<h2>DGPack - Tồn kho giấy cuộn</h2>"
            + "<form method='get'>"
            + "<input name='q' placeholder='Tìm mã giấy hoặc mã số cuộn...' value='" + WebUtility.HtmlEncode(keyword) + "' />"
            + "<select name='kho'>" + khoHtml + "</select>"
            + "<button type='submit'>Lọc</button>"
            + "<a class='btn no-print' href='" + exportUrl + "'>Xuất Excel</a>"
            + "<button type='button' class='print-btn no-print' onclick='window.print()'>In / Lưu PDF</button>"
            + "</form>"
            + "<div class='table-wrap'>"
            + "<table>"
            + "<tr><th>Mã số cuộn</th><th>Mã giấy</th><th>Trọng lượng còn</th><th>Kho</th></tr>"
            + string.Join("", rows)
            + "</table>"
            + "</div>"
            + "<div class='note'>Mẹo: bấm “In / Lưu PDF” để lưu PDF ngay trên điện thoại hoặc máy tính.</div>"
            + "</div>"
            + "</div>"
            + "</body>"
            + "</html>";

        return Results.Content(html, "text/html; charset=utf-8");
    }
    catch (Exception ex)
    {
        var html = "<h2>Lỗi SQL/App</h2><pre>" + WebUtility.HtmlEncode(ex.ToString()) + "</pre>";
        return Results.Content(html, "text/html; charset=utf-8");
    }
});

app.MapGet("/export/excel", async (HttpRequest request, IConfiguration config) =>
{
    var connStr = config.GetConnectionString("Default");

    if (string.IsNullOrWhiteSpace(connStr))
    {
        return Results.Content(BuildConnectionErrorHtml(), "text/html; charset=utf-8");
    }

    var keyword = request.Query["q"].ToString();
    var kho = request.Query["kho"].ToString();

    try
    {
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(BuildQuery(""), conn);
        cmd.Parameters.AddWithValue("@kw", keyword ?? "");
        cmd.Parameters.AddWithValue("@kho", kho ?? "");

        using var reader = await cmd.ExecuteReaderAsync();

        var dt = new DataTable();
        dt.Load(reader);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("TonKho");

        ws.Cell(1, 1).Value = "Mã số cuộn";
        ws.Cell(1, 2).Value = "Mã giấy";
        ws.Cell(1, 3).Value = "Trọng lượng còn";
        ws.Cell(1, 4).Value = "Kho";

        for (int i = 0; i < dt.Rows.Count; i++)
        {
            ws.Cell(i + 2, 1).Value = dt.Rows[i]["MaSoCuon"]?.ToString();
            ws.Cell(i + 2, 2).Value = dt.Rows[i]["MaGiay"]?.ToString();
            ws.Cell(i + 2, 3).Value = dt.Rows[i]["TrongLuongCon"]?.ToString();
            ws.Cell(i + 2, 4).Value = dt.Rows[i]["KhoGiay"]?.ToString();
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        var fileName = "TonKhoGiayCuon.xlsx";
        return Results.File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName
        );
    }
    catch (Exception ex)
    {
        var html = "<h2>Lỗi xuất Excel</h2><pre>" + WebUtility.HtmlEncode(ex.ToString()) + "</pre>";
        return Results.Content(html, "text/html; charset=utf-8");
    }
});

app.Run();
