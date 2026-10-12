# EXE101_BE - Web Application Backend

Dự án Backend cho hệ thống **EXE101**, xây dựng theo mô hình **Kiến trúc 3 tầng (3-Tier Architecture)** chuẩn FPT University.

## 🛠️ Công nghệ sử dụng
- **Platform:** .NET 10 (C#)
- **Web API:** ASP.NET Core Web API
- **ORM:** Entity Framework Core 10 (SQL Server)
- **Authentication/Identity:** ASP.NET Core Identity & JWT Bearer
- **Mapping:** AutoMapper 16
- **Documentation:** Swagger / OpenAPI

---

## 🏛️ Cấu trúc thư mục 3 tầng

```text
EXE101_BE/
├── EXE101_BE.slnx
├── .antigravityrules         # Quy chuẩn kiến trúc & luồng phát triển
├── .gitignore                # Git ignore chuẩn cho .NET
├── README.md
│
├── DAL/                      # Tầng 1: Data Access Layer (Truy cập dữ liệu)
│   ├── Data/
│   │   └── AppDbContext.cs   # DbContext chính kế thừa IdentityDbContext
│   ├── Entities/
│   │   ├── BaseEntity.cs     # Class cơ sở chứa Id, CreatedAt, UpdatedAt, IsDeleted
│   │   └── AppUser.cs        # Entity người dùng kế thừa IdentityUser
│   ├── IRepositories/
│   │   ├── IGenericRepository.cs # Interface CRUD chung
│   │   └── IUnitOfWork.cs        # Interface Unit of Work
│   └── Repositories/
│       ├── GenericRepository.cs  # Implementation CRUD chung
│       └── UnitOfWork.cs         # Implementation Unit of Work
│
├── BLL/                      # Tầng 2: Business Logic Layer (Xử lý nghiệp vụ)
│   ├── DTOs/
│   │   ├── Requests/         # Chứa DTO nhận từ Client
│   │   ├── Responses/        # Chứa DTO trả về Client
│   │   └── Common/
│   │       └── ApiResponse.cs# Wrapper chuẩn cho kết quả API
│   ├── Interfaces/           # Chứa các interface Service (IService)
│   ├── Services/             # Chứa logic nghiệp vụ thực thi Service
│   └── Mappings/
│       └── AutoMapperProfile.cs # Cấu hình ánh xạ Entity <-> DTO
│
└── API/                      # Tầng 3: Presentation Layer (Web API)
    ├── Controllers/
    │   └── BaseApiController.cs # Base Controller dùng chung
    ├── Middlewares/          # Exception handling, logging middlewares
    ├── Program.cs            # Cấu hình DI, DbContext, AutoMapper, Swagger, CORS
    └── appsettings.json      # ConnectionString & cấu hình hệ thống
```

---

## 🔄 Luồng thực thi chuẩn (Mandatory Flow)
```text
Client Request 
   ⬇
Controller (API) 
   ⬇
IService / Service (BLL) 
   ⬇
IUnitOfWork / IRepository (DAL) 
   ⬇
AppDbContext 
   ⬇
SQL Server Database
```

---

## 🚀 Hướng dẫn khởi chạy
1. **Kiểm tra build:**
   ```bash
   dotnet build EXE101_BE.slnx
   ```
2. **Cập nhật chuỗi kết nối:**
   Mở `API/appsettings.json` và sửa `DefaultConnection` cho phù hợp với máy của bạn.
3. **Chạy API:**
   ```bash
   dotnet run --project API/API.csproj
   ```
4. **Truy cập Swagger:**
   Mở trình duyệt vào đường dẫn `https://localhost:7297/swagger` để test các endpoint.
