# Tài liệu Database – SWD_Group4 (BookStoreDB)

Nguồn tham chiếu:
- `DB.sql`
- EF Core context `SWD_Group4.DataAccess\Context\BookStoreContext.cs`
- Feature diagrams:
  - `feature\register.png`
  - `feature\verifySellerInfo.png`

## 1) Tổng quan
- **DBMS**: Microsoft SQL Server
- **Database**: `BookStoreDB`
- Mục tiêu: lưu trữ dữ liệu cho hệ thống bán sách (user, sách, danh mục, khuyến mãi, đơn hàng, hoàn tiền) và luồng **xác minh thông tin seller** (feature).

## 2) Cấu hình kết nối (Connection String)
- Tên connection string: `DBDefault`
- Hiện đang khai báo tại: `SWD_Group4.Presentation\appsettings.Development.json`

Ví dụ (dev):
```json
"ConnectionStrings": {
  "DBDefault": "Data Source=localhost;Initial Catalog=BookStoreDB;User ID=sa;Password=123;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Ghi chú:
- `BookStoreContext.OnConfiguring()` đang load **`appsettings.json`** theo `Directory.GetCurrentDirectory()`. Khi chạy project, hãy đảm bảo file `appsettings.json` ở working directory có section `ConnectionStrings:DBDefault` (hoặc chỉnh lại cách load config theo environment).
- Không nên để mật khẩu thật trong repo; nên dùng Secret Manager / biến môi trường.

## 3) Cách tạo DB
Chạy script `DB.sql` trên SQL Server (SSMS/Azure Data Studio), script sẽ:
1. `CREATE DATABASE BookStoreDB`
2. `USE BookStoreDB`
3. `CREATE TABLE ...`

Ghi chú phạm vi:
- `DB.sql` hiện tạo các bảng lõi: `User`, `Category`, `Book`, `BookCategory`, `Promotion`, `Order`, `OrderItem`, `RefundRequest`, `VerificationRequest`.
- Thiết kế mới: **không có bảng `Seller`**. Thông tin seller (`url`, `citizen_id`, `bank_account`, `bank_name`, `citizen_image`, `citizen_image_back`) là các cột **optional** trên `[User]`.
- Luồng Verify Seller: user submit `VerificationRequest` (lưu payload chờ duyệt). Khi duyệt **APPROVED** thì copy dữ liệu sang `[User]` (không đổi role). Riêng `bank_card_image` chỉ lưu trên `VerificationRequest` để admin review.

## 4) ERD (mô tả quan hệ)
### 4.1) Các quan hệ đang có trong `DB.sql`
- `User (1) — (N) Book` qua `Book.seller_id`
- `Book (N) — (N) Category` qua bảng nối `BookCategory`
- `User (1) — (N) Promotion` qua `Promotion.created_by`
- `User (1) — (N) Order` qua `Order.customer_id`
- `User (1) — (N) Order` qua `Order.seller_id`
- `Promotion (1) — (N) Order` qua `Order.promotion_id` (nullable)
- `Order (1) — (N) OrderItem` qua `OrderItem.order_id`
- `Book (1) — (N) OrderItem` qua `OrderItem.book_id`
- `Order (1) — (N) RefundRequest` qua `RefundRequest.order_id`

### 4.2) Quan hệ theo feature `verifySellerInfo.png` (thiết kế đã cập nhật)
- `User (1) — (N) VerificationRequest` qua `VerificationRequest.user_id`
- Khi request được **APPROVED**: copy các field seller từ `VerificationRequest` → `[User]` (không đổi role)

## 5) Data Dictionary (bảng & cột)

### 5.1) Bảng `[User]`
Mục đích: lưu thông tin người dùng (Customer/Admin).

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| `id` | `INT IDENTITY` | PK | Khóa chính |
| `name` | `NVARCHAR(255)` | UNIQUE, NOT NULL | Username (UC Register) |
| `email` | `NVARCHAR(255)` | UNIQUE, NOT NULL | Email đăng nhập |
| `password` | `NVARCHAR(255)` | NOT NULL | Mật khẩu (lưu hash BCrypt) |
| `role` | `NVARCHAR(20)` | CHECK | `Customer` / `Seller` / `Admin` |
| `status` | `NVARCHAR(20)` | CHECK | `Active` / `Inactive` / `Suspended` |
| `url` | `NVARCHAR(500)` | NULL | URL shop/profile (chỉ có sau khi duyệt) |
| `citizen_id` | `NVARCHAR(50)` | UNIQUE (filtered), NULL | CCCD/CMND (cho phép nhiều NULL) |
| `bank_account` | `NVARCHAR(50)` | UNIQUE (filtered), NULL | Số tài khoản (cho phép nhiều NULL) |
| `bank_name` | `NVARCHAR(255)` | NULL | Tên ngân hàng |
| `citizen_image` | `NVARCHAR(1000)` | NULL | CCCD front image URL (presigned) |
| `citizen_image_back` | `NVARCHAR(1000)` | NULL | CCCD back image URL (presigned) |
| `suspension_end_at` | `DATETIME` | NULL | Nếu khác NULL và > NOW thì seller đang bị suspend đến thời điểm này |

### 5.2) Bảng `Category`
Mục đích: danh mục sách.

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| `id` | `INT IDENTITY` | PK | |
| `name` | `NVARCHAR(255)` | NULL | |
| `description` | `NVARCHAR(500)` | NULL | |

### 5.3) Bảng `Book`
Mục đích: thông tin sách do Seller đăng.

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| `id` | `INT IDENTITY` | PK | |
| `seller_id` | `INT` | FK → `[User](id)` | Seller sở hữu sách |
| `title` | `NVARCHAR(255)` | NULL | |
| `author` | `NVARCHAR(255)` | NULL | |
| `price` | `FLOAT` | NULL | Giá (khuyến nghị dùng DECIMAL cho tiền) |
| `description` | `NVARCHAR(MAX)` | NULL | |
| `status` | `NVARCHAR(20)` | CHECK | `Hidden` / `Pending` |
| `stock_quantity` | `INT` | NULL | Số lượng tồn |

### 5.4) Bảng `BookCategory` (bảng nối N–N)
Mục đích: map sách với danh mục.

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| `book_id` | `INT` | PK, FK → `Book(id)` | |
| `category_id` | `INT` | PK, FK → `Category(id)` | |

Khóa chính kép: (`book_id`, `category_id`).

### 5.5) Bảng `Promotion`
Mục đích: khuyến mãi do user tạo (platform/shop).

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| `id` | `INT IDENTITY` | PK | |
| `created_by` | `INT` | FK → `[User](id)` | Người tạo |
| `promotion_name` | `NVARCHAR(255)` | NULL | |
| `discount_type` | `NVARCHAR(20)` | CHECK | `Percentage` / `FixedAmount` |
| `discount_value` | `FLOAT` | NULL | |
| `promotion_scope` | `NVARCHAR(20)` | CHECK | `Platform` / `Shop` |
| `status` | `NVARCHAR(20)` | CHECK | `Active` / `Inactive` |
| `start_date` | `DATETIME` | NULL | |
| `end_date` | `DATETIME` | NULL | |

### 5.6) Bảng `[Order]`
Mục đích: đơn hàng giữa Customer và Seller.

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| `id` | `INT IDENTITY` | PK | |
| `customer_id` | `INT` | FK → `[User](id)` | |
| `seller_id` | `INT` | FK → `[User](id)` | |
| `promotion_id` | `INT` | FK → `Promotion(id)`, NULL | Có thể không áp khuyến mãi |
| `total_price` | `FLOAT` | NULL | |
| `status` | `NVARCHAR(20)` | CHECK | `Pending` / `Confirmed` / `Rejected` / `Finished` / `Refunded` |
| `payment_method` | `NVARCHAR(20)` | CHECK | `Cash` / `BankTransfer` |
| `payment_transaction_id` | `NVARCHAR(255)` | NULL | Mã giao dịch |
| `payment_date` | `DATETIME` | NULL | |
| `address` | `NVARCHAR(500)` | NULL | Địa chỉ giao |

### 5.7) Bảng `OrderItem`
Mục đích: các dòng hàng trong đơn.

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| `id` | `INT IDENTITY` | PK | |
| `order_id` | `INT` | FK → `[Order](id)` | |
| `book_id` | `INT` | FK → `Book(id)` | |
| `quantity` | `INT` | NULL | |
| `price` | `FLOAT` | NULL | Giá tại thời điểm mua |

### 5.8) Bảng `RefundRequest`
Mục đích: yêu cầu hoàn tiền theo đơn.

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| `id` | `INT IDENTITY` | PK | |
| `order_id` | `INT` | FK → `[Order](id)` | |
| `reason` | `NVARCHAR(500)` | NULL | Lý do |
| `status` | `NVARCHAR(20)` | CHECK | `Pending` / `Approved` / `Processed` / `Rejected` / `Failed` |

### 5.9) Bảng `VerificationRequest` (feature – Verify Seller)
Mục đích: lưu request xác minh seller (submit/approve/reject) và payload thông tin seller chờ duyệt.

Enum trạng thái: `PROCESS`, `APPROVED`, `REJECTED`.

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| `id` | `INT IDENTITY` | PK | |
| `user_id` | `INT` | FK → `[User](id)` | User gửi yêu cầu |
| `status` | `NVARCHAR(20)` | CHECK | `PROCESS` / `APPROVED` / `REJECTED` |
| `url` | `NVARCHAR(500)` | NULL | Payload submit |
| `citizen_id` | `NVARCHAR(50)` | NULL | Payload submit |
| `bank_account` | `NVARCHAR(50)` | NULL | Payload submit |
| `bank_name` | `NVARCHAR(255)` | NULL | Payload submit |
| `citizen_image` | `NVARCHAR(1000)` | NULL | CCCD front image URL (payload) |
| `citizen_image_back` | `NVARCHAR(1000)` | NULL | CCCD back image URL (payload) |
| `bank_card_image` | `NVARCHAR(1000)` | NULL | Bank card image URL (payload) |
| `created_at` | `DATETIME` | DEFAULT GETDATE() | |
| `updated_at` | `DATETIME` | NULL | |
| `approved` | `BIT` | NULL | Có thể dư thừa với `status` |
| `reason` | `NVARCHAR(500)` | NULL | Lý do reject/ghi chú |

Ghi chú: khi request được **APPROVED**, hệ thống copy các field (`url`, `citizen_id`, `bank_*`, `citizen_image`, `citizen_image_back`) sang `[User]` (không đổi role). `bank_card_image` vẫn được giữ trên `VerificationRequest` để phục vụ audit/review.

## 6) Lưu ý triển khai
- Các cột tiền (`price`, `total_price`, `discount_value`) đang dùng `FLOAT` → có thể sai số; production nên chuyển sang `DECIMAL(18,2)`.
- `User.password` nên lưu **hash + salt**, không lưu plaintext.
- Nên bổ sung index theo nhu cầu query (vd: `Book.seller_id`, `Order.customer_id`, `Order.seller_id`, `OrderItem.order_id`).
