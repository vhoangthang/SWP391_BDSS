USE [master]
GO
/****** Object:  Database [BloodDonation]    Script Date: 7/2/2025 8:23:51 AM ******/
CREATE DATABASE [BloodDonation]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'BloodDonation', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\BloodDonation.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'BloodDonation_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\BloodDonation_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [BloodDonation] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [BloodDonation].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [BloodDonation] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [BloodDonation] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [BloodDonation] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [BloodDonation] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [BloodDonation] SET ARITHABORT OFF 
GO
ALTER DATABASE [BloodDonation] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [BloodDonation] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [BloodDonation] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [BloodDonation] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [BloodDonation] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [BloodDonation] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [BloodDonation] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [BloodDonation] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [BloodDonation] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [BloodDonation] SET  ENABLE_BROKER 
GO
ALTER DATABASE [BloodDonation] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [BloodDonation] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [BloodDonation] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [BloodDonation] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [BloodDonation] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [BloodDonation] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [BloodDonation] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [BloodDonation] SET RECOVERY FULL 
GO
ALTER DATABASE [BloodDonation] SET  MULTI_USER 
GO
ALTER DATABASE [BloodDonation] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [BloodDonation] SET DB_CHAINING OFF 
GO
ALTER DATABASE [BloodDonation] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [BloodDonation] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [BloodDonation] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [BloodDonation] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'BloodDonation', N'ON'
GO
ALTER DATABASE [BloodDonation] SET QUERY_STORE = ON
GO
ALTER DATABASE [BloodDonation] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [BloodDonation]
GO
/****** Object:  Table [dbo].[Account]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Account](
	[AccountID] [int] IDENTITY(1,1) NOT NULL,
	[MedicalCenterID] [int] NULL,
	[Username] [nvarchar](50) NOT NULL,
	[Password] [varchar](50) NOT NULL,
	[Email] [nvarchar](100) NOT NULL,
	[Role] [varchar](20) NOT NULL,
	[PermissionLevel] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[AccountID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BloodBank]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BloodBank](
	[BloodBankID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[Location] [nvarchar](255) NULL,
	[ContactNumber] [varchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[BloodBankID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BloodInventory]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BloodInventory](
	[InventoryID] [int] IDENTITY(1,1) NOT NULL,
	[BloodTypeID] [int] NULL,
	[BloodBankID] [int] NULL,
	[Quantity] [decimal](10, 2) NULL,
	[LastUpdated] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[InventoryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BloodRequest]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BloodRequest](
	[BloodRequestID] [int] IDENTITY(1,1) NOT NULL,
	[MedicalCenterID] [int] NULL,
	[BloodTypeID] [int] NULL,
	[Reason] [nvarchar](255) NULL,
	[RequestDate] [date] NULL,
	[Quantity] [decimal](10, 2) NULL,
	[IsEmergency] [bit] NULL,
	[IsCompatible] [bit] NULL,
	[Status] [varchar](50) NULL,
	[BloodGiven] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[BloodRequestID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BloodType]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BloodType](
	[BloodTypeID] [int] IDENTITY(1,1) NOT NULL,
	[Type] [varchar](10) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[BloodTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DonationAppointment]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DonationAppointment](
	[AppointmentID] [int] IDENTITY(1,1) NOT NULL,
	[DonorID] [int] NULL,
	[MedicalCenterID] [int] NULL,
	[BloodTypeID] [int] NULL,
	[AppointmentDate] [date] NULL,
	[TimeSlot] [nvarchar](10) NULL,
	[Status] [varchar](50) NULL,
	[HealthSurvey] [nvarchar](max) NULL,
	[QuantityDonated] [decimal](10, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[AppointmentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DonationCertificate]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DonationCertificate](
	[CertificateID] [int] IDENTITY(1,1) NOT NULL,
	[AppointmentID] [int] NULL,
	[IssueDate] [date] NULL,
	[CertificateDetails] [nvarchar](250) NULL,
PRIMARY KEY CLUSTERED 
(
	[CertificateID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Donor]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Donor](
	[DonorID] [int] IDENTITY(1,1) NOT NULL,
	[AccountID] [int] NULL,
	[BloodTypeID] [int] NULL,
	[Name] [nvarchar](255) NOT NULL,
	[Gender] [varchar](10) NULL,
	[DateOfBirth] [date] NULL,
	[ContactNumber] [varchar](20) NULL,
	[Address] [nvarchar](255) NULL,
	[IsAvailable] [bit] NULL,
	[CCCD] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[DonorID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DonorBloodRequest]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DonorBloodRequest](
	[DonorBloodRequestID] [int] IDENTITY(1,1) NOT NULL,
	[BloodRequestID] [int] NULL,
	[DonorID] [int] NULL,
	[DonationDate] [date] NULL,
	[QuantityDonated] [decimal](10, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[DonorBloodRequestID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[HealthSurvey]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HealthSurvey](
	[SurveyID] [int] IDENTITY(1,1) NOT NULL,
	[AppointmentID] [int] NULL,
	[QuestionCode] [varchar](500) NULL,
	[Answer] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[SurveyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MedicalCenter]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MedicalCenter](
	[MedicalCenterID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[Location] [nvarchar](255) NULL,
	[ContactNumber] [varchar](20) NULL,
	[BloodBankID] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MedicalCenterID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Notifications]    Script Date: 7/2/2025 8:23:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Notifications](
	[NotificationID] [int] IDENTITY(1,1) NOT NULL,
	[DonorID] [int] NOT NULL,
	[Message] [nvarchar](max) NOT NULL,
	[SentAt] [datetime] NOT NULL,
	[IsRead] [bit] NOT NULL,
	[Type] [nvarchar](50) NULL,
	[IsConfirmed] [bit] NOT NULL,
	[AccountID] [int] NULL,
	[BloodRequestID] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[Account] ON 

INSERT [dbo].[Account] ([AccountID], [MedicalCenterID], [Username], [Password], [Email], [Role], [PermissionLevel]) VALUES (1, 1, N'MC1', N'pass456', N'csyt1@gmail.com', N'MedicalCenter', 1)
INSERT [dbo].[Account] ([AccountID], [MedicalCenterID], [Username], [Password], [Email], [Role], [PermissionLevel]) VALUES (2, 2, N'MC2', N'pass789', N'csyt2@gmail.com', N'MedicalCenter', 1)
INSERT [dbo].[Account] ([AccountID], [MedicalCenterID], [Username], [Password], [Email], [Role], [PermissionLevel]) VALUES (3, 3, N'MC3', N'pass321', N'csyt3@gmail.com', N'MedicalCenter', 1)
INSERT [dbo].[Account] ([AccountID], [MedicalCenterID], [Username], [Password], [Email], [Role], [PermissionLevel]) VALUES (4, NULL, N'donor1', N'pass123', N'donor1@gmail.com', N'Donor', 1)
INSERT [dbo].[Account] ([AccountID], [MedicalCenterID], [Username], [Password], [Email], [Role], [PermissionLevel]) VALUES (5, NULL, N'donor2', N'pass234', N'donor2@gmail.com', N'Donor', 1)
INSERT [dbo].[Account] ([AccountID], [MedicalCenterID], [Username], [Password], [Email], [Role], [PermissionLevel]) VALUES (6, NULL, N'donor3', N'pass345', N'donor3@gmail.com', N'Donor', 1)
INSERT [dbo].[Account] ([AccountID], [MedicalCenterID], [Username], [Password], [Email], [Role], [PermissionLevel]) VALUES (7, NULL, N'donor4', N'pass456', N'donor4@gmail.com', N'Donor', 1)
INSERT [dbo].[Account] ([AccountID], [MedicalCenterID], [Username], [Password], [Email], [Role], [PermissionLevel]) VALUES (8, NULL, N'donor5', N'pass567', N'donor5@gmail.com', N'Donor', 1)
INSERT [dbo].[Account] ([AccountID], [MedicalCenterID], [Username], [Password], [Email], [Role], [PermissionLevel]) VALUES (9, NULL, N'admin1', N'pass567', N'admin1@gmail.com', N'Admin', 3)
INSERT [dbo].[Account] ([AccountID], [MedicalCenterID], [Username], [Password], [Email], [Role], [PermissionLevel]) VALUES (10, NULL, N'staff1', N'pass567', N'staff1@gmail.com', N'Staff', 2)
SET IDENTITY_INSERT [dbo].[Account] OFF
GO
SET IDENTITY_INSERT [dbo].[BloodBank] ON 

INSERT [dbo].[BloodBank] ([BloodBankID], [Name], [Location], [ContactNumber]) VALUES (1, N'Ngân hàng máu Số 1', N'82 Đường 36, Linh Đông, Thủ Đức, Hồ Chí Minh, Việt Nam', N'0922334455')
SET IDENTITY_INSERT [dbo].[BloodBank] OFF
GO
SET IDENTITY_INSERT [dbo].[BloodInventory] ON 

INSERT [dbo].[BloodInventory] ([InventoryID], [BloodTypeID], [BloodBankID], [Quantity], [LastUpdated]) VALUES (1, 1, 1, CAST(2.00 AS Decimal(10, 2)), CAST(N'2025-06-01T08:00:00.000' AS DateTime))
INSERT [dbo].[BloodInventory] ([InventoryID], [BloodTypeID], [BloodBankID], [Quantity], [LastUpdated]) VALUES (2, 7, 1, CAST(1.00 AS Decimal(10, 2)), CAST(N'2025-06-01T08:00:00.000' AS DateTime))
SET IDENTITY_INSERT [dbo].[BloodInventory] OFF
GO
SET IDENTITY_INSERT [dbo].[BloodRequest] ON 

INSERT [dbo].[BloodRequest] ([BloodRequestID], [MedicalCenterID], [BloodTypeID], [Reason], [RequestDate], [Quantity], [IsEmergency], [IsCompatible], [Status], [BloodGiven]) VALUES (1, 1, 1, N'Cần máu cho ca phẫu thuật', CAST(N'2025-06-13' AS Date), CAST(1.00 AS Decimal(10, 2)), 1, 1, N'Approved', NULL)
INSERT [dbo].[BloodRequest] ([BloodRequestID], [MedicalCenterID], [BloodTypeID], [Reason], [RequestDate], [Quantity], [IsEmergency], [IsCompatible], [Status], [BloodGiven]) VALUES (2, 2, 2, N'Điều trị thiếu máu', CAST(N'2025-06-14' AS Date), CAST(1.50 AS Decimal(10, 2)), 0, 0, N'Completed', NULL)
INSERT [dbo].[BloodRequest] ([BloodRequestID], [MedicalCenterID], [BloodTypeID], [Reason], [RequestDate], [Quantity], [IsEmergency], [IsCompatible], [Status], [BloodGiven]) VALUES (3, 3, 3, N'Cần máu cho ca mổ tim', CAST(N'2025-06-15' AS Date), CAST(0.50 AS Decimal(10, 2)), 1, 1, N'Completed', NULL)
SET IDENTITY_INSERT [dbo].[BloodRequest] OFF
GO
SET IDENTITY_INSERT [dbo].[BloodType] ON 

INSERT [dbo].[BloodType] ([BloodTypeID], [Type]) VALUES (1, N'A+')
INSERT [dbo].[BloodType] ([BloodTypeID], [Type]) VALUES (2, N'A-')
INSERT [dbo].[BloodType] ([BloodTypeID], [Type]) VALUES (5, N'AB+')
INSERT [dbo].[BloodType] ([BloodTypeID], [Type]) VALUES (6, N'AB-')
INSERT [dbo].[BloodType] ([BloodTypeID], [Type]) VALUES (3, N'B+')
INSERT [dbo].[BloodType] ([BloodTypeID], [Type]) VALUES (4, N'B-')
INSERT [dbo].[BloodType] ([BloodTypeID], [Type]) VALUES (7, N'O+')
INSERT [dbo].[BloodType] ([BloodTypeID], [Type]) VALUES (8, N'O-')
SET IDENTITY_INSERT [dbo].[BloodType] OFF
GO
SET IDENTITY_INSERT [dbo].[DonationAppointment] ON 

INSERT [dbo].[DonationAppointment] ([AppointmentID], [DonorID], [MedicalCenterID], [BloodTypeID], [AppointmentDate], [TimeSlot], [Status], [HealthSurvey], [QuantityDonated]) VALUES (1, 1, 1, 1, CAST(N'2025-06-20' AS Date), N'Sáng', N'Completed', N'{
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
    }', CAST(2.30 AS Decimal(10, 2)))
SET IDENTITY_INSERT [dbo].[DonationAppointment] OFF
GO
SET IDENTITY_INSERT [dbo].[DonationCertificate] ON 

INSERT [dbo].[DonationCertificate] ([CertificateID], [AppointmentID], [IssueDate], [CertificateDetails]) VALUES (1, 1, CAST(N'2025-06-27' AS Date), N'Chứng chỉ hiến máu cho Nguyễn Văn A, nhóm máu A+, hiến 350ml tại cơ sở y tế Linh Đông, Thủ Đức, TP.HCM ngày 2025-06-20.')
SET IDENTITY_INSERT [dbo].[DonationCertificate] OFF
GO
SET IDENTITY_INSERT [dbo].[Donor] ON 

INSERT [dbo].[Donor] ([DonorID], [AccountID], [BloodTypeID], [Name], [Gender], [DateOfBirth], [ContactNumber], [Address], [IsAvailable], [CCCD]) VALUES (1, 4, 3, N'Nguyễn Văn A', N'M', CAST(N'1985-01-01' AS Date), N'0912345678', N'26 Lý Tự Trọng, Bến Nghé, Quận 1, Hồ Chí Minh 700000, Việt Nam', 1, N'123456789012')
INSERT [dbo].[Donor] ([DonorID], [AccountID], [BloodTypeID], [Name], [Gender], [DateOfBirth], [ContactNumber], [Address], [IsAvailable], [CCCD]) VALUES (2, 5, 1, N'Trần Thị B', N'F', CAST(N'1990-02-02' AS Date), N'0987654321', N'130 Đường Lê Văn Thịnh, Phường Bình Trưng Tây, Thủ Đức, Hồ Chí Minh, Việt Nam', 1, N'234567890123')
INSERT [dbo].[Donor] ([DonorID], [AccountID], [BloodTypeID], [Name], [Gender], [DateOfBirth], [ContactNumber], [Address], [IsAvailable], [CCCD]) VALUES (3, 6, 4, N'Phạm Minh C', N'M', CAST(N'1987-03-03' AS Date), N'0912348765', N'Lưu Hữu Phước Tân Lập, Đông Hoà, Dĩ An, Bình Dương, Việt Nam', 1, N'345678901234')
INSERT [dbo].[Donor] ([DonorID], [AccountID], [BloodTypeID], [Name], [Gender], [DateOfBirth], [ContactNumber], [Address], [IsAvailable], [CCCD]) VALUES (4, 7, 7, N'Nguyễn Thị D', N'F', CAST(N'1992-04-04' AS Date), N'0934567890', N'37/1 Đặng Văn Bi, Phường Linh Tây, Thủ Đức', 1, N'456789012345')
INSERT [dbo].[Donor] ([DonorID], [AccountID], [BloodTypeID], [Name], [Gender], [DateOfBirth], [ContactNumber], [Address], [IsAvailable], [CCCD]) VALUES (5, 8, 8, N'Lê Văn E', N'M', CAST(N'1993-05-05' AS Date), N'0913456789', N'475A Điện Biên Phủ, Phường 25, Bình Thạnh, Hồ Chí Minh, Việt Nam', 1, N'567890123456')
SET IDENTITY_INSERT [dbo].[Donor] OFF
GO
SET IDENTITY_INSERT [dbo].[DonorBloodRequest] ON 

INSERT [dbo].[DonorBloodRequest] ([DonorBloodRequestID], [BloodRequestID], [DonorID], [DonationDate], [QuantityDonated]) VALUES (1, 1, 1, CAST(N'2025-06-13' AS Date), CAST(1.50 AS Decimal(10, 2)))
SET IDENTITY_INSERT [dbo].[DonorBloodRequest] OFF
GO
SET IDENTITY_INSERT [dbo].[HealthSurvey] ON 

INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (1, 1, N'1_AnhChiDaTungHienMauChua', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (2, 1, N'2_HienTaiAnhChiCoMacBenhLyKhong', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (3, 1, N'3_TruocDayAnhChiCoMacCacBenhLietKeKhong', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (4, 1, N'4_KhoiBenhSauMacCacBenh12Thang', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (5, 1, N'4_DuocTruyenMauHoacGayGhepMo', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (6, 1, N'4_TiemVaccine', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (7, 1, N'5_KhoiBenhSauMacCacBenh6Thang', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (8, 1, N'6_KhoiBenhSauMacCacBenh1Thang', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (9, 1, N'7_BiCumCamLanhHoNhucDauSotDauHong14Ngay', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (10, 1, N'8_DungThuocKhangSinhKhangViêmAspirinCorticoide7Ngay', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (11, 1, N'9_HienChiDangMangThaiHoacCoThai12ThangTruoc', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (12, 1, N'9_ChamDutThaiKy12ThangGanDay', N'0')
INSERT [dbo].[HealthSurvey] ([SurveyID], [AppointmentID], [QuestionCode], [Answer]) VALUES (13, 1, N'10_AnhChiSanSangHienMauNeuDuDieuKien', N'1')
SET IDENTITY_INSERT [dbo].[HealthSurvey] OFF
GO
SET IDENTITY_INSERT [dbo].[MedicalCenter] ON 

INSERT [dbo].[MedicalCenter] ([MedicalCenterID], [Name], [Location], [ContactNumber], [BloodBankID]) VALUES (1, N'Cơ sở y tế Linh Đông', N'79 Đình Phong Phú, Tăng Nhơn Phú B, Thủ Đức, Hồ Chí Minh, Việt Nam', N'02438693731', 1)
INSERT [dbo].[MedicalCenter] ([MedicalCenterID], [Name], [Location], [ContactNumber], [BloodBankID]) VALUES (2, N'Cơ sở y tế Tam Bình', N'14 Einstein, Bình Thọ, Thủ Đức, Hồ Chí Minh, Việt Nam', N'02838554137', 1)
INSERT [dbo].[MedicalCenter] ([MedicalCenterID], [Name], [Location], [ContactNumber], [BloodBankID]) VALUES (3, N'Cơ sở y tế Hiệp Bình Chánh', N'12 Đặng Văn Bi, Trường Thọ, Thủ Đức, Hồ Chí Minh 70000, Việt Nam', N'02343823473', 1)
SET IDENTITY_INSERT [dbo].[MedicalCenter] OFF
GO
SET IDENTITY_INSERT [dbo].[Notifications] ON 

INSERT [dbo].[Notifications] ([NotificationID], [DonorID], [Message], [SentAt], [IsRead], [Type], [IsConfirmed], [AccountID], [BloodRequestID]) VALUES (32, 5, N'Bạn được mời tham gia hiến máu cho một trường hợp cần thiết. Vui lòng xác nhận nếu bạn đồng ý.', CAST(N'2025-07-02T00:21:54.997' AS DateTime), 1, N'Invite', 1, 8, 1)
INSERT [dbo].[Notifications] ([NotificationID], [DonorID], [Message], [SentAt], [IsRead], [Type], [IsConfirmed], [AccountID], [BloodRequestID]) VALUES (33, 5, N'Donor Lê Văn E (ID: 5, Nhóm máu: O-) đã xác nhận đi hiến máu.', CAST(N'2025-07-02T00:22:25.607' AS DateTime), 1, N'DonorConfirmed', 1, 10, 1)
INSERT [dbo].[Notifications] ([NotificationID], [DonorID], [Message], [SentAt], [IsRead], [Type], [IsConfirmed], [AccountID], [BloodRequestID]) VALUES (34, 5, N'Donor Lê Văn E (ID: 5, Nhóm máu: O-) đã xác nhận đi hiến máu.', CAST(N'2025-07-02T00:22:25.623' AS DateTime), 1, N'DonorConfirmed', 1, 1, 1)
SET IDENTITY_INSERT [dbo].[Notifications] OFF
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Account__536C85E424236224]    Script Date: 7/2/2025 8:23:51 AM ******/
ALTER TABLE [dbo].[Account] ADD UNIQUE NONCLUSTERED 
(
	[Username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__BloodTyp__F9B8A48B47366E62]    Script Date: 7/2/2025 8:23:51 AM ******/
ALTER TABLE [dbo].[BloodType] ADD UNIQUE NONCLUSTERED 
(
	[Type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ__Donation__8ECDFCA326E5E6CA]    Script Date: 7/2/2025 8:23:51 AM ******/
ALTER TABLE [dbo].[DonationCertificate] ADD UNIQUE NONCLUSTERED 
(
	[AppointmentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ__Donor__349DA587DC4F1C59]    Script Date: 7/2/2025 8:23:51 AM ******/
ALTER TABLE [dbo].[Donor] ADD UNIQUE NONCLUSTERED 
(
	[AccountID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ__DonorBlo__A42B2A2068F140F5]    Script Date: 7/2/2025 8:23:51 AM ******/
ALTER TABLE [dbo].[DonorBloodRequest] ADD UNIQUE NONCLUSTERED 
(
	[BloodRequestID] ASC,
	[DonorID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[BloodRequest] ADD  DEFAULT ('Pending') FOR [Status]
GO
ALTER TABLE [dbo].[Notifications] ADD  DEFAULT (getdate()) FOR [SentAt]
GO
ALTER TABLE [dbo].[Notifications] ADD  DEFAULT ((0)) FOR [IsRead]
GO
ALTER TABLE [dbo].[Notifications] ADD  DEFAULT ((0)) FOR [IsConfirmed]
GO
ALTER TABLE [dbo].[Account]  WITH CHECK ADD FOREIGN KEY([MedicalCenterID])
REFERENCES [dbo].[MedicalCenter] ([MedicalCenterID])
GO
ALTER TABLE [dbo].[BloodInventory]  WITH CHECK ADD FOREIGN KEY([BloodTypeID])
REFERENCES [dbo].[BloodType] ([BloodTypeID])
GO
ALTER TABLE [dbo].[BloodInventory]  WITH CHECK ADD FOREIGN KEY([BloodBankID])
REFERENCES [dbo].[BloodBank] ([BloodBankID])
GO
ALTER TABLE [dbo].[BloodRequest]  WITH CHECK ADD FOREIGN KEY([BloodTypeID])
REFERENCES [dbo].[BloodType] ([BloodTypeID])
GO
ALTER TABLE [dbo].[BloodRequest]  WITH CHECK ADD FOREIGN KEY([MedicalCenterID])
REFERENCES [dbo].[MedicalCenter] ([MedicalCenterID])
GO
ALTER TABLE [dbo].[DonationAppointment]  WITH CHECK ADD FOREIGN KEY([BloodTypeID])
REFERENCES [dbo].[BloodType] ([BloodTypeID])
GO
ALTER TABLE [dbo].[DonationAppointment]  WITH CHECK ADD FOREIGN KEY([DonorID])
REFERENCES [dbo].[Donor] ([DonorID])
GO
ALTER TABLE [dbo].[DonationAppointment]  WITH CHECK ADD FOREIGN KEY([MedicalCenterID])
REFERENCES [dbo].[MedicalCenter] ([MedicalCenterID])
GO
ALTER TABLE [dbo].[DonationCertificate]  WITH CHECK ADD FOREIGN KEY([AppointmentID])
REFERENCES [dbo].[DonationAppointment] ([AppointmentID])
GO
ALTER TABLE [dbo].[Donor]  WITH CHECK ADD FOREIGN KEY([AccountID])
REFERENCES [dbo].[Account] ([AccountID])
GO
ALTER TABLE [dbo].[Donor]  WITH CHECK ADD FOREIGN KEY([BloodTypeID])
REFERENCES [dbo].[BloodType] ([BloodTypeID])
GO
ALTER TABLE [dbo].[DonorBloodRequest]  WITH CHECK ADD FOREIGN KEY([BloodRequestID])
REFERENCES [dbo].[BloodRequest] ([BloodRequestID])
GO
ALTER TABLE [dbo].[DonorBloodRequest]  WITH CHECK ADD FOREIGN KEY([DonorID])
REFERENCES [dbo].[Donor] ([DonorID])
GO
ALTER TABLE [dbo].[HealthSurvey]  WITH CHECK ADD FOREIGN KEY([AppointmentID])
REFERENCES [dbo].[DonationAppointment] ([AppointmentID])
GO
ALTER TABLE [dbo].[MedicalCenter]  WITH CHECK ADD FOREIGN KEY([BloodBankID])
REFERENCES [dbo].[BloodBank] ([BloodBankID])
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD FOREIGN KEY([DonorID])
REFERENCES [dbo].[Donor] ([DonorID])
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_BloodRequest] FOREIGN KEY([BloodRequestID])
REFERENCES [dbo].[BloodRequest] ([BloodRequestID])
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [FK_Notifications_BloodRequest]
GO
ALTER TABLE [dbo].[Account]  WITH CHECK ADD  CONSTRAINT [CHK_PermissionLevel] CHECK  ((([Role]='Donor' OR [Role]='MedicalCenter') AND [PermissionLevel]=(1) OR [Role]='Staff' AND [PermissionLevel]=(2) OR [Role]='Admin' AND [PermissionLevel]=(3)))
GO
ALTER TABLE [dbo].[Account] CHECK CONSTRAINT [CHK_PermissionLevel]
GO
ALTER TABLE [dbo].[Account]  WITH CHECK ADD  CONSTRAINT [CHK_Role] CHECK  (([Role]='Staff' OR [Role]='Admin' OR [Role]='Donor' OR [Role]='MedicalCenter'))
GO
ALTER TABLE [dbo].[Account] CHECK CONSTRAINT [CHK_Role]
GO
ALTER TABLE [dbo].[BloodRequest]  WITH CHECK ADD CHECK  (([Status]='Approved' OR [Status]='Canceled' OR [Status]='Rejected' OR [Status]='Completed' OR [Status]='Pending'))
GO
ALTER TABLE [dbo].[DonationAppointment]  WITH CHECK ADD CHECK  (([TimeSlot]=N'Chiều' OR [TimeSlot]=N'Sáng'))
GO
USE [master]
GO
ALTER DATABASE [BloodDonation] SET  READ_WRITE 
GO
