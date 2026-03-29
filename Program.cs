using System.Text;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", async context =>
{
    var html = """
    <!DOCTYPE html>
    <html lang="vi">
    <head>
        <meta charset="UTF-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>DGPack Tồn Kho</title>
        <style>
            body {
                font-family: Arial, sans-serif;
                margin: 0;
                background: #f5f7fb;
                color: #222;
            }
            .header {
                background: #0f4c81;
                color: white;
                padding: 20px;
                text-align: center;
            }
            .container {
                max-width: 1000px;
                margin: 30px auto;
                padding: 0 16px;
            }
            .card {
                background: white;
                border-radius: 12px;
                padding: 20px;
                box-shadow: 0 2px 10px rgba(0,0,0,0.08);
                margin-bottom: 20px;
            }
            h1, h2 {
                margin-top: 0;
            }
            .btn {
                display: inline-block;
                background: #0f4c81;
                color: white;
                padding: 10px 16px;
                border-radius: 8px;
                text-decoration: none;
                margin-top: 10px;
            }
            table {
                width: 100%;
                border-collapse: collapse;
                margin-top: 10px;
            }
            th, td {
                border: 1px solid #ddd;
                padding: 10px;
                text-align: left;
            }
            th {
                background: #eef4fb;
            }
            .footer {
                text-align: center;
                color: #666;
                padding: 20px;
                font-size: 14px;
            }
        </style>
    </head>
    <body>
        <div class="header">
            <h1>DGPack - Hệ thống tồn kho</h1>
            <div>Web app chạy trên Azure App Service</div>
        </div>

        <div class="container">
            <div class="card">
                <h2>Trạng thái</h2>
                <p>Ứng dụng đã chạy thành công.</p>
                <p>Bước tiếp theo là kết nối Azure SQL để đọc dữ liệu tồn kho thật.</p>
            </div>

            <div class="card">
                <h2>Dữ liệu mẫu</h2>
                <table>
                    <thead>
                        <tr>
                            <th>Mã giấy</th>
                            <th>Tên giấy</th>
                            <th>Khổ</th>
                            <th>Số lượng tồn</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>G001</td>
                            <td>Giấy mặt trắng</td>
                            <td>1200</td>
                            <td>25</td>
                        </tr>
                        <tr>
                            <td>G002</td>
                            <td>Giấy sóng B</td>
                            <td>1400</td>
                            <td>18</td>
                        </tr>
                        <tr>
                            <td>G003</td>
                            <td>Giấy kraft</td>
                            <td>1600</td>
                            <td>42</td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <div class="card">
                <h2>Bản hiện tại</h2>
                <p>Đây là bản V1 tối giản để deploy thành công trước.</p>
                <p>Sau khi web lên, tôi sẽ đưa tiếp bản V2 có:</p>
                <ul>
                    <li>Kết nối Azure SQL</li>
                    <li>Đọc dữ liệu tồn kho thật</li>
                    <li>Ô tìm kiếm</li>
                    <li>Bảng dữ liệu động</li>
                </ul>
            </div>
        </div>

        <div class="footer">
            © DGPack TonKho WebApp
        </div>
    </body>
    </html>
    """;

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(html, Encoding.UTF8);
});

app.Run();
