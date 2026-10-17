# EXE101_BE - Backend FIND&GO

### Link xác minh email

Email đăng ký/gửi lại xác minh mở `GET /api/v1/auth/confirm-email?userId=...&token=...` trên BE. Endpoint xác minh token và hiển thị trang kết quả, kèm liên kết về `/login` của FE. `POST /api/v1/auth/confirm-email` vẫn hỗ trợ client gửi JSON như trước.

- Đặt `Api:PublicBaseUrl` (biến môi trường `Api__PublicBaseUrl`) thành URL BE mà người nhận email truy cập được, ví dụ `https://api.example.com`. Nếu chưa đặt, BE dùng scheme/host/path base của request.
- Đặt `ClientApp:BaseUrl` thành URL FE thực tế để nút về đăng nhập mở đúng trang. URL localhost chỉ dùng được trên chính máy đang chạy ứng dụng.
- Sau khi cập nhật BE, gửi lại email xác minh để nhận link mới; link cũ vẫn trỏ tới route FE cũ. Luồng đặt lại mật khẩu vẫn cần trang FE `/auth/reset-password`.

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

## Tài khoản và xác thực

Các endpoint tài khoản nằm dưới `/api/v1/auth`:

| Method | Endpoint | Mục đích |
| --- | --- | --- |
| POST | `/register` | Tạo tài khoản và gửi email xác minh. |
| POST | `/confirm-email` | Xác minh bằng `userId` và `token` nhận từ link email. |
| POST | `/resend-confirmation` | Gửi lại hướng dẫn xác minh. |
| POST | `/login` | Đăng nhập bằng email/mật khẩu; chỉ tài khoản đã xác minh email mới đăng nhập được. |
| POST | `/logout` | Thu hồi token hiện tại và các token khác của tài khoản bằng cách xoay security stamp. |
| POST | `/forgot-password` | Gửi link đặt lại mật khẩu nếu tài khoản tồn tại. |
| POST | `/reset-password` | Đặt mật khẩu mới bằng email và token từ link. |
| POST | `/google` | Đổi Google ID token do client nhận được thành JWT của FIND&GO. |

Các token xác minh email và đặt lại mật khẩu có hiệu lực 3 giờ. Sau khi thêm hoặc cập nhật code, chạy `dotnet ef database update --project DAL/DAL.csproj --startup-project API/API.csproj` để bảo đảm database có các bảng Identity (migration hiện có đã tạo chúng).

### Cấu hình local (không commit bí mật)

`API/appsettings.example.json` liệt kê các khóa cấu hình cần có. Đặt giá trị thật trong .NET User Secrets hoặc biến môi trường:

- `Jwt:SigningKey`: chuỗi bí mật ít nhất 32 byte; cùng một giá trị phải được dùng ở tất cả môi trường chạy API.
- `Email:Host`, `Email:Port`, `Email:From`, `Email:Username`, `Email:Password`, `Email:EnableSsl`: SMTP của dịch vụ gửi thư. Với nhà cung cấp yêu cầu xác thực, dùng app password/API credential riêng.
- `ClientApp:BaseUrl`: URL gốc của FE. FE cần có trang nhận `/auth/confirm-email` và `/auth/reset-password`, đọc query token rồi gọi endpoint tương ứng.
- `Google:ClientIds`: danh sách OAuth Client ID được FE/mobile dùng để lấy Google ID token. Client gửi ID token tới `POST /api/v1/auth/google`; API xác minh chữ ký, audience và email đã xác minh trước khi cấp JWT.

Ví dụ thiết lập bằng User Secrets (chạy trong thư mục `EXE101_BE`):

```bash
dotnet user-secrets set "Jwt:SigningKey" "<chuoi-bi-mat-it-nhat-32-byte>" --project API/API.csproj
dotnet user-secrets set "Email:Host" "<smtp-host>" --project API/API.csproj
dotnet user-secrets set "Email:Port" "587" --project API/API.csproj
dotnet user-secrets set "Email:From" "<email-gui-di>" --project API/API.csproj
dotnet user-secrets set "Email:Username" "<smtp-username>" --project API/API.csproj
dotnet user-secrets set "Email:Password" "<smtp-app-password>" --project API/API.csproj
dotnet user-secrets set "ClientApp:BaseUrl" "<url-fe-local>" --project API/API.csproj
dotnet user-secrets set "Google:ClientIds:0" "<google-oauth-client-id>" --project API/API.csproj
```

Không commit signing key, SMTP password, Google client secret hoặc ID token. Google Login ở đây nhận ID token từ client nên BE không cần Google client secret.

## Tiến độ BE và việc tiếp theo

### Đã hoàn thành

- [x] Tạo cấu trúc backend 3 tầng và cấu hình `AppDbContext` với Entity Framework Core.
- [x] Tạo migration cho catalog địa điểm và cấu hình nạp dữ liệu seed từ `DAL/Data/venue-seed.json` trong môi trường Development.
- [x] Tạo endpoint `POST /api/v1/search/recommendations` và kiểm tra được bằng Swagger.
- [x] Tạo endpoint `GET /api/v1/venues/{id}` lấy chi tiết quán theo GUID hoặc SourceId (kèm ảnh, lịch mở cửa các thứ trong tuần, tiện ích, vị trí, menu).
- [x] Tạo endpoint `GET /api/v1/venues/categories` và `GET /api/v1/venues/amenities` cung cấp dữ liệu cho bộ lọc Frontend.
- [x] Nạp danh mục và 7 tiện ích chuẩn vào database khi khởi động.
- [x] Thêm lọc kết quả theo các tiêu chí đã có dữ liệu: giờ mở cửa, danh mục, ngân sách và khoảng cách; hỗ trợ phân trang và link Google Maps.
- [x] Tích hợp Gemini 3.8 Flash để phân tích prompt thành tiêu chí tìm kiếm; khi thiếu key hoặc Gemini lỗi, service dùng bộ phân tích quy tắc tiếng Việt dự phòng.
- [x] Kết nối phân tích prompt với search và xác nhận trên Swagger: prompt tự nhiên được Gemini trích xuất, API trả các quán đang mở phù hợp.
- [x] Cấu hình kết nối Azure SQL trên máy phát triển. Connection string là cấu hình riêng, không commit; mỗi thành viên cần tự cấu hình và được cấp quyền truy cập Azure SQL.
- [x] Thêm API đăng ký, xác minh email, đăng nhập/đăng xuất, quên/đặt lại mật khẩu và xác minh Google ID token; các dịch vụ SMTP, JWT và Google cần được cấu hình riêng trước khi dùng.

### Chưa hoàn thành / đề xuất làm tiếp

- [ ] FE làm danh sách cuộn liên tục: gọi các đợt kết quả kế tiếp rồi nối vào danh sách, dùng `hasMore` để biết khi nào dừng; không hiển thị nút chuyển trang.
- [ ] FE lấy vị trí GPS hoặc cho người dùng nhập điểm bắt đầu rồi gửi `origin`; API hiện tính khoảng cách từ `origin` nhưng không tự lấy GPS.
- [ ] Tính năng người dùng: Lưu địa điểm yêu thích (Favorites / Bookmarks).
- [ ] Bổ sung bảng đánh giá thật (Reviews & Ratings) thay cho dữ liệu giả lập.
- [ ] Cấu hình SMTP, JWT signing key và Google OAuth Client ID trên môi trường dùng chung; FE cần nối các màn hình xác minh email/đặt lại mật khẩu và lấy Google ID token.
- [ ] Các thành viên cần cấu hình Gemini API key và quyền truy cập Azure SQL riêng trên máy hoặc môi trường chạy của mình; không commit key/connection string.

### Gợi ý việc tiếp theo

Nhã có thể dùng endpoint search hiện tại để làm infinite scroll. BE vẫn nhận `page`/`pageSize` nội bộ và trả `hasMore`; FE gọi trang kế tiếp khi gần cuối danh sách rồi nối kết quả vào cùng một màn hình. Khôi có thể bổ sung endpoint chi tiết theo `venueId`. Đọc `API/Controllers/SearchController.cs`, DTO request/response và `BLL/Services/VenueSearchService.cs` trước khi sửa; không commit API key hoặc connection string.

## Dữ liệu 40 địa điểm

Nhóm thống nhất dùng **40 địa điểm giả lập** cho bản MVP/demo FIND&GO. Tệp `DAL/Data/venue-seed.json` chứa mã VEN-001 đến VEN-040 cùng dữ liệu mẫu theo PDF/bảng tính của nhóm. Không xem các thông tin này là dữ liệu địa điểm đã xác minh ngoài thực tế.

### Nạp dữ liệu vào SQL Server local

Sau khi lấy code mới nhất, mỗi thành viên cấu hình kết nối tới SQL Server local của mình, rồi chạy từ thư mục `EXE101_BE`:

```bash
dotnet ef database update --project DAL/DAL.csproj --startup-project API/API.csproj
dotnet run --project API/API.csproj
```

Trong môi trường Development, API có thể tự nạp/upsert dữ liệu địa điểm khi `SeedData:ImportVenueCatalogOnStartup` bật. Có thể chạy lại an toàn: mã `SourceId` duy nhất (`VEN-001` ... `VEN-040`) được dùng để cập nhật bản ghi thay vì nhân đôi. Các địa điểm seed có trạng thái `Pending`; để dùng bộ dữ liệu demo khi search, đặt `Search:IncludePendingVenues` thành `true` trong cấu hình Development local. Mỗi người có thể dùng database riêng; tệp JSON được chia sẻ qua Git.

### Giờ hoạt động và tìm kiếm thử nghiệm

PDF có giờ mở cửa cho 37 mã địa điểm. Đã đưa lịch dùng được của 36 quán vào seed: `VEN-001`–`VEN-004`, `VEN-006`–`VEN-018`, `VEN-020`–`VEN-038`. `VEN-005` có lịch ghi trong bảng nhưng bị loại khỏi lọc giờ; không có lịch cho `VEN-019`, `VEN-039` và `VEN-040`. `VEN-009` chưa có giờ thứ Hai; `VEN-033` chưa có giờ thứ Ba. Một số lịch có giờ đóng sau nửa đêm. Đây là dữ liệu giả lập phục vụ demo/test.

Trong môi trường Development, search cho phép xem địa điểm `Pending` khi `Search:IncludePendingVenues` bật; kết quả chỉ gồm quán đang mở theo lịch mẫu. Endpoint là `POST /api/v1/search/recommendations`. `page` mặc định là 1, `pageSize` mặc định là 10; FE có thể dùng `hasMore` để tải thêm và nối kết quả trong giao diện cuộn liên tục. Gemini phân tích prompt thành category, purpose, atmosphere, amenities, budget, distance và group size; hiện giờ mở, danh mục, ngân sách, khoảng cách và tiện ích được dùng để lọc, còn purpose/atmosphere dùng để xếp hạng. Prompt đã được thử trên Swagger với Gemini.

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
