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

    using var wb = new ClosedXML.Excel.XLWorkbook();
    var ws = wb.Worksheets.Add("TonKho");

    // Header
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
        ws.Cell(row, 4).Value = reader["Kho"]?.ToString();
        row++;
    }

    using var stream = new MemoryStream();
    wb.SaveAs(stream);

    context.Response.Headers.Add("Content-Disposition", "attachment; filename=TonKho.xlsx");
    context.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    await context.Response.Body.WriteAsync(stream.ToArray());
});
