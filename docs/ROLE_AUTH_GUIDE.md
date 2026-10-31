# Customer, merchant và admin

## Cách cấp quyền

- Email/password và Google cùng dùng một hệ thống tài khoản. Người tự đăng ký mới nhận `customer`.
- Đăng nhập đọc role trong database. Đăng nhập Google vào tài khoản đã có giữ nguyên role.
- Khách chưa đăng nhập vẫn tìm địa điểm, xem chi tiết, danh mục và tiện ích như trước.
- Admin cấp tài khoản merchant, chọn quán được quản lý và gửi email mời merchant tự đặt mật khẩu.
- Merchant quản lý một hoặc nhiều quán. Mỗi quán có tối đa một merchant, lưu bằng `Venues.MerchantUserId`. `CreatedByUserId` vẫn là người tạo dữ liệu.
- Subscription là nghiệp vụ riêng, không được tự động cấp kèm role merchant.

## Triển khai database và admin đầu tiên

Thay đổi này có migration `AddVenueMerchantOwnership`. Phải áp dụng migration trước khi phiên bản mới truy vấn bảng Venues.

Chọn một trong hai cách:

1. Chạy migration bằng EF CLI với connection string của đúng môi trường:

   ```powershell
   dotnet ef database update --project DAL --startup-project API
   ```

2. Trên Render, đặt `Database__ApplyMigrationsOnStartup=true` để ứng dụng áp dụng các migration còn thiếu trước khi khởi tạo role. Sau deploy thành công có thể đặt lại `false`.

Không chạy lệnh cập nhật database nếu chưa kiểm tra môi trường đích. Bộ kiểm tra tự động bên dưới dùng database tạm, không chạy lệnh này.

Để cấp các admin ban đầu, đặt biến môi trường (email trong cấu hình local đã được thêm theo danh sách bạn chỉ định):

```text
RoleSetup__AdminEmails__0=hungthai1804@gmail.com
RoleSetup__AdminEmails__1=hoanglan141105@gmail.com
RoleSetup__AdminEmails__2=nguyenbuidangkhoixt@gmail.com
RoleSetup__AdminEmails__3=minhtruong300605@gmail.com
RoleSetup__AdminEmails__4=lthanhnha2005@gmail.com
```

Tài khoản phải tồn tại, đang hoạt động và đã xác minh email. Startup cấp admin cho đúng các email cấu hình, kể cả khi một admin khác đã được khởi tạo. Nếu email nào chưa đủ điều kiện, ứng dụng ghi log tên email đó và bỏ qua. Không tạo mật khẩu, không chọn user đầu tiên làm admin, không dùng tên hiển thị để cấp quyền. `RoleSetup__AdminEmail` vẫn hỗ trợ cấu hình một tài khoản nếu cần.

Gỡ các biến bootstrap sau khi cấp đủ admin. Khi vẫn giữ biến, startup sẽ cấp lại admin cho email được chỉ định nếu trước đó bị đổi role. Các tài khoản đã có đúng role admin không bị đổi security stamp mỗi lần restart. Admin đăng nhập lại rồi sử dụng API quản trị để cấp quyền tiếp.

Startup tạo ba role nếu chưa có và gán `customer` cho các tài khoản cũ chưa có bất kỳ role nào. Tài khoản đang có role không bị ghi đè.

Không cần sửa hoặc commit `API/appsettings.json`. Tiếp tục dùng biến môi trường hoặc User Secrets.

## Hợp đồng đăng nhập cho FE

Endpoint cũ không đổi:

- `POST /api/v1/auth/register`: `fullName`, `email`, `password`. Không nhận quyền do người dùng chọn.
- `POST /api/v1/auth/login`: `email`, `password`.
- `POST /api/v1/auth/google`: `idToken`.

Response của cả hai cách đăng nhập thêm `user.roles`, giữ các field cũ:

```json
{
  "accessToken": "...",
  "tokenType": "Bearer",
  "expiresAtUtc": "...",
  "user": {
    "userId": "...",
    "email": "...",
    "fullName": "...",
    "roles": ["merchant"]
  }
}
```

JWT có role claims. `GET /api/v1/auth/me` trả thông tin và role hiện tại, cần Bearer token.

FE điều hướng theo thứ tự admin, merchant, customer và xử lý 401 bằng đăng nhập lại. Giao diện không quyết định quyền API. Token cũ từ phiên bản chưa có role cũng cần đăng nhập lại.

## API admin

Tất cả route sau cần Bearer token có role `admin`.

| Method | Route | Mục đích |
| --- | --- | --- |
| GET | `/api/v1/admin/users?page=1&pageSize=20` | Danh sách tài khoản, thông tin xác minh và role. Không trả password hash, token hay security stamp. |
| PUT | `/api/v1/admin/users/{userId}/role` | Đặt một role cho tài khoản, body `{"role":"merchant"}`. Chỉ nhận `customer`, `merchant`, `admin`. |
| POST | `/api/v1/admin/merchants` | Tạo merchant chưa có mật khẩu, gán quán và gửi lời mời. |
| POST | `/api/v1/admin/merchants/{userId}/resend-invitation` | Gửi lại lời mời cho merchant chưa kích hoạt. |
| PUT | `/api/v1/admin/merchants/{userId}/venues` | Thay toàn bộ danh sách quán của merchant. Body `{"venueIds":["GUID"]}`; mảng rỗng gỡ toàn bộ quán. |

Body cấp merchant:

```json
{
  "fullName": "Chủ quán",
  "email": "owner@example.com",
  "venueIds": ["GUID-quán-trong-database"]
}
```

- `venueIds` phải có ít nhất một quán tồn tại và chưa được giao cho merchant khác.
- Email đã tồn tại trả 409. Với customer đã có tài khoản và xác minh email, admin đổi role sang merchant rồi gán quán.
- Khi email provider lỗi sau khi đã lưu merchant, API trả 503 và giữ tài khoản/quán đã gán. Tìm user qua danh sách admin rồi gọi resend-invitation. Không tạo lại cùng email.
- Đổi quyền nâng cao yêu cầu tài khoản hoạt động và đã xác minh email. Trường hợp tạo merchant mới có quy trình mời riêng.
- Đổi role thu hồi token cũ. Hạ merchant sang customer hoặc chuyển sang admin sẽ gỡ liên kết quán.
- Admin không tự hạ quyền của mình. API cũng bảo vệ admin cuối cùng.

## Trang FE cần bổ sung: kích hoạt merchant

Email mời mở:

```text
<ClientApp:BaseUrl>/auth/activate-merchant?userId=...&token=...
```

FE tạo trang `/auth/activate-merchant`, đọc userId/token từ URL, cho nhập và xác nhận mật khẩu. Gửi:

```http
POST /api/v1/auth/activate-merchant
Content-Type: application/json
```

```json
{
  "userId": "...",
  "token": "giá-trị-token-nguyên-bản-trong-query",
  "password": "mật-khẩu-do-merchant-tự-đặt"
}
```

Không tự decode Base64 token ở FE. BE xác minh lời mời, đặt mật khẩu và xác minh email trong cùng transaction. Sau 200, chuyển về đăng nhập.

Mật khẩu mới cần ít nhất 8 ký tự, gồm chữ hoa, chữ thường và số. Lời mời có hiệu lực 3 giờ và không dùng lại sau kích hoạt. Không lưu token trong analytics/log. Data Protection cần giữ key qua các lần restart để liên kết còn hiệu lực; môi trường mất key khi redeploy cần gửi lại lời mời/xác minh.

BE chưa thêm giao diện FE trong repository này. Có thể kiểm tra kích hoạt trên Swagger bằng token trong email trước khi trang FE hoàn thành.

## API merchant

Các route sau cần Bearer token `merchant`:

- `GET /api/v1/merchant/venues`: chỉ trả các quán của tài khoản đang đăng nhập, gồm quán đang chờ duyệt.
- `PUT /api/v1/merchant/venues/{venueId}`: cập nhật thông tin cơ bản cho quán của chính merchant.

Body PUT gồm toàn bộ `name`, `address` và các field tùy chọn `description`, `priceMin`, `priceMax`, `priceNote`, `phoneNumber`. Field tùy chọn không gửi sẽ được xóa về null. Không nhận `merchantUserId`, `status`, role hoặc các trường quản trị.

Merchant không có quyền đổi tọa độ, giờ mở cửa, ảnh, tiện ích hoặc trạng thái qua endpoint này. Các nghiệp vụ này cần endpoint riêng nếu bổ sung sau. Seed không ghi đè thông tin cơ bản của quán đã giao cho merchant.

401: chưa đăng nhập, token hết hạn/thu hồi, tài khoản bị khóa hoặc role đã đổi. 403: token hợp lệ nhưng sai role. 404 khi merchant sửa quán không thuộc mình.

## Kiểm tra tự động

```powershell
dotnet run --project Tests/RoleFlowChecks/RoleFlowChecks.csproj
```

Chạy HTTP server với JWT, Identity và SQLite in-memory riêng; email sender ghi nhận nội dung trong bộ nhớ. Không dùng SQL Server hoặc Mailjet thật. Kiểm tra đăng ký/xác minh/login customer, bootstrap admin, giới hạn 401/403, cấp/kích hoạt merchant, phục hồi lỗi gửi mail, quyền sở hữu quán, validation, thay role và token cũ.

Kiểm tra này không chứng minh Google OAuth thật, email Mailjet thực tế hoặc UI FE đã chạy end-to-end. Các phần đó phải thử lại sau khi triển khai và tích hợp FE.
