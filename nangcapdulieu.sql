
USE AutoRentDB_V2;
GO
IF OBJECT_ID(N'dbo.Vehicles',N'U') IS NULL OR OBJECT_ID(N'dbo.Users',N'U') IS NULL OR OBJECT_ID(N'dbo.OrderItems',N'U') IS NULL
    THROW 51005, 'Khong tim thay bang AutoRent V2. Kiem tra database truoc khi nang cap.', 1;
GO
IF COL_LENGTH('dbo.Vehicles','Quantity') IS NULL
    ALTER TABLE dbo.Vehicles ADD Quantity INT NOT NULL CONSTRAINT DF_Vehicles_Quantity DEFAULT (1) WITH VALUES;
GO
IF COL_LENGTH('dbo.Users','Phone') IS NULL
    ALTER TABLE dbo.Users ADD Phone NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.OrderItems','Quantity') IS NULL
    ALTER TABLE dbo.OrderItems ADD Quantity INT NOT NULL CONSTRAINT DF_OrderItems_Quantity DEFAULT (1) WITH VALUES;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Vehicles_Quantity')
    ALTER TABLE dbo.Vehicles WITH CHECK ADD CONSTRAINT CK_Vehicles_Quantity CHECK (Quantity >= 0);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_OrderItems_Quantity')
    ALTER TABLE dbo.OrderItems WITH CHECK ADD CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_OrderItems_VehicleDates' AND object_id=OBJECT_ID('dbo.OrderItems'))
    CREATE INDEX IX_OrderItems_VehicleDates ON dbo.OrderItems(VehicleId,StartDate,EndDate);
GO
UPDATE dbo.Vehicles SET ImageUrl = CASE Name
 WHEN N'Honda Vision' THEN N'~/Content/images/honda-vision.png'
 WHEN N'Honda Air Blade' THEN N'~/Content/images/honda-air-blade.png'
 WHEN N'Yamaha Exciter' THEN N'~/Content/images/yamaha-exciter.png'
 WHEN N'Toyota Vios' THEN N'~/Content/images/toyota-vios.png'
 WHEN N'Kia Seltos' THEN N'~/Content/images/kia-seltos.png'
 WHEN N'Mitsubishi Xpander' THEN N'~/Content/images/mitsubishi-xpander.png'
 ELSE ImageUrl END
WHERE ImageUrl IN (N'~/Content/images/motorbike.svg', N'~/Content/images/car.svg', N'~/Content/images/placeholder.svg');
GO
SELECT 'Vehicles' AS Bang, COUNT(*) AS SoDong FROM dbo.Vehicles
UNION ALL SELECT 'Users',COUNT(*) FROM dbo.Users
UNION ALL SELECT 'Orders',COUNT(*) FROM dbo.Orders
UNION ALL SELECT 'OrderItems',COUNT(*) FROM dbo.OrderItems;
GO
