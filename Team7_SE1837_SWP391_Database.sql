CREATE DATABASE BloodDonation
GO
USE BloodDonation
GO

-- BloodBank Table
CREATE TABLE BloodBank (
    BloodBankID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Location NVARCHAR(255),
    ContactNumber VARCHAR(20)
);

-- MedicalCenter Table
CREATE TABLE MedicalCenter (
    MedicalCenterID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Location NVARCHAR(255),
    ContactNumber VARCHAR(20),
    BloodBankID INT,
    FOREIGN KEY (BloodBankID) REFERENCES BloodBank(BloodBankID)
);

-- BloodType Table
CREATE TABLE BloodType (
    BloodTypeID INT PRIMARY KEY IDENTITY(1,1),
    Type VARCHAR(10) NOT NULL UNIQUE -- e.g., A+, B-, O+
);

-- Account Table
CREATE TABLE Account (
    AccountID INT PRIMARY KEY IDENTITY(1,1),
    MedicalCenterID INT NULL,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Password VARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    PermissionLevel INT NOT NULL,
    FOREIGN KEY (MedicalCenterID) REFERENCES MedicalCenter(MedicalCenterID),
    CONSTRAINT CHK_Role CHECK (Role IN ('MedicalCenter', 'Donor', 'Admin', 'Staff')),
    CONSTRAINT CHK_PermissionLevel CHECK (
        (Role IN ('MedicalCenter', 'Donor') AND PermissionLevel = 1) OR
        (Role IN ('Staff') AND PermissionLevel = 2) OR
        (Role IN ('Admin') AND PermissionLevel = 3)
    )
);

-- Donor Table
CREATE TABLE Donor (
    DonorID INT PRIMARY KEY IDENTITY(1,1),
    AccountID INT UNIQUE,
    BloodTypeID INT,
    Name NVARCHAR(255) NOT NULL,
    Gender VARCHAR(10),
    DateOfBirth DATE,
    ContactNumber VARCHAR(20),
    Address NVARCHAR(255),
    IsAvailable BIT,
    CCCD VARCHAR(50),
    FOREIGN KEY (AccountID) REFERENCES Account(AccountID),
    FOREIGN KEY (BloodTypeID) REFERENCES BloodType(BloodTypeID)
);

-- BloodRequest Table
CREATE TABLE BloodRequest (
    BloodRequestID INT PRIMARY KEY IDENTITY(1,1),
    MedicalCenterID INT,
    BloodTypeID INT,   
    Reason NVARCHAR(255),
    RequestDate DATE,
    Quantity DECIMAL(10,2),
    IsEmergency BIT,
    IsCompatible BIT,
	Status VARCHAR(50) DEFAULT 'Pending' 
           CHECK (Status IN ('Pending', 'Completed', 'Rejected', 'Canceled', 'Approved')),
    BloodGiven NVARCHAR(50),
    FOREIGN KEY (MedicalCenterID) REFERENCES MedicalCenter(MedicalCenterID),
    FOREIGN KEY (BloodTypeID) REFERENCES BloodType(BloodTypeID)
);

-- DonorBloodRequest Table
CREATE TABLE DonorBloodRequest (
    DonorBloodRequestID INT PRIMARY KEY IDENTITY(1,1),
    BloodRequestID INT,
    DonorID INT,
    DonationDate DATE,
    QuantityDonated DECIMAL(10,2),
    FOREIGN KEY (BloodRequestID) REFERENCES BloodRequest(BloodRequestID),
    FOREIGN KEY (DonorID) REFERENCES Donor(DonorID),
    UNIQUE (BloodRequestID, DonorID)
);

-- DonationAppointment Table
CREATE TABLE DonationAppointment (
    AppointmentID INT PRIMARY KEY IDENTITY(1,1),
    DonorID INT,
    MedicalCenterID INT,
    BloodTypeID INT,
    AppointmentDate DATE,
    TimeSlot NVARCHAR(10) CHECK (TimeSlot IN (N'Sáng', N'Chiều')),
    Status VARCHAR(50),
    HealthSurvey NVARCHAR(MAX),
    QuantityDonated DECIMAL(10,2),
    FOREIGN KEY (DonorID) REFERENCES Donor(DonorID),
    FOREIGN KEY (MedicalCenterID) REFERENCES MedicalCenter(MedicalCenterID),
    FOREIGN KEY (BloodTypeID) REFERENCES BloodType(BloodTypeID)
);

-- DonationCertificate Table
CREATE TABLE DonationCertificate (
    CertificateID INT PRIMARY KEY IDENTITY(1,1),
    AppointmentID INT UNIQUE,
    IssueDate DATE,
    CertificateDetails NVARCHAR(250),
    FOREIGN KEY (AppointmentID) REFERENCES DonationAppointment(AppointmentID)
);

-- BloodInventory Table
CREATE TABLE BloodInventory (
    InventoryID INT PRIMARY KEY IDENTITY(1,1),
    BloodTypeID INT,
    BloodBankID INT,
    Quantity DECIMAL(10,2),
    LastUpdated DATETIME,
    FOREIGN KEY (BloodTypeID) REFERENCES BloodType(BloodTypeID),
    FOREIGN KEY (BloodBankID) REFERENCES BloodBank(BloodBankID)
);

-- HealthSurvey Table
CREATE TABLE HealthSurvey (
    SurveyID INT PRIMARY KEY IDENTITY,
    AppointmentID INT FOREIGN KEY REFERENCES DonationAppointment(AppointmentID),
    QuestionCode VARCHAR(500),
    Answer NVARCHAR(100)
);

-- Notifications Table
CREATE TABLE Notifications (
    NotificationID INT IDENTITY(1,1) PRIMARY KEY,
    DonorID INT NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    SentAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsRead BIT NOT NULL DEFAULT 0,
    Type NVARCHAR(50) NULL,
    IsConfirmed BIT NOT NULL DEFAULT 0,
    AccountID INT NULL,
    BloodRequestID INT NULL,
    FOREIGN KEY (DonorID) REFERENCES Donor(DonorID),
    FOREIGN KEY (BloodRequestID) REFERENCES BloodRequest(BloodRequestID)
);

-- News Table
CREATE TABLE News (
    NewsId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Url NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,
    Type NVARCHAR(50) 
);
------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 1. Insert into BloodBank
INSERT INTO BloodBank (Name, Location, ContactNumber)
VALUES 
    (N'Ngân hàng máu Số 1', N'82 Đường 36, Linh Đông, Thủ Đức, Hồ Chí Minh, Việt Nam', '0922334455');

-- 2. Insert into MedicalCenter
INSERT INTO MedicalCenter (Name, Location, ContactNumber, BloodBankID)
VALUES  
    (N'Cơ sở y tế Linh Đông', N'79 Đình Phong Phú, Tăng Nhơn Phú B, Thủ Đức, Hồ Chí Minh, Việt Nam', '02438693731', 1),
    (N'Cơ sở y tế Tam Bình', N'161 Đường Võ Nguyên Giáp, Thảo Điền, Thủ Đức, Hồ Chí Minh 70000, Việt Nam', '02838554137', 1),
    (N'Cơ sở y tế Hiệp Bình Chánh', N'12 Đặng Văn Bi, Trường Thọ, Thủ Đức, Hồ Chí Minh 70000, Việt Nam', '02343823473', 1);

-- 3. Insert into Account (For MedicalCenter and Donor)
INSERT INTO Account (MedicalCenterID, Username, Password, Email, Role, PermissionLevel)
VALUES
    -- MedicalCenter accounts
    (1, 'linhdongmc', 'Medical1', 'linhdongmc@gmail.com', 'MedicalCenter', 1), 
    (2, 'tambinhmc', 'Medical2', 'tambinhmc@gmail.com', 'MedicalCenter', 1),
    (3, 'hiepbinhchanhmc', 'Medical3', 'hiepbinhchanhmc@gmail.com', 'MedicalCenter', 1),
    -- Donor accounts
    (NULL, 'vohoangthang', 'se182121', 'thangvhse182121@fpt.edu.vn', 'Donor', 1),
    (NULL, 'levuvan', 'se190612', 'levuvanbat11@gmail.com', 'Donor', 1),
    (NULL, 'nguyentuankiet', 'se182120', 'kietntse182120@fpt.edu.vn', 'Donor', 1),
    (NULL, 'quanbathanh', 'se180150', 'thanhqbse180150@fpt.edu.vn', 'Donor', 1),
    (NULL, 'nguyenviethuy', 'se173703', 'huynvse173703@fpt.edu.vn', 'Donor', 1),
    -- Admin accounts
    (NULL, 'adminbdss', 'admin12345', 'adminbdss@gmail.com', 'Admin', 3),
    -- Staff accounts
    (NULL, 'staffbdss', 'staff12345', 'staffbdss@gmail.com', 'Staff', 2);

-- 4. Insert into BloodType
INSERT INTO BloodType (Type)
VALUES 
    ('A+'), ('A-'), ('B+'), ('B-'), ('AB+'), ('AB-'), ('O+'), ('O-');

-- 5. Insert into Donor
INSERT INTO Donor (AccountID, BloodTypeID, Name, Gender, DateOfBirth, ContactNumber, Address, IsAvailable, CCCD)
VALUES 
    (4, 3, N'Võ Hoàng Thắng', 'M', '2004-01-19', '0853382267', N'26 Lý Tự Trọng, Bến Nghé, Quận 1, Hồ Chí Minh 700000, Việt Nam', 0, '123456789012'),
    (5, 1, N'Lê Vũ Văn ', 'M', '2005-02-02', '0987654321', N'130 Đường Lê Văn Thịnh, Phường Bình Trưng Tây, Thủ Đức, Hồ Chí Minh, Việt Nam', 0, '234567890123'),
    (6, 4, N'Nguyễn Tuấn Kiệt', 'M', '2004-03-03', '0912348765', N'Lưu Hữu Phước Tân Lập, Đông Hoà, Dĩ An, Bình Dương, Việt Nam', 0, '345678901234'),
    (7, 7, N'Quản Bá Thành', 'M', '2004-04-04', '0934567890', N'37/1 Đặng Văn Bi, Phường Linh Tây, Thủ Đức', 0, '456789012345'),
    (8, 8, N'Nguyễn Việt Huy', 'M', '2003-05-05', '0913456789', N'475A Điện Biên Phủ, Phường 25, Bình Thạnh, Hồ Chí Minh, Việt Nam', 0, '567890123456');

-- 6. Insert into BloodInventory
INSERT INTO BloodInventory (BloodTypeID, BloodBankID, Quantity, LastUpdated)
VALUES 
    (1, 1, 1, '2025-06-01 08:00:00'), -- A+
    (2, 1, 1, '2025-06-01 08:00:00'), -- A-
    (3, 1, 1, '2025-06-01 08:00:00'), -- B+
    (4, 1, 1, '2025-06-01 08:00:00'), -- B-
    (5, 1, 1, '2025-06-01 08:00:00'), -- AB+
    (6, 1, 1, '2025-06-01 08:00:00'), -- AB-
    (7, 1, 1, '2025-06-01 08:00:00'), -- O+
    (8, 1, 1, '2025-06-01 08:00:00'); -- O-


-- 7. Insert into BloodRequest
INSERT INTO BloodRequest (MedicalCenterID, BloodTypeID, Reason, RequestDate, Quantity, IsEmergency, Status, IsCompatible)
VALUES 
    (1, 1, N'Cần máu cho ca phẫu thuật', '2025-06-13', 1, 1, N'Pending',1),
    (2, 2, N'Điều trị thiếu máu', '2025-06-14', 1.25, 0, N'Rejected',0),
    (3, 3, N'Cần máu cho ca mổ tim', '2025-06-15', 0.75, 1, N'Completed',1),
    
    -- 5 COMPLETED
    (1, 1, N'Cần máu cấp cứu A+', '2025-07-01', 1, 1, 'Completed', 1),
    (1, 2, N'Ca phẫu thuật nội soi', '2025-07-02', 1.25, 0, 'Completed', 1),
    (2, 3, N'Mổ tim cần máu', '2025-07-03', 0.75, 1, 'Completed', 1),
    (2, 4, N'Thiếu máu sau sinh', '2025-07-04', 1.25, 0, 'Completed', 1),
    (3, 5, N'Cấp cứu bệnh nhân tai nạn', '2025-07-05', 1.25, 1, 'Completed', 1),

    -- 4 APPROVED
    (3, 6, N'Điều trị thiếu máu mạn tính', '2025-07-06', 1.25, 0, 'Approved', 1),
    (1, 7, N'Ca ghép gan', '2025-07-07', 1, 1, 'Approved', 1),
    (2, 8, N'Bệnh lý máu đông', '2025-07-08', 1.25, 0, 'Approved', 1),
    (3, 1, N'Mổ xương đùi', '2025-06-09', 0.75, 0, 'Approved', 1),

    -- 3 PENDING
    (1, 2, N'Chờ xác minh bệnh nhân', '2025-07-10', 1.25, 0, 'Pending', 1),
    (2, 3, N'Phẫu thuật thần kinh', '2025-07-11', 1, 1, 'Pending', 1),
    (3, 4, N'Chờ xác minh giấy tờ', '2025-07-12', 0.75, 0, 'Pending', 1),

    -- 2 CANCELED
    (1, 5, N'Bệnh nhân hủy lịch', '2025-07-13', 0.75, 0, 'Canceled', 0),
    (2, 6, N'Lý do cá nhân', '2025-07-14', 1.25, 0, 'Canceled', 0),

    -- 1 REJECTED
    (3, 7, N'Lỗi thông tin', '2025-07-15', 0.75, 0, 'Rejected', 0);

-- 8. Blood Donor Allocation
INSERT INTO DonorBloodRequest (BloodRequestID, DonorID, DonationDate, QuantityDonated)
VALUES 
    (1, 1, '2025-06-13', 0.75), 
    (2, 2, '2025-07-02', 1.0),
    (3, 3, '2025-07-03', 0.75),
    (4, 4, '2025-07-04', 1.25),
    (5, 5, '2025-07-05', 1.25);

-- 9. Insert into DonationAppointment
INSERT INTO DonationAppointment (DonorID, BloodTypeID, AppointmentDate, TimeSlot, Status, HealthSurvey,QuantityDonated)
VALUES (
    1, 3,
    '2025-06-20',
    N'Sáng',
    'Pending',
    N'{
      "1_AnhChiDaTungHienMauChua": false,
      "2_HienTaiAnhChiCoMacBenhLyKhong": false,
      "3_TruocDayAnhChiCoMacCacBenhLietKeKhong": false,
      "4_KhoiBenhSauMacCacBenh12Thang": false,
      "4_DuocTruyenMauHoacGayGhepMo": false,
      "4_TiemVaccine": false,
      "5_KhoiBenhSauMacCacBenh6Thang": false,
      "6_KhoiBenhSauMacCacBenh1Thang": false,
      "7_BiCumCamLanhHoNhucDauSotDauHong14Ngay": false,
      "8_DungThuocKhangSinhKhangViêmAspirinCorticoide7Ngay": false,
      "9_HienChiDangMangThaiHoacCoThai12ThangTruoc": false,
      "9_ChamDutThaiKy12ThangGanDay": false,
      "10_AnhChiSanSangHienMauNeuDuDieuKien": true
    }',
    0.75);
    -- Get ID was add
    DECLARE @NewAppointmentID INT = SCOPE_IDENTITY();
    -- Update status to 'Completed'
    UPDATE DonationAppointment
    SET STATUS = 'Completed'
    WHERE AppointmentID = @NewAppointmentID;
    -- Update MedicalCenter for Appointment
    UPDATE DonationAppointment
    SET MedicalCenterID = 1
    WHERE AppointmentID = @NewAppointmentID
-- Appointment for DonorID 2 - BloodTypeID 1 (A+)
INSERT INTO DonationAppointment (DonorID, BloodTypeID, AppointmentDate, TimeSlot, Status, HealthSurvey, QuantityDonated)
VALUES (
    2, 1,
    '2025-07-21',
    N'Sáng',
    'Pending',
    N'{
      "1_AnhChiDaTungHienMauChua": true,
      "2_HienTaiAnhChiCoMacBenhLyKhong": false,
      "3_TruocDayAnhChiCoMacCacBenhLietKeKhong": false,
      "4_KhoiBenhSauMacCacBenh12Thang": false,
      "5_KhoiBenhSauMacCacBenh6Thang": false,
      "6_KhoiBenhSauMacCacBenh1Thang": false,
      "7_BiCumCamLanhHoNhucDauSotDauHong14Ngay": false,
      "8_DungThuocKhangSinhKhangViêmAspirinCorticoide7Ngay": false,
      "9_HienChiDangMangThaiHoacCoThai12ThangTruoc": false,
      "10_AnhChiSanSangHienMauNeuDuDieuKien": true
    }',
    1.0);
DECLARE @Appointment2 INT = SCOPE_IDENTITY();
UPDATE DonationAppointment SET Status = 'Completed', MedicalCenterID = 1 WHERE AppointmentID = @Appointment2;

-- Appointment for DonorID 3 - BloodTypeID 4 (B-)
INSERT INTO DonationAppointment (DonorID, BloodTypeID, AppointmentDate, TimeSlot, Status, HealthSurvey, QuantityDonated)
VALUES (
    3, 4,
    '2025-07-22',
    N'Sáng',
    'Pending',
    N'{
      "1_AnhChiDaTungHienMauChua": true,
      "2_HienTaiAnhChiCoMacBenhLyKhong": false,
      "3_TruocDayAnhChiCoMacCacBenhLietKeKhong": false,
      "4_KhoiBenhSauMacCacBenh12Thang": false,
      "5_KhoiBenhSauMacCacBenh6Thang": false,
      "6_KhoiBenhSauMacCacBenh1Thang": false,
      "7_BiCumCamLanhHoNhucDauSotDauHong14Ngay": false,
      "8_DungThuocKhangSinhKhangViêmAspirinCorticoide7Ngay": false,
      "9_HienChiDangMangThaiHoacCoThai12ThangTruoc": false,
      "10_AnhChiSanSangHienMauNeuDuDieuKien": true
    }',
    0.75);
DECLARE @Appointment3 INT = SCOPE_IDENTITY();
UPDATE DonationAppointment SET Status = 'Completed', MedicalCenterID = 1 WHERE AppointmentID = @Appointment3;

-- Appointment for DonorID 4 - BloodTypeID 7 (O+)
INSERT INTO DonationAppointment (DonorID, BloodTypeID, AppointmentDate, TimeSlot, Status, HealthSurvey, QuantityDonated)
VALUES (
    4, 7,
    '2025-07-23',
    N'Sáng',
    'Pending',
    N'{
      "1_AnhChiDaTungHienMauChua": true,
      "2_HienTaiAnhChiCoMacBenhLyKhong": false,
      "3_TruocDayAnhChiCoMacCacBenhLietKeKhong": false,
      "4_KhoiBenhSauMacCacBenh12Thang": false,
      "5_KhoiBenhSauMacCacBenh6Thang": false,
      "6_KhoiBenhSauMacCacBenh1Thang": false,
      "7_BiCumCamLanhHoNhucDauSotDauHong14Ngay": false,
      "8_DungThuocKhangSinhKhangViêmAspirinCorticoide7Ngay": false,
      "9_HienChiDangMangThaiHoacCoThai12ThangTruoc": false,
      "10_AnhChiSanSangHienMauNeuDuDieuKien": true
    }',
    1.25);
DECLARE @Appointment4 INT = SCOPE_IDENTITY();
UPDATE DonationAppointment SET Status = 'Completed', MedicalCenterID = 1 WHERE AppointmentID = @Appointment4;

-- Appointment for DonorID 5 - BloodTypeID 8 (O-)
INSERT INTO DonationAppointment (DonorID, BloodTypeID, AppointmentDate, TimeSlot, Status, HealthSurvey, QuantityDonated)
VALUES (
    5, 8,
    '2025-07-24',
    N'Sáng',
    'Pending',
    N'{
      "1_AnhChiDaTungHienMauChua": true,
      "2_HienTaiAnhChiCoMacBenhLyKhong": false,
      "3_TruocDayAnhChiCoMacCacBenhLietKeKhong": false,
      "4_KhoiBenhSauMacCacBenh12Thang": false,
      "5_KhoiBenhSauMacCacBenh6Thang": false,
      "6_KhoiBenhSauMacCacBenh1Thang": false,
      "7_BiCumCamLanhHoNhucDauSotDauHong14Ngay": false,
      "8_DungThuocKhangSinhKhangViêmAspirinCorticoide7Ngay": false,
      "9_HienChiDangMangThaiHoacCoThai12ThangTruoc": false,
      "10_AnhChiSanSangHienMauNeuDuDieuKien": true
    }',
    1.25);
DECLARE @Appointment5 INT = SCOPE_IDENTITY();
UPDATE DonationAppointment SET Status = 'Completed', MedicalCenterID = 1 WHERE AppointmentID = @Appointment5;


-- 10. Insert into Certificate
INSERT INTO DonationCertificate (AppointmentID, IssueDate, CertificateDetails)
VALUES (
    @NewAppointmentID,
    GETDATE(),
    N'Chứng chỉ hiến máu cho Võ Hoàng Thắng, nhóm máu B+, hiến 1cc tại cơ sở y tế Linh Đông, Thủ Đức, TP.HCM ngày 2025-06-20.');
    UPDATE DonationCertificate
    SET IssueDate = '2025-06-20'
    WHERE CertificateID = 1;

    -- Certificate for Appointment
INSERT INTO DonationCertificate (AppointmentID, IssueDate, CertificateDetails)
VALUES 
(@Appointment2, '2025-07-21', N'Chứng chỉ hiến máu cho Lê Vũ Văn, nhóm máu A+, hiến 1cc tại cơ sở y tế Linh Đông.'),
(@Appointment3, '2025-07-22', N'Chứng chỉ hiến máu cho Nguyễn Tuấn Kiệt, nhóm máu B-, hiến 0.75cc tại cơ sở y tế Linh Đông.'),
(@Appointment4, '2025-07-23', N'Chứng chỉ hiến máu cho Quản Bá Thành, nhóm máu O+, hiến 1.25cc tại cơ sở y tế Linh Đông.'),
(@Appointment5, '2025-07-24', N'Chứng chỉ hiến máu cho Nguyễn Việt Huy, nhóm máu O-, hiến 1.25cc tại cơ sở y tế Linh Đông.');


-- 11. Insert into HealthSurvey
INSERT INTO HealthSurvey (AppointmentID, QuestionCode, Answer)
SELECT 
    da.AppointmentID,
    j.[key] AS QuestionCode,
    TRY_CAST(j.[value] AS BIT) AS Answer
FROM DonationAppointment da
CROSS APPLY OPENJSON(da.HealthSurvey) AS j
WHERE ISJSON(da.HealthSurvey) = 1;
UPDATE BloodRequest SET BloodGiven = N'A+' WHERE BloodRequestID = 1; -- Compatibility
UPDATE BloodRequest SET BloodGiven = N'B+' WHERE BloodRequestID = 2; -- Not Compatibility
UPDATE BloodRequest SET BloodGiven = N'O-' WHERE BloodRequestID = 3; -- Compatibility

-- 12. Insert into News
INSERT INTO News (Title, Url, CreatedAt, UpdatedAt, Type)
VALUES
    (N'Cần gấp các nhóm máu A, B, AB, O', N'https://giotmauvang.org.vn/news/can-gap-cac-nhom-mau-a-b-ab-o', '2025-07-10 09:00:00', '2025-07-15 09:00:00', N'news'),
    (N'TPHCM KÊU GỌI THAM GIA HIẾN MÁU CỨU NGƯỜI', N'https://giotmauvang.org.vn/news/tphcm-keu-goi-tham-gia-hien-mau-cuu-nguoi', '2025-07-16 09:00:00', '2025-07-17 09:00:00', N'news'),
    (N'BS. Phạm Tuấn Dương - Bác sĩ Huyết Học', N'http://www.thiduakhenthuongvn.org.vn/dien-hinh-tien-tien/bac-si-hon-30-nam-gan-bo-voi-nganh-truyen-mau', GETDATE(), NULL, N'blogs'),
    (N'BS. Nguyễn Thanh Hưng - Bác sĩ Huyết Học', N'https://suckhoedoisong.vn/bac-si-tre-hon-40-lan-hien-mau-169240724154313626.htm', GETDATE(), NULL, N'blogs'),
    (N'Chú Ngô Quốc Tùng - Tình nguyện viên', N'https://hienmaunhandao.org.vn/blogs/guong-hien-mau-tieu-bieu/16-nam-53-lan-hien-mau', GETDATE(), NULL, N'blogs'),
    (N'Ông Rodger Webster - Tình nguyện viên', N'https://hienmaunhandao.org.vn/blogs/guong-hien-mau-tieu-bieu/cu-ong-nguoi-anh-hien-hon-60-lit-mau-cuu-nguoi', GETDATE(), NULL, N'blogs');