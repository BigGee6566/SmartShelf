/* ============================================================================
   SmartShelf - Supermarket Management System
   Schema + seed script.  This file is the source of truth for the database;
   the EF6 Code-First classes in Models\ are written to match it and no
   initializer or migration ever runs (see Global.Application_Start).

   HOW TO RUN
     1. Open in SQL Server Management Studio or the VS SQL Server Object Explorer
        connected to (localdb)\MSSQLLocalDB (or your hosting provider's server).
     2. Execute the whole file.  It creates the SmartShelf database, all 13
        tables, and 46 days of trading data so every report has
        something real to aggregate.
     3. Re-running it is safe: it drops and recreates everything.

   Demo passwords are listed in README.md.  Only PBKDF2 hashes are stored here.
   ============================================================================ */

IF DB_ID('SmartShelf') IS NULL
    CREATE DATABASE SmartShelf;
GO

USE SmartShelf;
GO

-- Required for the filtered index on StockBatch.ExpiryDate, and the settings EF6
-- itself connects with - keeping them explicit avoids surprises under sqlcmd.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------------
   1. Drop existing objects (child tables first so FKs never block the drop)
   --------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.CashUp', 'U')            IS NOT NULL DROP TABLE dbo.CashUp;
IF OBJECT_ID('dbo.SaleLine', 'U')          IS NOT NULL DROP TABLE dbo.SaleLine;
IF OBJECT_ID('dbo.Sale', 'U')              IS NOT NULL DROP TABLE dbo.Sale;
IF OBJECT_ID('dbo.Shift', 'U')             IS NOT NULL DROP TABLE dbo.Shift;
IF OBJECT_ID('dbo.StockMovement', 'U')     IS NOT NULL DROP TABLE dbo.StockMovement;
IF OBJECT_ID('dbo.StockBatch', 'U')        IS NOT NULL DROP TABLE dbo.StockBatch;
IF OBJECT_ID('dbo.PurchaseOrderLine', 'U') IS NOT NULL DROP TABLE dbo.PurchaseOrderLine;
IF OBJECT_ID('dbo.PurchaseOrder', 'U')     IS NOT NULL DROP TABLE dbo.PurchaseOrder;
IF OBJECT_ID('dbo.Product', 'U')           IS NOT NULL DROP TABLE dbo.Product;
IF OBJECT_ID('dbo.Category', 'U')          IS NOT NULL DROP TABLE dbo.Category;
IF OBJECT_ID('dbo.[User]', 'U')            IS NOT NULL DROP TABLE dbo.[User];
IF OBJECT_ID('dbo.Supplier', 'U')          IS NOT NULL DROP TABLE dbo.Supplier;
IF OBJECT_ID('dbo.[Role]', 'U')            IS NOT NULL DROP TABLE dbo.[Role];
GO

/* ---------------------------------------------------------------------------
   2. Identity and permissions
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.[Role] (
    RoleId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Role PRIMARY KEY,
    Name         NVARCHAR(30)  NOT NULL,
    Description  NVARCHAR(200) NULL
);
CREATE UNIQUE INDEX IX_Role_Name ON dbo.[Role](Name);
GO

CREATE TABLE dbo.Supplier (
    SupplierId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Supplier PRIMARY KEY,
    Name          NVARCHAR(100) NOT NULL,
    ContactPerson NVARCHAR(100) NULL,
    Email         NVARCHAR(150) NULL,
    Phone         NVARCHAR(30)  NULL,
    LeadTimeDays  INT           NOT NULL CONSTRAINT DF_Supplier_LeadTime DEFAULT (3),
    IsActive      BIT           NOT NULL CONSTRAINT DF_Supplier_IsActive DEFAULT (1)
);
GO

CREATE TABLE dbo.[User] (
    UserId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_User PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL,
    PasswordHash NVARCHAR(200) NOT NULL,   -- PBKDF2, never a plain password
    PasswordSalt NVARCHAR(100) NOT NULL,
    FullName     NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(150) NULL,
    RoleId       INT           NOT NULL,
    SupplierId   INT           NULL,       -- set only for Supplier logins
    IsActive     BIT           NOT NULL CONSTRAINT DF_User_IsActive DEFAULT (1),
    CreatedAt    DATETIME      NOT NULL CONSTRAINT DF_User_CreatedAt DEFAULT (GETDATE()),
    LastLoginAt  DATETIME      NULL,
    CONSTRAINT FK_User_Role     FOREIGN KEY (RoleId)     REFERENCES dbo.[Role](RoleId),
    CONSTRAINT FK_User_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Supplier(SupplierId)
);
CREATE UNIQUE INDEX IX_User_Username ON dbo.[User](Username);
GO

/* ---------------------------------------------------------------------------
   3. Focus A - catalogue and pricing
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.Category (
    CategoryId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Category PRIMARY KEY,
    Name        NVARCHAR(60)  NOT NULL,
    Description NVARCHAR(200) NULL,
    IsActive    BIT           NOT NULL CONSTRAINT DF_Category_IsActive DEFAULT (1)
);
CREATE UNIQUE INDEX IX_Category_Name ON dbo.Category(Name);
GO

CREATE TABLE dbo.Product (
    ProductId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Product PRIMARY KEY,
    Barcode          NVARCHAR(30)   NOT NULL,
    Name             NVARCHAR(120)  NOT NULL,
    Description      NVARCHAR(300)  NULL,
    CategoryId       INT            NOT NULL,
    SupplierId       INT            NULL,
    CostPrice        DECIMAL(18,2)  NOT NULL,
    SellingPrice     DECIMAL(18,2)  NOT NULL,
    VatRate          DECIMAL(5,4)   NOT NULL CONSTRAINT DF_Product_Vat DEFAULT (0.1500),
    ReorderLevel     INT            NOT NULL CONSTRAINT DF_Product_Reorder DEFAULT (10),
    QuantityOnHand   INT            NOT NULL CONSTRAINT DF_Product_Qty DEFAULT (0),
    ImageUrl         NVARCHAR(400)  NULL,
    ImageData        VARBINARY(MAX) NULL,   -- uploaded image wins over ImageUrl
    ImageContentType NVARCHAR(60)   NULL,
    IsActive         BIT            NOT NULL CONSTRAINT DF_Product_IsActive DEFAULT (1),
    CreatedAt        DATETIME       NOT NULL CONSTRAINT DF_Product_CreatedAt DEFAULT (GETDATE()),
    CONSTRAINT FK_Product_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category(CategoryId),
    CONSTRAINT FK_Product_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Supplier(SupplierId),
    -- Stock can never be negative, whatever path tries to write it.
    CONSTRAINT CK_Product_QtyNonNegative CHECK (QuantityOnHand >= 0),
    CONSTRAINT CK_Product_PricePositive  CHECK (SellingPrice > 0 AND CostPrice >= 0)
);
CREATE UNIQUE INDEX IX_Product_Barcode ON dbo.Product(Barcode);
CREATE INDEX IX_Product_Category ON dbo.Product(CategoryId);
CREATE INDEX IX_Product_Supplier ON dbo.Product(SupplierId);
GO

/* ---------------------------------------------------------------------------
   4. Focus C - purchase orders (declared before StockBatch, which references them)
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.PurchaseOrder (
    PurchaseOrderId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOrder PRIMARY KEY,
    OrderNumber      NVARCHAR(20)  NOT NULL,
    SupplierId       INT           NOT NULL,
    CreatedByUserId  INT           NOT NULL,
    Status           INT           NOT NULL,   -- PurchaseOrderStatus enum
    CreatedAt        DATETIME      NOT NULL,
    SentAt           DATETIME      NULL,
    ExpectedDate     DATETIME      NOT NULL,
    ConfirmedAt      DATETIME      NULL,
    DeliveredAt      DATETIME      NULL,
    Notes            NVARCHAR(300) NULL,
    SupplierNote     NVARCHAR(300) NULL,
    CONSTRAINT FK_PO_Supplier FOREIGN KEY (SupplierId)      REFERENCES dbo.Supplier(SupplierId),
    CONSTRAINT FK_PO_User     FOREIGN KEY (CreatedByUserId) REFERENCES dbo.[User](UserId)
);
CREATE UNIQUE INDEX IX_PurchaseOrder_OrderNumber ON dbo.PurchaseOrder(OrderNumber);
CREATE INDEX IX_PurchaseOrder_Supplier ON dbo.PurchaseOrder(SupplierId, Status);
GO

CREATE TABLE dbo.PurchaseOrderLine (
    PurchaseOrderLineId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOrderLine PRIMARY KEY,
    PurchaseOrderId     INT           NOT NULL,
    ProductId           INT           NOT NULL,
    QuantityOrdered     INT           NOT NULL,
    QuantityReceived    INT           NOT NULL CONSTRAINT DF_POL_Received DEFAULT (0),
    UnitCost            DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_POL_PO      FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrder(PurchaseOrderId) ON DELETE CASCADE,
    CONSTRAINT FK_POL_Product FOREIGN KEY (ProductId)       REFERENCES dbo.Product(ProductId),
    CONSTRAINT CK_POL_Qty CHECK (QuantityOrdered > 0 AND QuantityReceived >= 0)
);
CREATE INDEX IX_POL_PO ON dbo.PurchaseOrderLine(PurchaseOrderId);
GO

/* ---------------------------------------------------------------------------
   5. Focus B - stock control
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.StockBatch (
    StockBatchId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockBatch PRIMARY KEY,
    ProductId        INT           NOT NULL,
    BatchCode        NVARCHAR(40)  NULL,
    QuantityReceived INT           NOT NULL,
    QuantityRemaining INT          NOT NULL,
    ReceivedDate     DATETIME      NOT NULL,
    ExpiryDate       DATETIME      NULL,
    UnitCost         DECIMAL(18,2) NOT NULL CONSTRAINT DF_Batch_Cost DEFAULT (0),
    PurchaseOrderId  INT           NULL,
    CONSTRAINT FK_Batch_Product FOREIGN KEY (ProductId)       REFERENCES dbo.Product(ProductId),
    CONSTRAINT FK_Batch_PO      FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrder(PurchaseOrderId),
    CONSTRAINT CK_Batch_Qty CHECK (QuantityRemaining >= 0 AND QuantityRemaining <= QuantityReceived)
);
CREATE INDEX IX_Batch_Product ON dbo.StockBatch(ProductId);
CREATE INDEX IX_Batch_Expiry  ON dbo.StockBatch(ExpiryDate) WHERE ExpiryDate IS NOT NULL;
GO

CREATE TABLE dbo.StockMovement (
    StockMovementId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockMovement PRIMARY KEY,
    ProductId       INT           NOT NULL,
    MovementType    INT           NOT NULL,   -- MovementType enum
    QuantityDelta   INT           NOT NULL,   -- negative for sales and waste
    QuantityAfter   INT           NOT NULL,
    Reason          NVARCHAR(250) NULL,
    UserId          INT           NOT NULL,   -- who is accountable for this change
    SaleId          INT           NULL,
    PurchaseOrderId INT           NULL,
    UnitCostAtTime  DECIMAL(18,2) NOT NULL CONSTRAINT DF_Move_Cost DEFAULT (0),
    CreatedAt       DATETIME      NOT NULL,
    CONSTRAINT FK_Move_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId),
    CONSTRAINT FK_Move_User    FOREIGN KEY (UserId)    REFERENCES dbo.[User](UserId)
);
CREATE INDEX IX_Move_Product ON dbo.StockMovement(ProductId, CreatedAt);
CREATE INDEX IX_Move_Created ON dbo.StockMovement(CreatedAt);
CREATE INDEX IX_Move_Type    ON dbo.StockMovement(MovementType, CreatedAt);
GO

/* ---------------------------------------------------------------------------
   6. Focus D and E - sales, shifts, cash-up
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.Shift (
    ShiftId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Shift PRIMARY KEY,
    CashierUserId INT           NOT NULL,
    TillNumber    NVARCHAR(20)  NOT NULL,
    OpenedAt      DATETIME      NOT NULL,
    ClosedAt      DATETIME      NULL,
    OpeningFloat  DECIMAL(18,2) NOT NULL CONSTRAINT DF_Shift_Float DEFAULT (0),
    Status        INT           NOT NULL,   -- ShiftStatus enum
    CONSTRAINT FK_Shift_User FOREIGN KEY (CashierUserId) REFERENCES dbo.[User](UserId)
);
CREATE INDEX IX_Shift_User ON dbo.Shift(CashierUserId, Status);
GO

CREATE TABLE dbo.Sale (
    SaleId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sale PRIMARY KEY,
    ReceiptNumber  NVARCHAR(20)  NOT NULL,
    CashierUserId  INT           NOT NULL,
    ShiftId        INT           NOT NULL,
    SaleDate       DATETIME      NOT NULL,
    Subtotal       DECIMAL(18,2) NOT NULL,
    VatAmount      DECIMAL(18,2) NOT NULL,
    Total          DECIMAL(18,2) NOT NULL,
    PaymentMethod  INT           NOT NULL,   -- PaymentMethod enum
    AmountTendered DECIMAL(18,2) NOT NULL,
    ChangeDue      DECIMAL(18,2) NOT NULL,   -- always server-calculated
    Status         INT           NOT NULL,   -- SaleStatus enum
    CONSTRAINT FK_Sale_User  FOREIGN KEY (CashierUserId) REFERENCES dbo.[User](UserId),
    CONSTRAINT FK_Sale_Shift FOREIGN KEY (ShiftId)       REFERENCES dbo.Shift(ShiftId)
);
CREATE UNIQUE INDEX IX_Sale_ReceiptNumber ON dbo.Sale(ReceiptNumber);
CREATE INDEX IX_Sale_Date  ON dbo.Sale(SaleDate);
CREATE INDEX IX_Sale_Shift ON dbo.Sale(ShiftId, Status);
GO

CREATE TABLE dbo.SaleLine (
    SaleLineId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SaleLine PRIMARY KEY,
    SaleId      INT           NOT NULL,
    ProductId   INT           NOT NULL,
    ProductName NVARCHAR(120) NOT NULL,   -- snapshot: renaming a product must not rewrite history
    UnitPrice   DECIMAL(18,2) NOT NULL,
    Quantity    INT           NOT NULL,
    VatRate     DECIMAL(5,4)  NOT NULL,
    LineTotal   DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_SaleLine_Sale    FOREIGN KEY (SaleId)    REFERENCES dbo.Sale(SaleId) ON DELETE CASCADE,
    CONSTRAINT FK_SaleLine_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId),
    CONSTRAINT CK_SaleLine_Qty CHECK (Quantity > 0)
);
CREATE INDEX IX_SaleLine_Sale    ON dbo.SaleLine(SaleId);
CREATE INDEX IX_SaleLine_Product ON dbo.SaleLine(ProductId);
GO

CREATE TABLE dbo.CashUp (
    CashUpId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashUp PRIMARY KEY,
    ShiftId          INT           NOT NULL,
    UserId           INT           NOT NULL,   -- cashier who counted the drawer
    ExpectedCash     DECIMAL(18,2) NOT NULL,   -- computed server-side, never posted
    CountedCash      DECIMAL(18,2) NOT NULL,
    Variance         DECIMAL(18,2) NOT NULL,
    Notes            NVARCHAR(300) NULL,
    CreatedAt        DATETIME      NOT NULL,
    ReviewedByUserId INT           NULL,
    ReviewedAt       DATETIME      NULL,
    ReviewNote       NVARCHAR(300) NULL,
    CONSTRAINT FK_CashUp_Shift    FOREIGN KEY (ShiftId)          REFERENCES dbo.Shift(ShiftId),
    CONSTRAINT FK_CashUp_User     FOREIGN KEY (UserId)           REFERENCES dbo.[User](UserId),
    CONSTRAINT FK_CashUp_Reviewer FOREIGN KEY (ReviewedByUserId) REFERENCES dbo.[User](UserId)
);
CREATE UNIQUE INDEX IX_CashUp_Shift ON dbo.CashUp(ShiftId);
GO

PRINT 'Schema created.';
GO

/* ============================================================================
   SEED DATA
   Reference data first, then ~45 days of trading generated by the loops at the
   bottom.  Everything the four reports show is aggregated from these ordinary
   tables at run time - no report output is stored anywhere.
   ============================================================================ */

/* ---------- Roles ---------- */
INSERT INTO dbo.[Role] (Name, Description) VALUES
 ('Cashier',  'Operates the POS terminal, takes payment and cashes up their own shift.'),
 ('Manager',  'Store manager: catalogue, stock, purchase orders, reconciliation and reports.'),
 ('Supplier', 'External supplier: views, confirms and delivers against purchase orders.'),
 ('Admin',    'System administrator: user accounts and role permissions.');
GO

/* ---------- Suppliers ---------- */
INSERT INTO dbo.Supplier (Name, ContactPerson, Email, Phone, LeadTimeDays, IsActive) VALUES
 ('Amathole Fresh Produce',        'Sipho Mkhize',   'orders@amathole-fresh.co.za',  '043 555 0142', 2, 1),
 ('Border Dairy Co-op',            'Lerato Nkosi',   'sales@borderdairy.co.za',      '043 555 0198', 1, 1),
 ('Eastern Cape Bakeries',         'Johan Slabbert', 'dispatch@ecbakeries.co.za',    '041 555 0077', 1, 1),
 ('Sunrise Wholesale Distributors','Farhana Pillay', 'orders@sunrisewholesale.co.za','031 555 0311', 4, 1),
 ('Kei Valley Meats',              'Bongani Mtolo',  'orders@keivalleymeats.co.za',  '047 555 0265', 3, 1);
GO

/* ---------- Users ----------
   PasswordHash is PBKDF2-HMACSHA1, 100 000 iterations, 32-byte key, matching
   Services\PasswordHasher.cs exactly.  Plain passwords appear only in README.md. */
INSERT INTO dbo.[User] (Username, PasswordHash, PasswordSalt, FullName, Email, RoleId, SupplierId, IsActive, CreatedAt) VALUES
 ('a.booi',    'qvnFkKTlfCISWEpuD7I7PusUgHQdWUUCAc9QeeGhYIQ=', 'F57Mghgz2huFNKSxYNfUyw==', 'Ayanda Booi',           'a.booi@forte.co.za',    1, NULL, 1, DATEADD(DAY,-120,GETDATE())),
 ('n.dlamini', '3VmHC2AUHEmItstOhFrrpBXmKOcpxMHJsfyRueAgmnk=', '3Atc4o9oKTPEK3/GM3zH2Q==', 'Nomsa Dlamini',         'n.dlamini@forte.co.za', 1, NULL, 1, DATEADD(DAY,-115,GETDATE())),
 ('a.right',   'XXvrU1L624walfCTr2D6GYrTqZt7lpTWOmsp6+zTT9o=', 'PhJpBAfFXhjG1+l96OqNZQ==', 'Ayabonga Right',        'a.right@forte.co.za',   2, NULL, 1, DATEADD(DAY,-200,GETDATE())),
 ('t.jacobs',  'vTNLeHE5EoXLTZBd1eB/5TrnC/MZNjWQubjCHT7ajxU=', 'aHGz7305pmgMdZAyT4TwQg==', 'Thandi Jacobs',         't.jacobs@forte.co.za',  2, NULL, 1, DATEADD(DAY,-160,GETDATE())),
 ('s.mkhize',  'Qr1VeiA/CgDFr89Y9Lyf2iEASiAmcyXiyxzzA79RNy4=', '3t9aVjYCHUKigmUzzo5f1g==', 'Sipho Mkhize',          'sipho@amathole-fresh.co.za',   3, 1, 1, DATEADD(DAY,-90,GETDATE())),
 ('f.pillay',  'ZHRsAj1rPDqw9ozTZzYKmcmbeiPyTpuxg5ZdofHQESw=', '3HehQNS6sO7EHPrf5SLL0A==', 'Farhana Pillay',        'farhana@sunrisewholesale.co.za', 3, 4, 1, DATEADD(DAY,-88,GETDATE())),
 ('admin',     '9zuMUaDz2hH1gex/bAObQ30resJEXiW8DufK6Jtut1g=', 'UiYnqzj1mkuM95mMdZIdFw==', 'System Administrator',  'admin@forte.co.za',     4, NULL, 1, DATEADD(DAY,-200,GETDATE()));
GO

/* ---------- Categories ---------- */
INSERT INTO dbo.Category (Name, Description, IsActive) VALUES
 ('Bakery',    'Bread, rolls and in-store baked goods', 1),
 ('Dairy',     'Milk, cheese, butter and eggs',         1),
 ('Produce',   'Fresh fruit and vegetables',            1),
 ('Butchery',  'Fresh and packaged meat',               1),
 ('Pantry',    'Dry goods, tinned food and staples',    1),
 ('Beverages', 'Hot and cold drinks',                   1),
 ('Household', 'Cleaning and household consumables',    1),
 ('Frozen',    'Frozen foods',                          1);
GO

/* ---------- Products ----------
   VatRate 0.0000 on brown bread and maize meal: both are zero-rated basic
   foodstuffs in South Africa, which is exactly why the rate lives per product. */
INSERT INTO dbo.Product (Barcode, Name, Description, CategoryId, SupplierId, CostPrice, SellingPrice, VatRate, ReorderLevel, QuantityOnHand, ImageUrl, IsActive, CreatedAt) VALUES
 ('6001234500011','Full Cream Milk 2L',       'Long-life full cream milk, 2 litre',        2, 2, 24.00,  32.50, 0.1500, 15, 0, 'https://images.pexels.com/photos/3735169/pexels-photo-3735169.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-180,GETDATE())),
 ('6001234500028','White Bread 700g',         'Fresh white loaf, sliced',                  1, 3, 12.50,  18.99, 0.1500, 10, 0, 'https://images.pexels.com/photos/20008782/pexels-photo-20008782.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-180,GETDATE())),
 ('6001234500035','Fresh Tomatoes 500g',      'Vine-ripened tomatoes, punnet',             3, 1, 13.00,  19.99, 0.1500, 12, 0, 'https://images.pexels.com/photos/6073163/pexels-photo-6073163.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-180,GETDATE())),
 ('6001234500042','Farm Vegetables Mix',      'Mixed seasonal vegetables, 1kg',            3, 1, 16.00,  24.50, 0.1500, 15, 0, 'https://images.pexels.com/photos/37321079/pexels-photo-37321079.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-180,GETDATE())),
 ('6001234500059','Mixed Citrus 1kg',         'Oranges, naartjies and lemons',             3, 1, 18.50,  27.90, 0.1500, 10, 0, 'https://images.pexels.com/photos/4053672/pexels-photo-4053672.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-180,GETDATE())),
 ('6001234500066','Wicker Basket Fruit Mix',  'Gift basket of assorted fruit',             3, 1, 30.00,  45.00, 0.1500,  6, 0, 'https://images.pexels.com/photos/15626438/pexels-photo-15626438.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-150,GETDATE())),
 ('6001234500073','Bakery Assorted Pastry',   'Croissants and Danish pastries, 4 pack',    1, 3, 13.50,  22.00, 0.1500, 10, 0, 'https://images.pexels.com/photos/3341067/pexels-photo-3341067.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-150,GETDATE())),
 ('6001234500080','Canned Goods Bundle',      'Assorted tinned vegetables, 4 pack',        5, 4, 27.00,  38.75, 0.1500, 12, 0, 'https://images.pexels.com/photos/16211537/pexels-photo-16211537.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-150,GETDATE())),
 ('6001234500097','Low Fat Milk 1L',          'Fresh low fat milk, 1 litre',               2, 2, 14.00,  19.50, 0.1500, 20, 0, 'https://images.pexels.com/photos/3735169/pexels-photo-3735169.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-170,GETDATE())),
 ('6001234500103','Brown Bread 700g',         'Zero-rated brown loaf, sliced',             1, 3, 11.00,  17.50, 0.0000, 12, 0, 'https://images.pexels.com/photos/20008782/pexels-photo-20008782.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-170,GETDATE())),
 ('6001234500110','Free Range Eggs 18s',      'Free range large eggs, tray of 18',         2, 2, 47.00,  62.00, 0.1500, 10, 0, 'https://images.pexels.com/photos/4971916/pexels-photo-4971916.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-170,GETDATE())),
 ('6001234500127','Cheddar Cheese 400g',      'Mature cheddar block',                      2, 2, 58.00,  78.90, 0.1500,  8, 0, NULL, 1, DATEADD(DAY,-165,GETDATE())),
 ('6001234500134','Salted Butter 500g',       'Creamery salted butter',                    2, 2, 68.00,  89.99, 0.1500,  6, 0, NULL, 1, DATEADD(DAY,-165,GETDATE())),
 ('6001234500141','Baby Spinach 200g',        'Washed baby spinach leaves',                3, 1, 13.50,  21.00, 0.1500, 10, 0, 'https://images.pexels.com/photos/4971916/pexels-photo-4971916.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-160,GETDATE())),
 ('6001234500158','Green Beans 400g',         'Trimmed fine green beans',                  3, 1, 15.00,  23.50, 0.1500, 10, 0, NULL, 1, DATEADD(DAY,-160,GETDATE())),
 ('6001234500165','Baby Potatoes 1.5kg',      'Washed baby potatoes',                      3, 1, 22.00,  34.90, 0.1500, 12, 0, NULL, 1, DATEADD(DAY,-160,GETDATE())),
 ('6001234500172','Bananas 1kg',              'Ripe Cavendish bananas',                    3, 1, 15.50,  24.99, 0.1500, 15, 0, 'https://images.pexels.com/photos/9070110/pexels-photo-9070110.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-155,GETDATE())),
 ('6001234500189','Long Grain Rice 2kg',      'Parboiled long grain white rice',           5, 4, 40.00,  54.90, 0.1500, 10, 0, NULL, 1, DATEADD(DAY,-155,GETDATE())),
 ('6001234500196','Maize Meal 2.5kg',         'Zero-rated super maize meal',               5, 4, 31.00,  42.50, 0.0000, 12, 0, NULL, 1, DATEADD(DAY,-155,GETDATE())),
 ('6001234500202','Sunflower Oil 2L',         'Pure sunflower cooking oil',                5, 4, 61.00,  79.99, 0.1500,  8, 0, NULL, 1, DATEADD(DAY,-150,GETDATE())),
 ('6001234500219','White Sugar 2.5kg',        'Granulated white sugar',                    5, 4, 45.00,  59.90, 0.1500, 10, 0, NULL, 1, DATEADD(DAY,-150,GETDATE())),
 ('6001234500226','Baked Beans 410g',         'Baked beans in tomato sauce',               5, 4, 11.00,  16.99, 0.1500, 18, 0, 'https://images.pexels.com/photos/16211537/pexels-photo-16211537.jpeg?auto=compress&cs=tinysrgb&w=400', 1, DATEADD(DAY,-150,GETDATE())),
 ('6001234500233','Instant Coffee 200g',      'Freeze-dried instant coffee',               6, 4, 72.00,  94.90, 0.1500,  8, 0, NULL, 1, DATEADD(DAY,-145,GETDATE())),
 ('6001234500240','Rooibos Tea 80s',          'Pure rooibos teabags, 80 pack',             6, 4, 35.00,  48.50, 0.1500, 10, 0, NULL, 1, DATEADD(DAY,-145,GETDATE())),
 ('6001234500257','Orange Juice 1.5L',        '100% orange juice, chilled',                6, 4, 23.00,  32.90, 0.1500, 12, 0, NULL, 1, DATEADD(DAY,-145,GETDATE())),
 ('6001234500264','Still Water 5L',           'Purified still water, 5 litre',             6, 4, 18.00,  27.50, 0.1500, 15, 0, NULL, 1, DATEADD(DAY,-140,GETDATE())),
 ('6001234500271','Dishwashing Liquid 750ml', 'Lemon dishwashing liquid',                  7, 4, 24.50,  34.90, 0.1500, 10, 0, NULL, 1, DATEADD(DAY,-140,GETDATE())),
 ('6001234500288','Laundry Powder 2kg',       'Auto washing powder',                       7, 4, 68.00,  89.90, 0.1500,  8, 0, NULL, 1, DATEADD(DAY,-140,GETDATE())),
 ('6001234500295','Toilet Rolls 9s',          '2-ply toilet tissue, 9 pack',               7, 4, 59.00,  79.90, 0.1500, 10, 0, NULL, 1, DATEADD(DAY,-135,GETDATE())),
 ('6001234500301','Chicken Braai Pack 1kg',   'Mixed chicken portions for the braai',      4, 5, 68.00,  89.99, 0.1500,  8, 0, NULL, 1, DATEADD(DAY,-135,GETDATE())),
 ('6001234500318','Beef Mince 500g',          'Lean beef mince',                           4, 5, 56.00,  74.90, 0.1500,  8, 0, NULL, 1, DATEADD(DAY,-135,GETDATE())),
 ('6001234500325','Boerewors 1kg',            'Traditional farm-style boerewors',          4, 5, 85.00, 109.90, 0.1500,  6, 0, NULL, 1, DATEADD(DAY,-130,GETDATE()));
GO

PRINT 'Reference data seeded.';
GO

/* ============================================================================
   TRADING HISTORY
   Generated rather than hand-written so the four reports have a realistic
   spread to aggregate.  Everything below writes only to the ordinary tables -
   the reports themselves compute from these rows at run time.
   ============================================================================ */

DECLARE @cashier1 INT = (SELECT UserId FROM dbo.[User] WHERE Username = 'a.booi');
DECLARE @cashier2 INT = (SELECT UserId FROM dbo.[User] WHERE Username = 'n.dlamini');
DECLARE @manager  INT = (SELECT UserId FROM dbo.[User] WHERE Username = 'a.right');

/* ---------- Opening stock: one receipt per product, 60 days ago ---------- */
UPDATE dbo.Product SET QuantityOnHand = ReorderLevel * 20;

INSERT INTO dbo.StockMovement (ProductId, MovementType, QuantityDelta, QuantityAfter, Reason, UserId, UnitCostAtTime, CreatedAt)
SELECT ProductId, 3 /*Receipt*/, QuantityOnHand, QuantityOnHand,
       'Opening stock take', @manager, CostPrice, DATEADD(DAY, -60, GETDATE())
FROM dbo.Product;

/* ---------- 46 days of shifts and sales ---------- */
DECLARE @dayOffset INT = -45;
DECLARE @receiptSeq INT = 4000;

WHILE @dayOffset <= 0
BEGIN
    DECLARE @dayDate DATETIME = DATEADD(DAY, @dayOffset, CAST(CAST(GETDATE() AS DATE) AS DATETIME));

    -- Morning delivery: anything that has fallen near its reorder level is topped up,
    -- which is what keeps a real store trading and gives the receipt history its shape.
    INSERT INTO dbo.StockMovement (ProductId, MovementType, QuantityDelta, QuantityAfter, Reason,
                                   UserId, UnitCostAtTime, CreatedAt)
    SELECT ProductId, 3 /*Receipt*/,
           (ReorderLevel * 16) - QuantityOnHand, ReorderLevel * 16,
           'Routine supplier delivery received', @manager, CostPrice,
           DATEADD(HOUR, 7, @dayDate)
    FROM dbo.Product
    WHERE IsActive = 1 AND QuantityOnHand < ReorderLevel * 4;

    UPDATE dbo.Product SET QuantityOnHand = ReorderLevel * 16
    WHERE IsActive = 1 AND QuantityOnHand < ReorderLevel * 4;

    DECLARE @cashierIdx INT = 1;

    WHILE @cashierIdx <= 2
    BEGIN
        DECLARE @cashierId INT = CASE WHEN @cashierIdx = 1 THEN @cashier1 ELSE @cashier2 END;
        DECLARE @openedAt DATETIME = DATEADD(HOUR, CASE WHEN @cashierIdx = 1 THEN 8 ELSE 14 END, @dayDate);
        DECLARE @closedAt DATETIME = DATEADD(HOUR, 6, @openedAt);

        INSERT INTO dbo.Shift (CashierUserId, TillNumber, OpenedAt, ClosedAt, OpeningFloat, Status)
        VALUES (@cashierId, CASE WHEN @cashierIdx = 1 THEN 'Till 1' ELSE 'Till 2' END,
                @openedAt, @closedAt, 500.00, 2 /*Closed*/);

        DECLARE @shiftId INT = CAST(SCOPE_IDENTITY() AS INT);

        -- Saturdays are busier than the rest of the week, which is the whole reason
        -- Forte Supermarket needed this system in the first place.
        DECLARE @saleCount INT = CASE WHEN DATEPART(WEEKDAY, @dayDate) = 7 THEN 16 ELSE 9 END
                                 + ABS(CHECKSUM(NEWID()) % 1000000) % 6;
        DECLARE @saleIdx INT = 1;

        WHILE @saleIdx <= @saleCount
        BEGIN
            SET @receiptSeq = @receiptSeq + 1;

            DECLARE @saleDate DATETIME = DATEADD(MINUTE, (@saleIdx * 20) + (ABS(CHECKSUM(NEWID()) % 1000000) % 15), @openedAt);
            -- Cash still dominates at a neighbourhood supermarket; card next, mobile rare.
            DECLARE @payRoll INT = ABS(CHECKSUM(NEWID()) % 1000000) % 100;
            DECLARE @payMethod INT = CASE WHEN @payRoll < 58 THEN 1 WHEN @payRoll < 90 THEN 2 ELSE 3 END;

            INSERT INTO dbo.Sale (ReceiptNumber, CashierUserId, ShiftId, SaleDate, Subtotal, VatAmount, Total,
                                  PaymentMethod, AmountTendered, ChangeDue, Status)
            VALUES ('RCT-' + RIGHT('0000000' + CAST(@receiptSeq AS VARCHAR(10)), 7),
                    @cashierId, @shiftId, @saleDate, 0, 0, 0, @payMethod, 0, 0, 1 /*Completed*/);

            DECLARE @saleId INT = CAST(SCOPE_IDENTITY() AS INT);
            DECLARE @lineCount INT = 1 + ABS(CHECKSUM(NEWID()) % 1000000) % 4;

            -- The eight products carried over from the design mock-up are the store's
            -- staples, so they are weighted to come up far more often than the long tail.
            INSERT INTO dbo.SaleLine (SaleId, ProductId, ProductName, UnitPrice, Quantity, VatRate, LineTotal)
            SELECT TOP (@lineCount)
                   @saleId, p.ProductId, p.Name, p.SellingPrice, q.Qty, p.VatRate,
                   CAST(p.SellingPrice * q.Qty AS DECIMAL(18,2))
            FROM dbo.Product p
            CROSS APPLY (SELECT 1 + ABS(CHECKSUM(NEWID()) % 1000000) % 3 AS Qty) q
            WHERE p.IsActive = 1 AND p.QuantityOnHand >= 3
            ORDER BY (ABS(CHECKSUM(NEWID()) % 1000000) % 100) + CASE WHEN p.ProductId <= 8 THEN 0 ELSE 15 END;

            -- A sale that somehow picked up no lines is discarded rather than saved empty.
            IF NOT EXISTS (SELECT 1 FROM dbo.SaleLine WHERE SaleId = @saleId)
            BEGIN
                DELETE FROM dbo.Sale WHERE SaleId = @saleId;
                SET @saleIdx = @saleIdx + 1;
                CONTINUE;
            END

            UPDATE s
            SET Subtotal  = t.Sub,
                VatAmount = t.Vat,
                Total     = t.Sub + t.Vat
            FROM dbo.Sale s
            CROSS APPLY (
                SELECT CAST(ISNULL(SUM(l.LineTotal), 0) AS DECIMAL(18,2)) AS Sub,
                       CAST(ISNULL(SUM(l.LineTotal * l.VatRate), 0) AS DECIMAL(18,2)) AS Vat
                FROM dbo.SaleLine l WHERE l.SaleId = @saleId
            ) t
            WHERE s.SaleId = @saleId;

            -- Cash customers round up to the next R10; card and mobile tender the exact amount.
            UPDATE dbo.Sale
            SET AmountTendered = CASE WHEN PaymentMethod = 1 THEN CEILING(Total / 10.0) * 10 ELSE Total END,
                ChangeDue      = CASE WHEN PaymentMethod = 1 THEN (CEILING(Total / 10.0) * 10) - Total ELSE 0 END
            WHERE SaleId = @saleId;

            UPDATE p
            SET p.QuantityOnHand = p.QuantityOnHand - l.Quantity
            FROM dbo.Product p
            INNER JOIN dbo.SaleLine l ON l.ProductId = p.ProductId AND l.SaleId = @saleId;

            INSERT INTO dbo.StockMovement (ProductId, MovementType, QuantityDelta, QuantityAfter, Reason,
                                           UserId, SaleId, UnitCostAtTime, CreatedAt)
            SELECT l.ProductId, 1 /*Sale*/, -l.Quantity, p.QuantityOnHand,
                   'POS sale ' + s.ReceiptNumber, @cashierId, @saleId, p.CostPrice, @saleDate
            FROM dbo.SaleLine l
            INNER JOIN dbo.Product p ON p.ProductId = l.ProductId
            INNER JOIN dbo.Sale s ON s.SaleId = l.SaleId
            WHERE l.SaleId = @saleId;

            SET @saleIdx = @saleIdx + 1;
        END

        SET @cashierIdx = @cashierIdx + 1;
    END

    SET @dayOffset = @dayOffset + 1;
END
GO

PRINT 'Sales history generated.';
GO

/* ---------- Waste write-offs (feeds the expiry/waste report) ---------- */
DECLARE @manager2 INT = (SELECT UserId FROM dbo.[User] WHERE Username = 'a.right');

INSERT INTO dbo.StockMovement (ProductId, MovementType, QuantityDelta, QuantityAfter, Reason, UserId, UnitCostAtTime, CreatedAt)
SELECT p.ProductId, 4 /*Waste*/, -w.Qty, p.QuantityOnHand - w.Qty,
       w.Reason, @manager2, p.CostPrice, DATEADD(DAY, -w.DaysAgo, GETDATE())
FROM dbo.Product p
INNER JOIN (VALUES
      ('Fresh Tomatoes 500g',   4, 'Past sell-by date, removed from shelf', 3),
      ('White Bread 700g',      6, 'End-of-day bakery write-off',           2),
      ('Bakery Assorted Pastry',3, 'End-of-day bakery write-off',           2),
      ('Baby Spinach 200g',     5, 'Wilted, removed from shelf',            7),
      ('Full Cream Milk 2L',    2, 'Damaged packaging',                    11),
      ('Bananas 1kg',           7, 'Over-ripe, removed from shelf',        14),
      ('Beef Mince 500g',       2, 'Expired, disposed of',                 19),
      ('Farm Vegetables Mix',   4, 'Past sell-by date, removed from shelf',26)
) AS w(ProductName, Qty, Reason, DaysAgo) ON w.ProductName = p.Name
WHERE p.QuantityOnHand > w.Qty;

UPDATE p
SET p.QuantityOnHand = p.QuantityOnHand - w.Qty
FROM dbo.Product p
INNER JOIN (VALUES
      ('Fresh Tomatoes 500g',   4), ('White Bread 700g',       6),
      ('Bakery Assorted Pastry',3), ('Baby Spinach 200g',      5),
      ('Full Cream Milk 2L',    2), ('Bananas 1kg',            7),
      ('Beef Mince 500g',       2), ('Farm Vegetables Mix',    4)
) AS w(ProductName, Qty) ON w.ProductName = p.Name
WHERE p.QuantityOnHand > w.Qty;
GO

/* ---------- Stock-take corrections that leave the store's staples low ----------
   These are ordinary manual adjustments, traceable to the manager, exactly as the
   Inventory screen would record them. */
DECLARE @mgr INT = (SELECT UserId FROM dbo.[User] WHERE Username = 'a.right');

DECLARE @corrections TABLE (ProductName NVARCHAR(120), TargetQty INT);
INSERT INTO @corrections VALUES
 ('Full Cream Milk 2L',   4),
 ('White Bread 700g',     9),
 ('Fresh Tomatoes 500g',  6),
 ('Cheddar Cheese 400g',  5),
 ('Boerewors 1kg',        3);

INSERT INTO dbo.StockMovement (ProductId, MovementType, QuantityDelta, QuantityAfter, Reason, UserId, UnitCostAtTime, CreatedAt)
SELECT p.ProductId, 2 /*Adjustment*/, c.TargetQty - p.QuantityOnHand, c.TargetQty,
       'Stock take correction - shelf count differed from system', @mgr, p.CostPrice, DATEADD(HOUR, -20, GETDATE())
FROM dbo.Product p INNER JOIN @corrections c ON c.ProductName = p.Name
WHERE p.QuantityOnHand <> c.TargetQty;

UPDATE p SET p.QuantityOnHand = c.TargetQty
FROM dbo.Product p INNER JOIN @corrections c ON c.ProductName = p.Name;
GO

/* ---------- Stock batches ----------
   Batches describe what is physically on the shelf right now, so their remaining
   quantities are allocated out of each product's current on-hand figure.  Shelf
   life comes from the category: perishables get days, dry goods get months. */
INSERT INTO dbo.StockBatch (ProductId, BatchCode, QuantityReceived, QuantityRemaining, ReceivedDate, ExpiryDate, UnitCost)
SELECT p.ProductId,
       'B' + RIGHT('000000' + CAST(p.ProductId AS VARCHAR(10)), 4) + '-A',
       CEILING(p.QuantityOnHand * 0.6),
       CEILING(p.QuantityOnHand * 0.6),
       DATEADD(DAY, -7, GETDATE()),
       CASE c.Name
            WHEN 'Bakery'   THEN DATEADD(DAY,  2 + ABS(CHECKSUM(NEWID()) % 1000000) % 3,  GETDATE())
            WHEN 'Dairy'    THEN DATEADD(DAY,  5 + ABS(CHECKSUM(NEWID()) % 1000000) % 10, GETDATE())
            WHEN 'Produce'  THEN DATEADD(DAY,  3 + ABS(CHECKSUM(NEWID()) % 1000000) % 7,  GETDATE())
            WHEN 'Butchery' THEN DATEADD(DAY,  2 + ABS(CHECKSUM(NEWID()) % 1000000) % 5,  GETDATE())
            WHEN 'Frozen'   THEN DATEADD(DAY, 120 + ABS(CHECKSUM(NEWID()) % 1000000) % 120, GETDATE())
            WHEN 'Household'THEN NULL
            ELSE DATEADD(DAY, 180 + ABS(CHECKSUM(NEWID()) % 1000000) % 300, GETDATE())
       END,
       p.CostPrice
FROM dbo.Product p
INNER JOIN dbo.Category c ON c.CategoryId = p.CategoryId
WHERE p.QuantityOnHand > 0;

-- Remainder goes into an older batch that expires sooner - a few are already past
-- their date, which is precisely what the expiry report is meant to surface.
INSERT INTO dbo.StockBatch (ProductId, BatchCode, QuantityReceived, QuantityRemaining, ReceivedDate, ExpiryDate, UnitCost)
SELECT p.ProductId,
       'B' + RIGHT('000000' + CAST(p.ProductId AS VARCHAR(10)), 4) + '-B',
       p.QuantityOnHand - b.Allocated,
       p.QuantityOnHand - b.Allocated,
       DATEADD(DAY, -18, GETDATE()),
       CASE c.Name
            WHEN 'Bakery'   THEN DATEADD(DAY, -1, GETDATE())
            WHEN 'Dairy'    THEN DATEADD(DAY,  1 + ABS(CHECKSUM(NEWID()) % 1000000) % 4, GETDATE())
            WHEN 'Produce'  THEN DATEADD(DAY, -2 + ABS(CHECKSUM(NEWID()) % 1000000) % 5, GETDATE())
            WHEN 'Butchery' THEN DATEADD(DAY, -1 + ABS(CHECKSUM(NEWID()) % 1000000) % 3, GETDATE())
            WHEN 'Household'THEN NULL
            ELSE DATEADD(DAY, 60 + ABS(CHECKSUM(NEWID()) % 1000000) % 120, GETDATE())
       END,
       p.CostPrice
FROM dbo.Product p
INNER JOIN dbo.Category c ON c.CategoryId = p.CategoryId
CROSS APPLY (SELECT SUM(QuantityRemaining) AS Allocated FROM dbo.StockBatch sb WHERE sb.ProductId = p.ProductId) b
WHERE p.QuantityOnHand - ISNULL(b.Allocated, 0) > 0;
GO

PRINT 'Stock batches created.';
GO

/* ---------- Purchase orders across every status ---------- */
DECLARE @mgrPo INT = (SELECT UserId FROM dbo.[User] WHERE Username = 'a.right');
DECLARE @mgr2  INT = (SELECT UserId FROM dbo.[User] WHERE Username = 't.jacobs');

DECLARE @orders TABLE (
    Seq INT, SupplierId INT, Status INT, DaysAgo INT, LeadDays INT,
    DeliveredDaysAgo INT NULL, FillPercent INT, Note NVARCHAR(200)
);
INSERT INTO @orders (Seq, SupplierId, Status, DaysAgo, LeadDays, DeliveredDaysAgo, FillPercent, Note) VALUES
 (1, 1, 4, 52, 2,  50, 100, 'Delivered in full, on time'),
 (2, 2, 4, 46, 1,  45, 100, 'Delivered in full, on time'),
 (3, 3, 4, 40, 1,  38, 100, 'Delivered one day late'),
 (4, 4, 4, 35, 4,  28,  90, 'Short delivered - two cases missing'),
 (5, 5, 4, 30, 3,  26, 100, 'Delivered in full, on time'),
 (6, 1, 4, 24, 2,  19, 100, 'Delivered three days late'),
 (7, 2, 4, 18, 1,  17, 100, 'Delivered in full, on time'),
 (8, 4, 3, 12, 4,   6,  60, 'Partial delivery, balance to follow'),
 (9, 1, 3,  9, 2,   4,  75, 'Partial delivery, balance to follow'),
 (10,3, 2,  6, 1, NULL,   0, 'Confirmed by supplier, awaiting delivery'),
 (11,5, 2,  4, 3, NULL,   0, 'Confirmed by supplier, awaiting delivery'),
 (12,2, 2,  3, 1, NULL,   0, 'Confirmed by supplier, awaiting delivery'),
 (13,4, 1,  2, 4, NULL,   0, 'Sent to supplier, not yet confirmed'),
 (14,1, 1,  1, 2, NULL,   0, 'Sent to supplier, not yet confirmed'),
 (15,5, 0,  0, 3, NULL,   0, 'Draft raised from the low-stock report'),
 (16,3, 5, 21, 1, NULL,   0, 'Cancelled - ordered in error');

INSERT INTO dbo.PurchaseOrder (OrderNumber, SupplierId, CreatedByUserId, Status, CreatedAt, SentAt,
                               ExpectedDate, ConfirmedAt, DeliveredAt, Notes)
SELECT 'PO-' + RIGHT('0000' + CAST(1000 + o.Seq AS VARCHAR(10)), 5),
       o.SupplierId,
       CASE WHEN o.Seq % 3 = 0 THEN @mgr2 ELSE @mgrPo END,
       o.Status,
       DATEADD(DAY, -o.DaysAgo, GETDATE()),
       CASE WHEN o.Status = 0 THEN NULL ELSE DATEADD(DAY, -o.DaysAgo, GETDATE()) END,
       DATEADD(DAY, -o.DaysAgo + o.LeadDays, GETDATE()),
       CASE WHEN o.Status >= 2 AND o.Status <> 5 THEN DATEADD(HOUR, 6 - o.DaysAgo * 24, GETDATE()) ELSE NULL END,
       CASE WHEN o.DeliveredDaysAgo IS NOT NULL AND o.Status = 4 THEN DATEADD(DAY, -o.DeliveredDaysAgo, GETDATE()) ELSE NULL END,
       o.Note
FROM @orders o
ORDER BY o.Seq;

-- Three lines per order, drawn from that supplier's own catalogue.
INSERT INTO dbo.PurchaseOrderLine (PurchaseOrderId, ProductId, QuantityOrdered, QuantityReceived, UnitCost)
SELECT po.PurchaseOrderId, x.ProductId, x.Qty,
       CASE WHEN po.Status = 4 THEN CAST(x.Qty * o.FillPercent / 100.0 AS INT)
            WHEN po.Status = 3 THEN CAST(x.Qty * o.FillPercent / 100.0 AS INT)
            ELSE 0 END,
       x.CostPrice
FROM dbo.PurchaseOrder po
INNER JOIN @orders o ON 'PO-' + RIGHT('0000' + CAST(1000 + o.Seq AS VARCHAR(10)), 5) = po.OrderNumber
CROSS APPLY (
    SELECT TOP 3 p.ProductId, p.CostPrice, (p.ReorderLevel * 4) AS Qty
    FROM dbo.Product p
    WHERE p.SupplierId = po.SupplierId AND p.IsActive = 1
    ORDER BY NEWID()
) x;

-- Receiving goods raises stock, so the delivered orders leave a matching audit trail.
INSERT INTO dbo.StockMovement (ProductId, MovementType, QuantityDelta, QuantityAfter, Reason, UserId,
                               PurchaseOrderId, UnitCostAtTime, CreatedAt)
SELECT l.ProductId, 3 /*Receipt*/, l.QuantityReceived, p.QuantityOnHand,
       'Goods received against ' + po.OrderNumber, po.CreatedByUserId, po.PurchaseOrderId,
       l.UnitCost, ISNULL(po.DeliveredAt, po.ExpectedDate)
FROM dbo.PurchaseOrderLine l
INNER JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = l.PurchaseOrderId
INNER JOIN dbo.Product p ON p.ProductId = l.ProductId
WHERE l.QuantityReceived > 0;
GO

PRINT 'Purchase orders created.';
GO

/* ---------- Cash-ups ----------
   Expected cash is opening float plus every completed cash sale on the shift -
   the same calculation CashUpService performs, so the seeded rows and the live
   ones are directly comparable. */
INSERT INTO dbo.CashUp (ShiftId, UserId, ExpectedCash, CountedCash, Variance, Notes, CreatedAt)
SELECT s.ShiftId,
       s.CashierUserId,
       e.Expected,
       e.Expected + v.Variance,
       v.Variance,
       CASE WHEN v.Variance = 0 THEN 'Drawer balanced.'
            WHEN v.Variance < 0 THEN 'Short on the count - recounted twice.'
            ELSE 'Over on the count - recounted twice.' END,
       s.ClosedAt
FROM dbo.Shift s
CROSS APPLY (
    SELECT s.OpeningFloat + ISNULL(SUM(sa.Total), 0) AS Expected
    FROM dbo.Sale sa
    WHERE sa.ShiftId = s.ShiftId AND sa.Status = 1 AND sa.PaymentMethod = 1
) e
CROSS APPLY (
    -- Most drawers balance; roughly one in eight is out by a small amount.
    SELECT CASE ABS(CHECKSUM(s.ShiftId, 'v') % 1000000) % 8
                WHEN 0 THEN CAST(-10.00 AS DECIMAL(18,2))
                WHEN 3 THEN CAST(  5.00 AS DECIMAL(18,2))
                ELSE CAST(0.00 AS DECIMAL(18,2))
           END AS Variance
) v
WHERE s.Status = 2;

-- The most recent cash-up is deliberately R20 short and still unreviewed, so the
-- manager's reconciliation queue has something waiting on first login.
UPDATE dbo.CashUp
SET ExpectedCash = ExpectedCash,
    CountedCash  = ExpectedCash - 20.00,
    Variance     = -20.00,
    Notes        = 'Short by R20 - no obvious cause found, escalated to the manager.',
    ReviewedByUserId = NULL,
    ReviewedAt   = NULL
WHERE CashUpId = (SELECT TOP 1 CashUpId FROM dbo.CashUp ORDER BY CreatedAt DESC);

-- Older variances have already been looked at and signed off.
UPDATE c
SET ReviewedByUserId = (SELECT UserId FROM dbo.[User] WHERE Username = 'a.right'),
    ReviewedAt = DATEADD(HOUR, 14, c.CreatedAt),
    ReviewNote = 'Reviewed against till roll - accepted.'
FROM dbo.CashUp c
WHERE c.Variance <> 0 AND c.CreatedAt < DATEADD(DAY, -3, GETDATE());
GO

PRINT 'Cash-ups created.';
GO

/* ---------- Summary ---------- */
SELECT 'Products'        AS TableName, COUNT(*) AS [Rows] FROM dbo.Product
UNION ALL SELECT 'Users',           COUNT(*) FROM dbo.[User]
UNION ALL SELECT 'Shifts',          COUNT(*) FROM dbo.Shift
UNION ALL SELECT 'Sales',           COUNT(*) FROM dbo.Sale
UNION ALL SELECT 'Sale lines',      COUNT(*) FROM dbo.SaleLine
UNION ALL SELECT 'Stock movements', COUNT(*) FROM dbo.StockMovement
UNION ALL SELECT 'Stock batches',   COUNT(*) FROM dbo.StockBatch
UNION ALL SELECT 'Purchase orders', COUNT(*) FROM dbo.PurchaseOrder
UNION ALL SELECT 'Cash-ups',        COUNT(*) FROM dbo.CashUp;
GO

PRINT 'SmartShelf seed complete.';
GO
