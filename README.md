# EXE101_BE - Backend FIND&GO

Backend cho dự án **FIND&GO**, được tổ chức theo kiến trúc 3 tầng.

## Công nghệ

- .NET 10 và ASP.NET Core Web API
- Entity Framework Core 10 với SQL Server
- ASP.NET Core Identity và JWT Bearer
- AutoMapper
- Swagger / OpenAPI

## Cấu trúc dự án

```text
EXE101_BE/
├── DAL/                     # Tầng truy cập dữ liệu
│   ├── Data/                # AppDbContext và các migration
│   ├── Entities/            # Các thực thể dữ liệu
│   ├── IRepositories/       # Giao diện repository và Unit of Work
│   └── Repositories/        # Cài đặt repository và Unit of Work
├── BLL/                     # Tầng xử lý nghiệp vụ
│   ├── DTOs/                # Đối tượng nhận và trả dữ liệu
│   ├── Interfaces/          # Giao diện dịch vụ
│   ├── Services/            # Xử lý nghiệp vụ
│   └── Mappings/            # Cấu hình ánh xạ Entity và DTO
├── API/                     # Tầng Web API
│   ├── Controllers/         # Các API endpoint
│   ├── Program.cs           # Đăng ký dịch vụ và cấu hình ứng dụng
│   └── appsettings.json     # Cấu hình ứng dụng
└── README.md
```

Luồng xử lý chính:

```text
Client → Controller (API) → Service (BLL) → Repository / Unit of Work (DAL)
       → AppDbContext → SQL Server
```

## Khởi chạy

Chạy các lệnh từ thư mục `EXE101_BE`.

1. Khôi phục và biên dịch dự án:

   ```bash
   dotnet build EXE101_BE.slnx
   ```

2. Cấu hình `ConnectionStrings:DefaultConnection` cho SQL Server trên máy của bạn. Không đưa mật khẩu hoặc thông tin kết nối riêng vào Git. Có thể dùng .NET User Secrets hoặc biến môi trường `ConnectionStrings__DefaultConnection`.

3. Tạo/cập nhật các bảng trong database:

   ```bash
   dotnet ef database update --project DAL/DAL.csproj --startup-project API/API.csproj
   ```

4. Chạy API:

   ```bash
   dotnet run --project API/API.csproj
   ```

5. Mở địa chỉ Swagger được in trong cửa sổ chạy API, thêm `/swagger` để thử các endpoint.

Migration `InitialVenueCatalog` tạo cấu trúc bảng cho địa điểm, danh mục, thẻ, tiện ích, giờ mở cửa, ngoại lệ giờ hoạt động và ảnh. Migration chỉ tạo/cập nhật **cấu trúc** database; nó không tự thêm dữ liệu địa điểm.

## Tiến độ BE và việc tiếp theo

### Đã hoàn thành

- [x] Tạo cấu trúc backend 3 tầng và cấu hình `AppDbContext` với Entity Framework Core.
- [x] Tạo migration cho catalog địa điểm và cấu hình nạp dữ liệu seed từ `DAL/Data/venue-seed.json` trong môi trường Development.
- [x] Tạo endpoint `POST /api/v1/search/recommendations` và kiểm tra được bằng Swagger.
- [x] Tạo endpoint `GET /api/v1/venues/{id}` lấy chi tiết quán theo GUID hoặc SourceId (kèm ảnh, lịch mở cửa các thứ trong tuần, tiện ích, vị trí, menu).
- [x] Tạo endpoint `GET /api/v1/venues/categories` và `GET /api/v1/venues/amenities` cung cấp dữ liệu cho bộ lọc Frontend.
- [x] Nạp danh mục và 7 tiện ích chuẩn vào database khi khởi động.
- [x] Thêm lọc kết quả theo các tiêu chí đã có dữ liệu: giờ mở cửa, danh mục, ngân sách và khoảng cách; hỗ trợ phân trang và link Google Maps.
- [x] Cấu hình kết nối Azure SQL trên máy phát triển. Connection string là cấu hình riêng, không commit; mỗi thành viên cần tự cấu hình và được cấp quyền truy cập Azure SQL.

### Chưa hoàn thành / đề xuất làm tiếp

- [x] Tích hợp AI (Gemini 3.8 Flash) để phân tích `prompt` tự nhiên thành tiêu chí tìm kiếm có cấu trúc; hỗ trợ cơ chế xoay vòng nhiều API Key (Key Pool Rotation giống LoverApp), tự động đổi key khi hết quota/429/503 và có bộ NLP Rule-based Tiếng Việt dự phòng offline.
- [x] Bổ sung đối chiếu mục đích, không gian, tiện ích và quy mô nhóm với bộ lọc và thuật toán tính điểm gợi ý.
- [ ] Xây dựng hệ thống Xác thực & Tài khoản (ASP.NET Core Identity & JWT Bearer: Register, Login, Refresh Token).
- [ ] Tính năng người dùng: Lưu địa điểm yêu thích (Favorites / Bookmarks).
- [ ] Bổ sung bảng đánh giá thật (Reviews & Ratings) thay cho dữ liệu giả lập.
- [ ] Tích hợp GPS hoặc nhận vị trí bắt đầu do client gửi (nhiệm vụ phía Frontend/Mobile gửi qua `origin`).
- [ ] Bổ sung dữ liệu ảnh, giá, đánh giá và các thông tin còn thiếu khi nhóm có nguồn dữ liệu.
- [ ] Kiểm tra quyền truy cập Azure SQL và cấu hình riêng trên máy từng thành viên trước khi FE gọi API.

### Gợi ý bàn giao cho Khôi

Đọc phần tiến độ này cùng `API/Controllers/SearchController.cs`, service tìm kiếm trong `BLL/Services/`, DTO request/response và `DAL/Data/venue-seed.json`. Việc tiếp theo phù hợp là thiết kế/tạo service phân tích prompt bằng AI, rồi ánh xạ kết quả phân tích vào tiêu chí search hiện có. Trước khi sửa, kiểm tra code hiện tại và thống nhất hợp đồng request/response với FE; không commit API key hoặc connection string.

## Dữ liệu 40 địa điểm

Nhóm đã thu thập **40 địa điểm** cho bản MVP FIND&GO. Tệp `DAL/Data/venue-seed.json` chứa mã VEN-001 đến VEN-040 cùng tên, danh mục, địa chỉ và tọa độ lấy từ PDF/bảng tính. PDF/bảng tính gốc có thể nằm ngoài repository; cần chia sẻ riêng nếu muốn AI hoặc thành viên kiểm tra các trường khác.

### Nạp dữ liệu vào SQL Server local

Sau khi lấy code mới nhất, mỗi thành viên cấu hình kết nối tới SQL Server local của mình, rồi chạy từ thư mục `EXE101_BE`:

```bash
dotnet ef database update --project DAL/DAL.csproj --startup-project API/API.csproj
dotnet run --project API/API.csproj
```

Trong môi trường Development, API sẽ tự nạp/upsert dữ liệu địa điểm khi khởi động. Có thể chạy lại an toàn: mã `SourceId` duy nhất (`VEN-001` ... `VEN-040`) được dùng để cập nhật bản ghi thay vì nhân đôi. Mỗi người có database local riêng; tệp JSON được chia sẻ qua Git. Các địa điểm được tạo ở trạng thái `Pending` vì dữ liệu nguồn ghi là sơ bộ/cần xác minh. Giá, tiện ích, ảnh và các trường chưa có căn cứ chưa được tự điền.

### Giờ hoạt động và tìm kiếm thử nghiệm

PDF có giờ mở cửa cho 37 mã địa điểm. Đã đưa lịch dùng được của 36 quán vào seed: `VEN-001`–`VEN-004`, `VEN-006`–`VEN-018`, `VEN-020`–`VEN-038`. `VEN-005` có lịch ghi trong bảng nhưng ghi chú cùng PDF nói chưa thấy giờ rõ nên bị loại khỏi lọc giờ. Không có lịch cho `VEN-019`, `VEN-039` và `VEN-040`. `VEN-009` chưa có giờ thứ Hai; `VEN-033` chưa có giờ thứ Ba. Một số lịch có giờ đóng sau nửa đêm; ghi chú nguồn được giữ trong `VenueOpeningHour.Note`. Toàn bộ lịch vẫn là dữ liệu sơ bộ, chưa được xác minh thực tế.

Trong môi trường Development, search cho phép xem địa điểm `Pending` để thử API và chỉ trả quán đang mở theo lịch đã nhập. Endpoint là `POST /api/v1/search/recommendations`; có thể gửi `{ "page": 1, "pageSize": 10 }` để lấy các kết quả đang mở. API hiện lọc theo giờ, danh mục, ngân sách và khoảng cách; prompt chưa được phân tích bằng AI, còn mục đích/không gian/tiện ích/quy mô nhóm chưa được đối chiếu do dữ liệu tương ứng chưa nạp. Kết quả có link Google Maps theo tên và địa chỉ; ảnh, giá và đánh giá chưa có trong seed.

### Hướng dẫn dành cho AI hỗ trợ nhóm

Đọc README, `DAL/Data/venue-seed.json`, entity và migration trước khi sửa code/dữ liệu. Nếu cần kiểm tra hoặc bổ sung trường không có trong JSON, yêu cầu tệp PDF/bảng tính nguồn; không giả định có thể đọc đường dẫn ổ đĩa của thành viên khác.

- Nhập đủ 40 địa điểm, giữ ổn định mã địa điểm từ nguồn và không tạo bản ghi trùng khi chạy lại.
- Đối chiếu cột trong nguồn với `DAL/Entities/Venue.cs` và các entity liên quan. Nêu rõ trường nào chưa khớp schema trước khi đề xuất sửa schema.
- Không tự suy đoán hoặc bịa dữ liệu. Giá trị thiếu/chưa xác minh phải để trống hoặc giữ trạng thái chưa xác minh; không tự đánh dấu địa điểm đã công khai/được xác minh chỉ vì có giá trị trong nguồn.
- Giữ lại nguồn gốc dữ liệu và ghi chú kiểm chứng nếu nguồn có cung cấp; nêu các thông tin mâu thuẫn hoặc còn thiếu để nhóm rà soát.
- Không ghi chuỗi kết nối riêng của máy, mật khẩu hoặc bí mật vào tệp seed hay commit lên Git.
- Tệp seed/quy trình nhập phải được chia sẻ qua Git; bản thân database vẫn nằm trên từng máy trừ khi nhóm quyết định dùng database được host chung.
- Kiểm tra sau khi nhập có đúng 40 địa điểm và chạy lại không làm phát sinh bản ghi trùng.

Nếu thay đổi tệp seed hoặc cách nạp dữ liệu, cập nhật README và giữ quy trình chạy lặp lại an toàn, không làm mất các chỉnh sửa khác của nhóm.
