IF DB_ID(N'BookStoreDB') IS NULL
BEGIN
    CREATE DATABASE BookStoreDB;
END
GO

USE BookStoreDB;
GO

/*
DB_TEAM.sql
- Fresh schema matching current local structure
- Includes Seller verification + suspension fields
- Seeds a few pending VerificationRequest rows (PROCESS) for demo

NOTE: Seeded passwords are placeholders (NOT BCrypt). For real login, register via UI then update role/status as needed.
*/

-- Allow re-running
IF OBJECT_ID(N'dbo.VerificationRequest', N'U') IS NOT NULL DROP TABLE dbo.VerificationRequest;
IF OBJECT_ID(N'dbo.RefundRequest', N'U') IS NOT NULL DROP TABLE dbo.RefundRequest;
IF OBJECT_ID(N'dbo.OrderItem', N'U') IS NOT NULL DROP TABLE dbo.OrderItem;
IF OBJECT_ID(N'dbo.BookCategory', N'U') IS NOT NULL DROP TABLE dbo.BookCategory;
IF OBJECT_ID(N'dbo.[Order]', N'U') IS NOT NULL DROP TABLE dbo.[Order];
IF OBJECT_ID(N'dbo.Promotion', N'U') IS NOT NULL DROP TABLE dbo.Promotion;
IF OBJECT_ID(N'dbo.Book', N'U') IS NOT NULL DROP TABLE dbo.Book;
IF OBJECT_ID(N'dbo.Category', N'U') IS NOT NULL DROP TABLE dbo.Category;
IF OBJECT_ID(N'dbo.[User]', N'U') IS NOT NULL DROP TABLE dbo.[User];
GO

CREATE TABLE dbo.[User] (
    id INT IDENTITY PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    email NVARCHAR(255) NOT NULL,
    password NVARCHAR(255) NOT NULL,
    role NVARCHAR(20) NOT NULL
        CONSTRAINT DF_User_role DEFAULT ('Customer')
        CHECK (role IN ('Customer', 'Seller', 'Admin')),
    status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_User_status DEFAULT ('Active')
        CHECK (status IN ('Active', 'Inactive', 'Suspended')),

    -- Seller info (filled only after verification approval)
    url NVARCHAR(500) NULL,
    citizen_id NVARCHAR(50) NULL,
    bank_account NVARCHAR(50) NULL,
    bank_name NVARCHAR(255) NULL,
    citizen_image NVARCHAR(1000) NULL,
    citizen_image_back NVARCHAR(1000) NULL,

    -- Suspension (nullable; when set & status=Suspended, seller is suspended until this time)
    suspension_end_at DATETIME NULL,

    CONSTRAINT UQ_User_email UNIQUE (email),
    CONSTRAINT UQ_User_name UNIQUE (name)
);
GO

CREATE UNIQUE INDEX UX_User_citizen_id ON dbo.[User](citizen_id) WHERE citizen_id IS NOT NULL;
GO
CREATE UNIQUE INDEX UX_User_bank_account ON dbo.[User](bank_account) WHERE bank_account IS NOT NULL;
GO

CREATE TABLE dbo.Category (
    id INT IDENTITY PRIMARY KEY,
    name NVARCHAR(255) NULL,
    description NVARCHAR(500) NULL
);
GO

CREATE TABLE dbo.Book (
    id INT IDENTITY PRIMARY KEY,
    seller_id INT NULL,
    title NVARCHAR(255) NULL,
    author NVARCHAR(255) NULL,
    price FLOAT NULL,
    description NVARCHAR(MAX) NULL,
    status NVARCHAR(20) NULL CHECK (status IN ('Hidden', 'Pending')),
    stock_quantity INT NULL,

    CONSTRAINT FK_Book_User FOREIGN KEY (seller_id) REFERENCES dbo.[User](id)
);
GO

CREATE TABLE dbo.BookCategory (
    book_id INT NOT NULL,
    category_id INT NOT NULL,
    PRIMARY KEY (book_id, category_id),

    FOREIGN KEY (book_id) REFERENCES dbo.Book(id),
    FOREIGN KEY (category_id) REFERENCES dbo.Category(id)
);
GO

CREATE TABLE dbo.Promotion (
    id INT IDENTITY PRIMARY KEY,
    created_by INT NULL,
    promotion_name NVARCHAR(255) NULL,
    discount_type NVARCHAR(20) NULL CHECK (discount_type IN ('Percentage', 'FixedAmount')),
    discount_value FLOAT NULL,
    promotion_scope NVARCHAR(20) NULL CHECK (promotion_scope IN ('Platform', 'Shop')),
    status NVARCHAR(20) NULL CHECK (status IN ('Active', 'Inactive')),
    start_date DATETIME NULL,
    end_date DATETIME NULL,

    FOREIGN KEY (created_by) REFERENCES dbo.[User](id)
);
GO

CREATE TABLE dbo.[Order] (
    id INT IDENTITY PRIMARY KEY,
    customer_id INT NULL,
    seller_id INT NULL,
    promotion_id INT NULL,
    total_price FLOAT NULL,
    status NVARCHAR(20) NULL CHECK (status IN ('Pending', 'Confirmed', 'Rejected', 'Finished', 'Refunded')),
    payment_method NVARCHAR(20) NULL CHECK (payment_method IN ('Cash', 'BankTransfer')),
    payment_transaction_id NVARCHAR(255) NULL,
    payment_date DATETIME NULL,
    address NVARCHAR(500) NULL,

    FOREIGN KEY (customer_id) REFERENCES dbo.[User](id),
    FOREIGN KEY (seller_id) REFERENCES dbo.[User](id),
    FOREIGN KEY (promotion_id) REFERENCES dbo.Promotion(id)
);
GO

CREATE TABLE dbo.OrderItem (
    id INT IDENTITY PRIMARY KEY,
    order_id INT NULL,
    book_id INT NULL,
    quantity INT NULL,
    price FLOAT NULL,

    FOREIGN KEY (order_id) REFERENCES dbo.[Order](id),
    FOREIGN KEY (book_id) REFERENCES dbo.Book(id)
);
GO

CREATE TABLE dbo.RefundRequest (
    id INT IDENTITY PRIMARY KEY,
    order_id INT NULL,
    reason NVARCHAR(500) NULL,
    status NVARCHAR(20) NULL CHECK (status IN ('Pending', 'Approved', 'Processed', 'Rejected', 'Failed')),

    FOREIGN KEY (order_id) REFERENCES dbo.[Order](id)
);
GO

CREATE TABLE dbo.VerificationRequest (
    id INT IDENTITY PRIMARY KEY,
    user_id INT NOT NULL,
    status NVARCHAR(20) NOT NULL CHECK (status IN ('PROCESS', 'APPROVED', 'REJECTED')),

    url NVARCHAR(500) NULL,
    citizen_id NVARCHAR(50) NULL,
    bank_account NVARCHAR(50) NULL,
    bank_name NVARCHAR(255) NULL,
    citizen_image NVARCHAR(1000) NULL,
    citizen_image_back NVARCHAR(1000) NULL,
    bank_card_image NVARCHAR(1000) NULL,

    created_at DATETIME NOT NULL CONSTRAINT DF_VerificationRequest_created_at DEFAULT (GETDATE()),
    updated_at DATETIME NULL,
    approved BIT NULL,
    reason NVARCHAR(500) NULL,

    CONSTRAINT FK_VerificationRequest_User FOREIGN KEY (user_id) REFERENCES dbo.[User](id)
);
GO

CREATE INDEX IX_Book_seller_id ON dbo.Book(seller_id);
CREATE INDEX IX_Order_customer_id ON dbo.[Order](customer_id);
CREATE INDEX IX_Order_seller_id ON dbo.[Order](seller_id);
CREATE INDEX IX_OrderItem_order_id ON dbo.OrderItem(order_id);
CREATE INDEX IX_RefundRequest_order_id ON dbo.RefundRequest(order_id);
CREATE INDEX IX_VerificationRequest_user_id ON dbo.VerificationRequest(user_id);
CREATE INDEX IX_VerificationRequest_status ON dbo.VerificationRequest(status);
GO

-- Seed users
INSERT INTO dbo.[User] (name, email, password, role, status)
VALUES
(N'admin_demo', N'admin@swd.local', N'DEMO_PASSWORD_NOT_HASHED', N'Admin', N'Active'),
(N'seller_a', N'playunknowquangoc@gmail.com', N'DEMO_PASSWORD_NOT_HASHED', N'Seller', N'Active'),
(N'seller_b', N'quangpnhe180573@gmail.com', N'DEMO_PASSWORD_NOT_HASHED', N'Seller', N'Active'),
(N'seller_c', N'seller3@swd.local', N'DEMO_PASSWORD_NOT_HASHED', N'Seller', N'Active');
GO

-- Bulk seed MANY mock sellers to make pending/update list larger
DECLARE @n INT = 30;
DECLARE @i INT = 1;
WHILE @i <= @n
BEGIN
    DECLARE @name NVARCHAR(255) = CONCAT(N'seller_mock_', RIGHT(CONCAT('00', @i), 2));
    DECLARE @email NVARCHAR(255) = CONCAT(N'seller', @i, N'@swd.local');

    IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE email = @email)
    BEGIN
        INSERT INTO dbo.[User] (name, email, password, role, status)
        VALUES (@name, @email, N'DEMO_PASSWORD_NOT_HASHED', N'Seller', N'Active');
    END

    SET @i += 1;
END
GO

DECLARE @u1 INT = (SELECT id FROM dbo.[User] WHERE email = N'playunknowquangoc@gmail.com');
DECLARE @u2 INT = (SELECT id FROM dbo.[User] WHERE email = N'quangpnhe180573@gmail.com');
DECLARE @u3 INT = (SELECT id FROM dbo.[User] WHERE email = N'seller3@swd.local');

-- Seed some books for lockdown demo
INSERT INTO dbo.Book (seller_id, title, author, price, description, status, stock_quantity)
VALUES
(@u1, N'Demo Book A1', N'Author A', 10.0, N'Demo', N'Pending', 10),
(@u2, N'Demo Book B1', N'Author B', 12.5, N'Demo', N'Pending', 5),
(@u3, N'Demo Book C1', N'Author C', 9.9,  N'Demo', N'Pending', 7);

-- Add 1 mock book per mock seller (helps suspend lockdown demo)
INSERT INTO dbo.Book (seller_id, title, author, price, description, status, stock_quantity)
SELECT u.id,
       CONCAT(N'Mock Book - ', u.name),
       N'Mock Author',
       15.0,
       N'Demo',
       N'Pending',
       10
FROM dbo.[User] u
WHERE u.role = N'Seller' AND u.email LIKE N'seller%@swd.local';

-- Seed pending verification requests (PROCESS)
-- Existing 3 sellers
INSERT INTO dbo.VerificationRequest
(user_id, status, url, citizen_id, bank_account, bank_name, citizen_image, citizen_image_back, bank_card_image, created_at)
VALUES
(@u1, 'PROCESS', N'https://shop.example.com/seller_a', N'111122223333', N'0102030405', N'VCB', N'https://files.example.com/u1-front', N'https://files.example.com/u1-back', N'https://files.example.com/u1-bank', DATEADD(MINUTE, -1, GETDATE())),
(@u2, 'PROCESS', N'https://shop.example.com/seller_b', N'444455556666', N'1111222233', N'TPB', N'https://files.example.com/u2-front', N'https://files.example.com/u2-back', N'https://files.example.com/u2-bank', DATEADD(MINUTE, -2, GETDATE())),
(@u3, 'PROCESS', N'https://shop.example.com/seller_c', N'777788889999', N'2222333344', N'ACB', N'https://files.example.com/u3-front', N'https://files.example.com/u3-back', N'https://files.example.com/u3-bank', DATEADD(MINUTE, -3, GETDATE()));

-- Many mock sellers -> many PROCESS requests
INSERT INTO dbo.VerificationRequest
(user_id, status, url, citizen_id, bank_account, bank_name, citizen_image, citizen_image_back, bank_card_image, created_at)
SELECT u.id,
       'PROCESS',
       CONCAT(N'https://shop.example.com/', u.name),
       CONCAT(N'MOCK-CITIZEN-', RIGHT(CONCAT('000000', u.id), 6)),
       CONCAT(N'MOCK-BANK-', RIGHT(CONCAT('000000', u.id), 6)),
       CASE (u.id % 5)
            WHEN 0 THEN N'VCB'
            WHEN 1 THEN N'TPB'
            WHEN 2 THEN N'ACB'
            WHEN 3 THEN N'BIDV'
            ELSE N'MBB'
       END,
       CONCAT(N'https://files.example.com/', u.name, N'-front'),
       CONCAT(N'https://files.example.com/', u.name, N'-back'),
       CONCAT(N'https://files.example.com/', u.name, N'-bank'),
       DATEADD(MINUTE, -u.id, GETDATE())
FROM dbo.[User] u
WHERE u.role = N'Seller'
  AND u.email LIKE N'seller%@swd.local';
GO

SELECT TOP 50 * FROM dbo.VerificationRequest ORDER BY created_at DESC;
SELECT TOP 50 * FROM dbo.[User] ORDER BY id DESC;
GO
