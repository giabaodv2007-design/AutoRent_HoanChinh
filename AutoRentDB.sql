
IF DB_ID(N'AutoRentDB_V2') IS NULL
    CREATE DATABASE AutoRentDB_V2;
GO
USE AutoRentDB_V2;
GO
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL AND (
    COL_LENGTH(N'dbo.Categories', N'Name') IS NULL OR
    COL_LENGTH(N'dbo.Categories', N'Description') IS NULL
)
    THROW 51000, 'Bang Categories hien co khong dung cau truc. Hay kiem tra database AutoRentDB_V2.', 1;
GO
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(80) NOT NULL,
    Description NVARCHAR(255) NULL
);
GO
IF OBJECT_ID(N'dbo.Vehicles', N'U') IS NULL
CREATE TABLE dbo.Vehicles (
    VehicleId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    Name NVARCHAR(120) NOT NULL,
    Brand NVARCHAR(80) NULL,
    Description NVARCHAR(1000) NULL,
    DailyPrice DECIMAL(18,2) NOT NULL,
    Deposit DECIMAL(18,2) NOT NULL,
    ImageUrl NVARCHAR(300) NULL,
    IsAvailable BIT NOT NULL CONSTRAINT DF_Vehicles_IsAvailable DEFAULT 1,
    Quantity INT NOT NULL CONSTRAINT DF_Vehicles_Quantity DEFAULT 1,
    CONSTRAINT FK_Vehicles_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT CK_Vehicles_Price CHECK (DailyPrice >= 0 AND Deposit >= 0),
    CONSTRAINT CK_Vehicles_Quantity CHECK (Quantity >= 0)
);
GO
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,
    Role NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'User',
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    Phone NVARCHAR(20) NULL,
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'User', N'Admin'))
);
GO
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
CREATE TABLE dbo.Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSDATETIME(),
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'Chờ duyệt',
    TotalRent DECIMAL(18,2) NOT NULL,
    TotalDeposit DECIMAL(18,2) NOT NULL,
    CustomerPhone NVARCHAR(20) NOT NULL,
    Note NVARCHAR(500) NULL,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Chờ duyệt', N'Đang giao', N'Đã giao'))
);
GO
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
CREATE TABLE dbo.OrderItems (
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    Quantity INT NOT NULL CONSTRAINT DF_OrderItems_Quantity DEFAULT 1,
    OrderId INT NOT NULL,
    VehicleId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Days INT NOT NULL,
    PricePerDay DECIMAL(18,2) NOT NULL,
    Deposit DECIMAL(18,2) NOT NULL,
    RentAmount DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId),
    CONSTRAINT FK_OrderItems_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles(VehicleId),
    CONSTRAINT CK_OrderItems_Dates CHECK (EndDate >= StartDate AND Days >= 1),
    CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0)
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    INSERT INTO dbo.Categories (Name, Description) VALUES
      (N'Xe máy', N'Xe tay ga, xe số, xe côn tay'),
      (N'Ô tô', N'Ô tô tự lái 4-7 chỗ');
END;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles)
BEGIN
    DECLARE @Bike INT = (SELECT TOP 1 CategoryId FROM dbo.Categories WHERE Name=N'Xe máy');
    DECLARE @Car INT = (SELECT TOP 1 CategoryId FROM dbo.Categories WHERE Name=N'Ô tô');
    INSERT INTO dbo.Vehicles (CategoryId, Name, Brand, Description, DailyPrice, Deposit, ImageUrl, IsAvailable) VALUES
       (@Bike, N'Honda Vision', N'Honda', N'Xe tay ga nhỏ gọn, tiết kiệm xăng.', 150000, 1000000, N'~/Content/images/honda-vision.png', 1),
       (@Bike, N'Honda Air Blade', N'Honda', N'Xe tay ga phù hợp đi trong thành phố.', 190000, 1200000, N'~/Content/images/honda-air-blade.png', 1),
       (@Bike, N'Yamaha Exciter', N'Yamaha', N'Xe côn tay thể thao.', 230000, 1500000, N'~/Content/images/yamaha-exciter.png', 1),
       (@Car, N'Toyota Vios', N'Toyota', N'Sedan 5 chỗ, dễ lái và tiết kiệm.', 750000, 5000000, N'~/Content/images/toyota-vios.png', 1),
       (@Car, N'Kia Seltos', N'Kia', N'SUV đô thị rộng rãi.', 1000000, 7000000, N'~/Content/images/kia-seltos.png', 1),
       (@Car, N'Mitsubishi Xpander', N'Mitsubishi', N'Xe 7 chỗ phù hợp nhóm bạn và gia đình.', 1200000, 8000000, N'~/Content/images/mitsubishi-xpander.png', 1);
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_OrderItems_VehicleDates' AND object_id=OBJECT_ID('dbo.OrderItems'))
    CREATE INDEX IX_OrderItems_VehicleDates ON dbo.OrderItems(VehicleId,StartDate,EndDate);
GO
