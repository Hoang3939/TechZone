CREATE DATABASE ShopDienTu1
use ShopDienTu1
go

CREATE TABLE Ranks (
	RankID INT PRIMARY KEY IDENTITY(1,1),
	RankName NVARCHAR(50) NOT NULL,
	Description NVARCHAR(255),
	MinimumPoints INT NOT NULL,
	CreatedAt DATETIME DEFAULT GETDATE(),
	DiscountPercentage DECIMAL(5, 2) NOT NULL
);

-- Tạo bảng Users để quản lý thông tin người dùng
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    UserName VARCHAR(20) NOT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Password NVARCHAR(255) NOT NULL,
    Phone NVARCHAR(15),
	Address NVARCHAR(255),
    Role NVARCHAR(20) DEFAULT 'Customer', -- Quản lý phân quyền: Admin, Customer
    CreatedAt DATETIME DEFAULT GETDATE(),
	RankID INT,
	Points INT,
	IsTwoFactorEnabled BIT NOT NULL DEFAULT 0,
	FOREIGN KEY (RankID) REFERENCES Ranks(RankID)
);

-- Bảng Tỉnh/Thành phố
CREATE TABLE Provinces (
    ProvinceID INT PRIMARY KEY IDENTITY(1,1),
    ProvinceName NVARCHAR(100) NOT NULL UNIQUE
); 

-- Bảng Quận/Huyện (Liên kết với Tỉnh/Thành phố)
CREATE TABLE Districts (
    DistrictID INT PRIMARY KEY IDENTITY(1,1),
    ProvinceID INT NOT NULL,
    DistrictName NVARCHAR(100) NOT NULL,
    FOREIGN KEY (ProvinceID) REFERENCES Provinces(ProvinceID)
);

-- Bảng Phường/Xã (Liên kết với Quận/Huyện)
CREATE TABLE Wards (
    WardID INT PRIMARY KEY IDENTITY(1,1),
    DistrictID INT NOT NULL,
    WardName NVARCHAR(100) NOT NULL,
    FOREIGN KEY (DistrictID) REFERENCES Districts(DistrictID)
);

CREATE TABLE UserAddresses (
    UserAddressID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    Address NVARCHAR(255) NOT NULL,          -- Số nhà, tên đường cụ thể
    ProvinceID INT NOT NULL,                 -- ID Tỉnh/Thành phố
    DistrictID INT NOT NULL,                 -- ID Quận/Huyện
    WardID INT NOT NULL,                     -- ID Phường/Xã
    PhoneNumber NVARCHAR(20) NOT NULL,       -- Số điện thoại cho địa chỉ này
    IsDefault BIT DEFAULT 0 NOT NULL,        -- Đánh dấu địa chỉ mặc định
    CreatedAt DATETIME DEFAULT GETDATE(),    -- Thời gian tạo bản ghi
    AddedAt DATETIME NOT NULL,               -- Thời gian thêm (nếu khác CreatedAt)
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ProvinceID) REFERENCES Provinces(ProvinceID),
    FOREIGN KEY (DistrictID) REFERENCES Districts(DistrictID),
    FOREIGN KEY (WardID) REFERENCES Wards(WardID)
);

CREATE TABLE Categories (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
	Description NVARCHAR(MAX) NULL,
	IsActive BIT DEFAULT 1 NULL,
	CreatedAt DATETIME DEFAULT GETDATE() NULL,
);

CREATE TABLE SubCategories (
    SubCategoryID INT PRIMARY KEY IDENTITY(1,1),
    SubCategoryName NVARCHAR(100) NOT NULL,
    CategoryID INT NOT NULL, 
	Description NVARCHAR(MAX) NULL,
	IsActive BIT DEFAULT 1 NULL,
	CreatedAt DATETIME DEFAULT GETDATE() NULL,
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID) ON DELETE CASCADE
);

-- Tạo bảng Products để quản lý sản phẩm
CREATE TABLE Products (
    ProductID INT PRIMARY KEY IDENTITY(1,1),
    ProductName NVARCHAR(250) NOT NULL,
    Description NVARCHAR(MAX),
    Price DECIMAL(18, 2) NOT NULL,
    SubCategoryID INT,
    StockQuantity INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,
    IsActive BIT DEFAULT 1, -- Sản phẩm có khả dụng không
    FOREIGN KEY (SubCategoryID) REFERENCES SubCategories(SubCategoryID) ON DELETE SET NULL
);

-- Tạo bảng ProductImages để lưu đường dẫn ảnh sản phẩm
CREATE TABLE ProductImages (
    ImageID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT,
    ImagePath NVARCHAR(255) NOT NULL,
    IsMainImage BIT DEFAULT 0, -- Đánh dấu ảnh chính
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE
);

-- Tạo bảng PaymentMethods để quản lý các phương thức thanh toán
CREATE TABLE PaymentMethods (
    PaymentMethodID INT PRIMARY KEY IDENTITY(1,1),
    MethodName NVARCHAR(50) NOT NULL,
    IsActive BIT DEFAULT 1
);

-- Tạo bảng Orders để quản lý đơn hàng
CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT,
    TotalAmount DECIMAL(18, 2),
    PaymentMethodID INT,
    OrderStatus NVARCHAR(50) DEFAULT N'Chờ xác nhận',  -- Trạng thái đơn hàng: 
                                               -- 'Pending' <Đang xử lý>,
                                               -- 'Confirmed' <Đã xác nhận>, 
                                               -- 'Cancelled' <Đã hủy>   
	Notes NVARCHAR(500) NULL,
	OrderNumber VARCHAR(30),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,
	ShippingAddress NVARCHAR(255),
	Discount DECIMAL(18, 0) NULL, -- BỔ SUNG
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
	FOREIGN KEY (PaymentMethodID) REFERENCES PaymentMethods(PaymentMethodID)
);

-- Tạo bảng OrderDetails để quản lý chi tiết từng đơn hàng
CREATE TABLE OrderDetails (
    OrderDetailID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT,
    ProductID INT,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    TotalPrice AS (Quantity * UnitPrice), -- Tính tổng giá tự động
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

CREATE TABLE OrderStatuses (
	OrderStatusID INT IDENTITY(1,1) PRIMARY KEY,
	CreatedAt DATETIME NOT NULL,
	Description NVARCHAR(4000) NULL,
	OrderID INT NOT NULL,
	Status NVARCHAR(50) NULL,
	FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

CREATE TABLE Reviews (
    ReviewID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT,
    UserID INT,
    Rating INT NOT NULL,
    Comment NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

CREATE TABLE Promotions (
    PromotionID INT PRIMARY KEY IDENTITY(1,1),
	PromotionName NVARCHAR(30),
    ProductID INT NULL, -- NULL để hỗ trợ khuyến mãi toàn hệ thống
    DiscountPercentage DECIMAL(5, 2) NOT NULL,
	IsActive BIT DEFAULT 1,
	PromoCode NVARCHAR(15) NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    Description NVARCHAR(255),
	DiscountAmount DECIMAL(18,0) NULL, -- BỔ SUNG
    MinOrderValue  DECIMAL(18,0) NULL, -- BỔ SUNG
	RankID INT,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE SET NULL,
	FOREIGN KEY (RankID) REFERENCES Ranks(RankID)
);

CREATE TABLE WishlistItems (
    WishlistItemID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    ProductID INT NOT NULL,
    AddedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE
);

CREATE TABLE Carts (
    CartID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL, -- Liên kết giỏ hàng với một người dùng cụ thể
    CreatedAt DATETIME DEFAULT GETDATE(), -- Thời gian giỏ hàng được tạo
    LastUpdatedAt DATETIME DEFAULT GETDATE(), -- Thời gian giỏ hàng được cập nhật lần cuối
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE -- Nếu người dùng bị xóa, giỏ hàng của họ cũng bị xóa
);

CREATE TABLE CartItems ( -- Tên bảng mới: CartItems
    CartItemID INT PRIMARY KEY IDENTITY(1,1), -- Tên cột khóa chính mới: CartItemID
    CartID INT NOT NULL, -- Liên kết với giỏ hàng chứa mục này
    ProductID INT NOT NULL, -- Liên kết với sản phẩm cụ thể
    Quantity INT NOT NULL CHECK (Quantity > 0), -- Số lượng sản phẩm, phải lớn hơn 0
    UnitPriceAtAddition DECIMAL(18, 2) NOT NULL, -- Giá sản phẩm tại thời điểm thêm vào giỏ hàng (sau khi áp dụng khuyến mãi nếu có)
    DiscountPercentageAtAddition DECIMAL(5, 2) NOT NULL CHECK (DiscountPercentageAtAddition >= 0 AND DiscountPercentageAtAddition <= 100), -- Phần trăm giảm giá tại thời điểm thêm vào giỏ
    FOREIGN KEY (CartID) REFERENCES Carts(CartID) ON DELETE CASCADE, -- Nếu giỏ hàng bị xóa, các mục trong đó cũng bị xóa
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE -- Nếu sản phẩm bị xóa, mục này cũng bị xóa khỏi giỏ hàng
);


INSERT INTO Categories (CategoryName, Description, IsActive, CreatedAt)
VALUES
(N'Laptop', N'Các dòng máy tính xách tay phổ biến', 1, GETDATE()),
(N'Điện thoại', N'Smartphone các hãng', 1, GETDATE()),
(N'Phụ kiện', N'Các loại phụ kiện máy tính và thiết bị di động tổng hợp (như tai nghe, ổ cứng di động, cáp sạc...)', 1, GETDATE()),
(N'Chuột', N'Các loại chuột máy tính cho mọi nhu cầu từ văn phòng đến gaming, có dây và không dây', 1, GETDATE()),
(N'Bàn phím', N'Các loại bàn phím máy tính đa dạng từ phổ thông đến cơ khí cao cấp, có đèn nền và không đèn nền', 1, GETDATE()),
(N'Màn hình', N'Các loại màn hình hiển thị cho máy tính, từ văn phòng đến gaming chuyên nghiệp với nhiều kích thước và độ phân giải', 1, GETDATE()),
(N'Linh kiện máy tính', N'Các thành phần riêng lẻ dùng để lắp ráp hoặc nâng cấp máy tính để bàn (như CPU, GPU, RAM, ổ cứng)', 1, GETDATE()),
(N'Thiết bị nhà thông minh', N'Các sản phẩm giúp tự động hóa và kết nối ngôi nhà của bạn (như loa thông minh, camera an ninh, đèn thông minh)', 1, GETDATE()),
(N'Tivi & Loa', N'Các loại tivi và thiết bị âm thanh (loa, soundbar, ampli) cho trải nghiệm giải trí tại gia', 1, GETDATE()),
(N'Máy tính bảng', N'Thiết bị di động có màn hình lớn, đa năng cho học tập, làm việc và giải trí', 1, GETDATE());

INSERT INTO SubCategories (SubCategoryName, CategoryID, Description, IsActive, CreatedAt)
VALUES
-- SubCategories cho Laptop (CategoryID 1)
(N'Acer', 1, N'Dòng laptop Acer', 1, GETDATE()),
(N'ASUS', 1, N'Dòng laptop ASUS', 1, GETDATE()),
(N'Dell', 1, N'Dòng laptop Dell', 1, GETDATE()),
(N'Lenovo', 1, N'Dòng laptop Lenovo', 1, GETDATE()),
(N'HP', 1, N'Dòng laptop HP với đa dạng mẫu mã từ phổ thông đến cao cấp', 1, GETDATE()),
(N'MSI', 1, N'Dòng laptop MSI chuyên game và đồ họa với hiệu năng mạnh mẽ', 1, GETDATE()),

-- SubCategories cho Điện thoại (CategoryID 2)
(N'Samsung', 2, N'Điện thoại Samsung', 1, GETDATE()),
(N'Apple', 2, N'Điện thoại iPhone của Apple', 1, GETDATE()),
(N'Xiaomi', 2, N'Điện thoại Xiaomi với cấu hình tốt trong tầm giá và công nghệ hiện đại', 1, GETDATE()),
(N'Oppo', 2, N'Điện thoại Oppo nổi bật với camera selfie và thiết kế thời trang', 1, GETDATE()),

-- SubCategories cho Phụ kiện (CategoryID 3) - Dành cho phụ kiện chung không thuộc chuột/bàn phím/màn hình
(N'Havid', 3, N'Phụ kiện tai nghe Havid', 1, GETDATE()),
(N'Tai nghe Bluetooth', 3, N'Các loại tai nghe không dây tiện lợi', 1, GETDATE()),
(N'Sạc & Cáp', 3, N'Các loại sạc và cáp kết nối cho điện thoại và laptop', 1, GETDATE()),

-- SubCategories cho Chuột (CategoryID 4)
(N'Logitech (Chuột)', 4, N'Các mẫu chuột từ Logitech', 1, GETDATE()),
(N'Razer (Chuột)', 4, N'Các mẫu chuột gaming từ Razer', 1, GETDATE()),
(N'Corsair (Chuột)', 4, N'Các mẫu chuột gaming từ Corsair', 1, GETDATE()),
(N'SteelSeries (Chuột)', 4, N'Các mẫu chuột gaming từ SteelSeries', 1, GETDATE()),

-- SubCategories cho Bàn phím (CategoryID 5)
(N'Logitech (Bàn phím)', 5, N'Các mẫu bàn phím từ Logitech', 1, GETDATE()),
(N'Razer (Bàn phím)', 5, N'Các mẫu bàn phím gaming từ Razer', 1, GETDATE()),
(N'Corsair (Bàn phím)', 5, N'Các mẫu bàn phím gaming từ Corsair', 1, GETDATE()),
(N'SteelSeries (Bàn phím)', 5, N'Các mẫu bàn phím gaming từ SteelSeries', 1, GETDATE()),
(N'Bàn phím Cơ', 5, N'Các loại bàn phím cơ với độ bền cao và cảm giác gõ tốt', 1, GETDATE()),

-- SubCategories cho Màn hình (CategoryID 6)
(N'Dell (Màn hình)', 6, N'Màn hình Dell cho văn phòng và gaming', 1, GETDATE()),
(N'ASUS (Màn hình)', 6, N'Màn hình ASUS cho gaming và đồ họa', 1, GETDATE()),
(N'Samsung (Màn hình)', 6, N'Màn hình Samsung đa dạng chủng loại', 1, GETDATE()),
(N'LG (Màn hình)', 6, N'Màn hình LG với công nghệ hiển thị tiên tiến', 1, GETDATE()),
(N'Màn hình Gaming', 6, N'Các loại màn hình chuyên dụng cho game thủ với tần số quét cao và độ trễ thấp', 1, GETDATE()),
(N'Màn hình Văn phòng', 6, N'Các loại màn hình phù hợp cho công việc văn phòng, học tập và giải trí thông thường', 1, GETDATE())

INSERT INTO Products (ProductName, Description, Price, SubCategoryID, StockQuantity, CreatedAt, IsActive)
VALUES
(N'Acer Nitro 5', N'Laptop gaming tầm trung với hiệu năng ổn định, phù hợp cho cả học tập, làm việc và giải trí với các tựa game phổ biến.', 22000000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Acer'), 10, GETDATE(), 1),
(N'ASUS ROG Strix', N'Laptop gaming cao cấp, mạnh mẽ với card đồ họa rời và màn hình tần số quét cao, mang lại trải nghiệm chơi game mượt mà.', 38000000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'ASUS'), 5, GETDATE(), 1),
(N'Samsung Galaxy A55', N'Điện thoại tầm trung, thiết kế hiện đại, camera chất lượng và pin bền bỉ, đáp ứng tốt nhu cầu hàng ngày.', 9500000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Samsung'), 20, GETDATE(), 1),
(N'Logitech G102', N'Chuột gaming phổ biến với cảm biến chính xác, đèn RGB tùy chỉnh và thiết kế công thái học, lý tưởng cho các game thủ.', 450000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Logitech (Chuột)'), 30, GETDATE(), 1),
(N'Tai nghe Bluetooth Havid 630BT', N'Tai nghe không dây chất lượng cao với âm thanh sống động và pin bền bỉ, mang lại trải nghiệm nghe nhạc tuyệt vời, phù hợp cho mọi hoạt động.', 690000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Havid'), 25, GETDATE(), 1),
(N'Điện thoại iPhone 15 Pro Max', N'Phiên bản cao cấp nhất của iPhone 15 với camera nâng cấp mạnh mẽ, hiệu năng vượt trội từ chip A17 Pro và thiết kế bền bỉ với khung titanium.', 28990000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Apple'), 15, GETDATE(), 1),
(N'Điện thoại iPhone 16 Pro Max', N'Dòng iPhone mới nhất với công nghệ tiên tiến, màn hình sắc nét và hiệu năng đỉnh cao, mang lại trải nghiệm người dùng không giới hạn, đột phá trong nhiếp ảnh và hiệu suất.', 32500000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Apple'), 10, GETDATE(), 1),
(N'Laptop Dell Inspiron 15', N'Laptop đa năng phù hợp cho công việc văn phòng, học tập và giải trí hàng ngày với hiệu năng ổn định, thiết kế mỏng nhẹ và thời lượng pin dài.', 15500000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Dell'), 12, GETDATE(), 1),
(N'Laptop Lenovo Legion 5', N'Laptop gaming mạnh mẽ với hiệu năng vượt trội, tản nhiệt hiệu quả và màn hình tần số quét cao, lý tưởng cho game thủ cần sự ổn định và đồ họa đỉnh cao.', 25000000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Lenovo'), 8, GETDATE(), 1),
(N'Chuột Gaming Logitech G502 X LIGHTSPEED', N'Chuột gaming không dây cao cấp với cảm biến chính xác, thiết kế công thái học và các nút lập trình được, mang lại lợi thế trong mọi trận đấu.', 1850000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Logitech (Chuột)'), 25, GETDATE(), 1),
(N'Bàn phím cơ Corsair K70 RGB', N'Bàn phím cơ chơi game cao cấp với đèn RGB tùy chỉnh và độ bền vượt trội.', 3200000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Corsair (Bàn phím)'), 15, GETDATE(), 1),
(N'Màn hình Dell UltraSharp U2723QE', N'Màn hình 4K sắc nét 27 inch, chuyên nghiệp cho đồ họa và văn phòng.', 12500000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'Dell (Màn hình)'), 10, GETDATE(), 1),
(N'CPU Intel Core i9-14900K', N'Bộ vi xử lý mạnh mẽ nhất cho gaming và các tác vụ nặng.', 14000000, (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = N'CPU'), 5, GETDATE(), 1);

INSERT INTO ProductImages (ProductID, ImagePath, IsMainImage)
VALUES
((SELECT ProductID FROM Products WHERE ProductName = N'Acer Nitro 5'), 'AcerNito5i7.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Acer Nitro 5'), 'AcerNito5i7_1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'ASUS ROG Strix'), 'asus_rog.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'ASUS ROG Strix'), 'asus_rog1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'Samsung Galaxy A55'), 'galaxy_a55.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Samsung Galaxy A55'), 'galaxy_a55_1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'Logitech G102'), 'g102_logitech.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Logitech G102'), 'g102_logitech1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'Tai nghe Bluetooth Havid 630BT'), 'havid_630bt.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Tai nghe Bluetooth Havid 630BT'), 'havid_630bt1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'Điện thoại iPhone 15 Pro Max'), 'ip15promax.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Điện thoại iPhone 15 Pro Max'), 'ip15promax1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'Điện thoại iPhone 16 Pro Max'), 'ip16promaxm.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Điện thoại iPhone 16 Pro Max'), 'ip16promax1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'Laptop Dell Inspiron 15'), 'laptop_dell.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Laptop Dell Inspiron 15'), 'laptop_dell1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'Laptop Lenovo Legion 5'), 'lenovo_legion.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Laptop Lenovo Legion 5'), 'lenovo_legion1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'Chuột Gaming Logitech G502 X LIGHTSPEED'), 'logitech_g502.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Chuột Gaming Logitech G502 X LIGHTSPEED'), 'logitech_g502_1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'Bàn phím cơ Corsair K70 RGB'), 'CorsairK70.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Bàn phím cơ Corsair K70 RGB'), 'CorsairK70_1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'Màn hình Dell UltraSharp U2723QE'), 'DellUltraSharp.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'Màn hình Dell UltraSharp U2723QE'), 'DellUltraSharp1.jpg', 0),
((SELECT ProductID FROM Products WHERE ProductName = N'CPU Intel Core i9-14900K'), 'CPUCoreI9.jpg', 1),
((SELECT ProductID FROM Products WHERE ProductName = N'CPU Intel Core i9-14900K'), 'CPUCoreI91.jpg', 0);

INSERT INTO Promotions (PromotionName, ProductID, DiscountPercentage, IsActive, PromoCode, StartDate, EndDate, Description)
VALUES 
(N'Giảm giá laptop Acer', 1, 10.0, 1, 'ACER10', GETDATE(), DATEADD(DAY, 7, GETDATE()), N'Giảm 10% cho dòng Acer'),
(N'Giảm giá Galaxy', 3, 5.0, 1, 'SAM5', GETDATE(), DATEADD(DAY, 5, GETDATE()), N'Giảm 5% cho Galaxy A55');

INSERT INTO PaymentMethods (MethodName)
VALUES 
(N'Thanh toán khi nhận hàng'),
(N'Chuyển khoản ngân hàng'),
(N'Ví điện tử Momo');


-- Dữ liệu cho bảng Ranks (Tùy chọn)
INSERT INTO Ranks (RankName, Description, MinimumPoints, DiscountPercentage, CreatedAt)
VALUES
(N'Đồng', N'Thành viên đồng', 0, 0.00, GETDATE()),
(N'Bạc', N'Thành viên bạc', 10000, 2.00, GETDATE()),
(N'Vàng', N'Thành viên vàng', 15000, 5.00, GETDATE()),
(N'Bạch kim', N'Thành viên bạch kim', 20000, 8.00, GETDATE()),
(N'Kim cương', N'Thành viên kim cương', 30000, 10.00, GETDATE());