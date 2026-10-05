# English Center API

Backend ASP.NET Core 8 cho hệ thống quản lý trung tâm tiếng Anh. Dự án dùng SQL Server, Dapper, JWT, Redis cache, Serilog, Swagger và xUnit.

## Chạy ở máy cá nhân

1. Cấu hình `ConnectionStrings:DefaultConnection`, `Jwt`, `EmailSettings` và `VNPay` bằng User Secrets hoặc biến môi trường. Các giá trị nhạy cảm đã được đưa ra khỏi `appsettings.json`.
2. Tạo database SQL Server và chạy script dữ liệu tại `Database/Seed_2000.sql` nếu cần dữ liệu lớn.
3. Chạy `dotnet restore`, sau đó `dotnet run`.
4. Mở Swagger tại `https://localhost:7207/swagger`.

## Kiểm thử

Chạy `dotnet test EnglishCenter.Tests/EnglishCenter.Tests.csproj`.

## Health check

- `GET /health/live`: kiểm tra tiến trình API.
- `GET /health`: kiểm tra API và kết nối SQL Server.

## API versioning

API v1 hỗ trợ route `GET /api/v1/Courses`, `POST /api/v1/Auth/login` và các controller còn lại theo cùng mẫu. Route cũ `/api/...` vẫn hoạt động tạm thời để không làm hỏng frontend/Gateway hiện có; response sẽ có header `api-supported-versions: 1.0`.

## Cache và bảo mật

Danh sách khóa học được cache 5 phút bằng Redis khi cấu hình `Redis__ConnectionString`; nếu không có Redis, ứng dụng tự dùng distributed memory cache cho môi trường local. API xác thực được giới hạn 5 request/phút trên mỗi client. Log được ghi ra console và thư mục `logs`. Sau khi xác thực OTP, API trả access token và refresh token; `POST /api/Auth/refresh` xoay refresh token, còn `POST /api/Auth/logout` thu hồi refresh token.

Ví dụ cấu hình local bằng User Secrets (thay giá trị mẫu bằng dữ liệu của bạn):

`dotnet user-secrets set "Jwt:Key" "mot-khoa-ngau-nhien-dai-va-kho-doan"`

## Docker Compose

1. Sao chép `.env.example` thành `.env` và thay giá trị bí mật.
2. Chạy `docker compose up --build`.
3. API chạy ở `http://localhost:7207`.

Docker Compose khởi chạy API, SQL Server và Redis. Cấu trúc database cần được tạo trước theo script SQL của dự án.

## CI/CD

- `.github/workflows/ci.yml`: tự restore, build và test cho mọi push/pull request.
- CD push Docker image tới `namele555/englishcenter` khi push vào nhánh `master`. Trước khi dùng, tạo GitHub Secrets `DOCKERHUB_USERNAME` (giá trị `namele555`) và `DOCKERHUB_TOKEN` (Docker Hub access token). Không đưa token Docker Hub vào source code.
