USE [master]
GO
/****** Object:  Database [home]    Script Date: 26.01.2024 20:25:37 ******/
CREATE DATABASE [home]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'home', FILENAME = N'/var/opt/mssql/data/home.mdf' , SIZE = 73728KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'home_log', FILENAME = N'/var/opt/mssql/data/home_log.ldf' , SIZE = 73728KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [home] SET COMPATIBILITY_LEVEL = 150
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [home].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [home] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [home] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [home] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [home] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [home] SET ARITHABORT OFF 
GO
ALTER DATABASE [home] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [home] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [home] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [home] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [home] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [home] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [home] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [home] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [home] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [home] SET  DISABLE_BROKER 
GO
ALTER DATABASE [home] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [home] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [home] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [home] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [home] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [home] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [home] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [home] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [home] SET  MULTI_USER 
GO
ALTER DATABASE [home] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [home] SET DB_CHAINING OFF 
GO
ALTER DATABASE [home] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [home] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [home] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [home] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'home', N'ON'
GO
ALTER DATABASE [home] SET QUERY_STORE = OFF
GO
USE [home]
GO
/****** Object:  User [Privat]    Script Date: 26.01.2024 20:25:37 ******/
CREATE USER [Privat] FOR LOGIN [Privat] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [Privat]
GO
ALTER ROLE [db_accessadmin] ADD MEMBER [Privat]
GO
/****** Object:  Table [dbo].[Device]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Device](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GUID] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[IP] [nvarchar](50) NULL,
	[MacAddress] [varchar](50) NULL,
	[LastSeen] [datetime] NOT NULL,
	[Status] [bit] NOT NULL,
	[DeviceTypeId] [int] NOT NULL,
	[OSType] [int] NOT NULL,
	[DeviceGroup] [nvarchar](50) NULL,
	[Location] [nvarchar](50) NULL,
	[IsLive] [bit] NULL,
	[EnvironmentId] [int] NOT NULL,
	[ServiceClientVersion] [nvarchar](50) NOT NULL,
	[IsScreenshotRequired] [bit] NOT NULL,
	[DeviceUsageId] [int] NULL,
 CONSTRAINT [PK_Device_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceBattery]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceBattery](
	[BatteryID] [int] IDENTITY(1,1) NOT NULL,
	[IsCharging] [bit] NOT NULL,
	[Percentage] [float] NULL,
 CONSTRAINT [PK_DeviceBattery] PRIMARY KEY CLUSTERED 
(
	[BatteryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceBIOS]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceBIOS](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[DeviceID] [int] NOT NULL,
	[Vendor] [nvarchar](255) NOT NULL,
	[Description] [nvarchar](255) NULL,
	[Version] [nvarchar](255) NULL,
	[ReleaseDate] [datetime] NULL,
 CONSTRAINT [PK_DeviceBIOS] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceChange]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceChange](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[DeviceID] [int] NOT NULL,
	[Timestamp] [datetime] NOT NULL,
	[Type] [int] NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_DeviceChange] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceCommand]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceCommand](
	[DeviceCommandId] [int] IDENTITY(1,1) NOT NULL,
	[DeviceId] [int] NULL,
	[Executable] [text] NULL,
	[Parameter] [text] NULL,
	[Timestamp] [datetime] NOT NULL,
	[IsExceuted] [bit] NOT NULL,
 CONSTRAINT [PK_DeviceCommand] PRIMARY KEY CLUSTERED 
(
	[DeviceCommandId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceDiskDrive]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceDiskDrive](
	[DiskDriveId] [int] IDENTITY(1,1) NOT NULL,
	[DeviceID] [int] NULL,
	[GUID] [varchar](50) NULL,
	[PhysicalName] [text] NULL,
	[DiskName] [text] NULL,
	[DiskModel] [text] NULL,
	[DiskInterface] [text] NULL,
	[MediaType] [text] NULL,
	[MediaSignature] [bigint] NULL,
	[DriveName] [text] NULL,
	[DriveID] [text] NULL,
	[DriveCompressed] [bit] NOT NULL,
	[DriveType] [int] NULL,
	[FileSystem] [text] NULL,
	[TotalSpace] [bigint] NULL,
	[FreeSpace] [bigint] NULL,
	[DriveMediaType] [int] NULL,
	[VolumeName] [text] NULL,
	[VolumeSerial] [text] NULL,
	[MediaLoaded] [bit] NULL,
	[MediaStatus] [text] NULL,
 CONSTRAINT [PK_DiskDriveId] PRIMARY KEY CLUSTERED 
(
	[DiskDriveId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceEnvironment]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceEnvironment](
	[DeviceID] [int] IDENTITY(1,1) NOT NULL,
	[Product] [text] NULL,
	[Description] [text] NULL,
	[Vendor] [text] NULL,
	[OSName] [text] NULL,
	[OSVersion] [text] NULL,
	[CPUName] [text] NULL,
	[CPUCount] [smallint] NULL,
	[CPUUsage] [float] NULL,
	[Motherboard] [text] NULL,
	[TotalRAM] [float] NULL,
	[AvailableRAM] [float] NULL,
	[DiskUsage] [float] NULL,
	[Is64BitOS] [bit] NOT NULL,
	[MachineName] [text] NOT NULL,
	[UserName] [varchar](50) NULL,
	[DomainName] [varchar](50) NULL,
	[RunningTime] [bigint] NULL,
	[StartTimestamp] [datetime] NULL,
	[BatteryID] [int] NULL,
 CONSTRAINT [PK_DeviceEnvironment] PRIMARY KEY CLUSTERED 
(
	[DeviceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceGraphic]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceGraphic](
	[DeviceGraphicsID] [int] IDENTITY(1,1) NOT NULL,
	[DeviceID] [int] NOT NULL,
	[Name] [text] NOT NULL,
 CONSTRAINT [PK_DeviceGraphicsID] PRIMARY KEY CLUSTERED 
(
	[DeviceGraphicsID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceLog]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceLog](
	[LogEntryID] [int] IDENTITY(1,1) NOT NULL,
	[DeviceID] [int] NULL,
	[Blob] [text] NOT NULL,
	[Timestamp] [datetime] NULL,
	[LogLevel] [int] NOT NULL,
 CONSTRAINT [PK_DeviceLog] PRIMARY KEY CLUSTERED 
(
	[LogEntryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceMessage]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceMessage](
	[MessageId] [int] IDENTITY(1,1) NOT NULL,
	[DeviceId] [int] NULL,
	[Title] [text] NULL,
	[Content] [text] NULL,
	[Type] [smallint] NOT NULL,
	[Timestamp] [datetime] NULL,
	[IsRecieved] [bit] NOT NULL,
 CONSTRAINT [PK_DeviceMessage] PRIMARY KEY CLUSTERED 
(
	[MessageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceOSType]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceOSType](
	[OSTypeID] [int] NOT NULL,
	[Name] [varchar](max) NOT NULL,
	[Description] [varchar](max) NOT NULL,
 CONSTRAINT [PK_OSTypes] PRIMARY KEY CLUSTERED 
(
	[OSTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceScreen]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceScreen](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ScreenID] [text] NOT NULL,
	[DeviceID] [int] NOT NULL,
	[Manufacturer] [text] NULL,
	[Serial] [text] NULL,
	[BuiltDate] [text] NULL,
	[ScreenIndex] [int] NOT NULL,
	[IsPrimary] [bit] NOT NULL,
	[DeviceName] [text] NULL,
	[Resolution] [text] NOT NULL,
 CONSTRAINT [PK_DeviceScreen] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceScreenshot]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceScreenshot](
	[ScreenshotId] [int] IDENTITY(1,1) NOT NULL,
	[DeviceId] [int] NULL,
	[ScreenshotFileName] [varchar](260) NOT NULL,
	[Timestamp] [datetime] NOT NULL,
	[ScreenId] [int] NULL,
 CONSTRAINT [PK_DeviceScreenshot] PRIMARY KEY CLUSTERED 
(
	[ScreenshotId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceType]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceType](
	[TypeID] [int] NOT NULL,
	[Type] [varchar](max) NOT NULL,
 CONSTRAINT [PK_DeviceTypes] PRIMARY KEY CLUSTERED 
(
	[TypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceUsage]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceUsage](
	[DeviceUsageId] [int] IDENTITY(1,1) NOT NULL,
	[CPU] [text] NULL,
	[RAM] [text] NULL,
	[DISK] [text] NULL,
	[Battery] [text] NULL,
 CONSTRAINT [PK_DeviceUsage] PRIMARY KEY CLUSTERED 
(
	[DeviceUsageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceWarning]    Script Date: 26.01.2024 20:25:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceWarning](
	[WarningId] [int] IDENTITY(1,1) NOT NULL,
	[DeviceId] [int] NULL,
	[WarningType] [smallint] NOT NULL,
	[CriticalValue] [bigint] NOT NULL,
	[AdditionalInfo] [text] NULL,
	[Timestamp] [datetime] NOT NULL,
 CONSTRAINT [PK_DeviceWarning] PRIMARY KEY CLUSTERED 
(
	[WarningId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Index [IX_Device]    Script Date: 26.01.2024 20:25:37 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Device] ON [dbo].[Device]
(
	[EnvironmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_DeviceDiskDrive]    Script Date: 26.01.2024 20:25:37 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_DeviceDiskDrive] ON [dbo].[DeviceDiskDrive]
(
	[GUID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DeviceTypes]    Script Date: 26.01.2024 20:25:37 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_DeviceTypes] ON [dbo].[DeviceType]
(
	[TypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Device] ADD  CONSTRAINT [DF_Device_Status_1]  DEFAULT ((0)) FOR [Status]
GO
ALTER TABLE [dbo].[Device] ADD  CONSTRAINT [DF_Device_IsScreenshotRequired]  DEFAULT ((0)) FOR [IsScreenshotRequired]
GO
ALTER TABLE [dbo].[DeviceBattery] ADD  CONSTRAINT [DF_DeviceBattery_IsCharging]  DEFAULT ((0)) FOR [IsCharging]
GO
ALTER TABLE [dbo].[DeviceCommand] ADD  CONSTRAINT [DF_DeviceCommand_IsExceuted]  DEFAULT ((0)) FOR [IsExceuted]
GO
ALTER TABLE [dbo].[DeviceEnvironment] ADD  CONSTRAINT [DF_DeviceEnvironment_CPUCount]  DEFAULT ((1)) FOR [CPUCount]
GO
ALTER TABLE [dbo].[DeviceMessage] ADD  CONSTRAINT [DF_DeviceMessage_IsRecieved]  DEFAULT ((0)) FOR [IsRecieved]
GO
ALTER TABLE [dbo].[Device]  WITH CHECK ADD  CONSTRAINT [FK_Device_DeviceUsage] FOREIGN KEY([DeviceUsageId])
REFERENCES [dbo].[DeviceUsage] ([DeviceUsageId])
GO
ALTER TABLE [dbo].[Device] CHECK CONSTRAINT [FK_Device_DeviceUsage]
GO
ALTER TABLE [dbo].[Device]  WITH CHECK ADD  CONSTRAINT [FK_DeviceTypes] FOREIGN KEY([DeviceTypeId])
REFERENCES [dbo].[DeviceType] ([TypeID])
GO
ALTER TABLE [dbo].[Device] CHECK CONSTRAINT [FK_DeviceTypes]
GO
ALTER TABLE [dbo].[Device]  WITH CHECK ADD  CONSTRAINT [FK_Environment] FOREIGN KEY([EnvironmentId])
REFERENCES [dbo].[DeviceEnvironment] ([DeviceID])
GO
ALTER TABLE [dbo].[Device] CHECK CONSTRAINT [FK_Environment]
GO
ALTER TABLE [dbo].[Device]  WITH CHECK ADD  CONSTRAINT [FK_OSTypes] FOREIGN KEY([OSType])
REFERENCES [dbo].[DeviceOSType] ([OSTypeID])
GO
ALTER TABLE [dbo].[Device] CHECK CONSTRAINT [FK_OSTypes]
GO
ALTER TABLE [dbo].[DeviceBIOS]  WITH CHECK ADD  CONSTRAINT [FK_BIOS_Device] FOREIGN KEY([DeviceID])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceBIOS] CHECK CONSTRAINT [FK_BIOS_Device]
GO
ALTER TABLE [dbo].[DeviceChange]  WITH CHECK ADD  CONSTRAINT [FK_DeviceChange_Device] FOREIGN KEY([DeviceID])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceChange] CHECK CONSTRAINT [FK_DeviceChange_Device]
GO
ALTER TABLE [dbo].[DeviceCommand]  WITH CHECK ADD  CONSTRAINT [FK_DeviceCommand_Device] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceCommand] CHECK CONSTRAINT [FK_DeviceCommand_Device]
GO
ALTER TABLE [dbo].[DeviceDiskDrive]  WITH CHECK ADD  CONSTRAINT [FK_DeviceDiskDrives_Device] FOREIGN KEY([DeviceID])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceDiskDrive] CHECK CONSTRAINT [FK_DeviceDiskDrives_Device]
GO
ALTER TABLE [dbo].[DeviceEnvironment]  WITH CHECK ADD  CONSTRAINT [FK_DeviceEnvironment_DeviceBattery] FOREIGN KEY([BatteryID])
REFERENCES [dbo].[DeviceBattery] ([BatteryID])
GO
ALTER TABLE [dbo].[DeviceEnvironment] CHECK CONSTRAINT [FK_DeviceEnvironment_DeviceBattery]
GO
ALTER TABLE [dbo].[DeviceGraphic]  WITH CHECK ADD  CONSTRAINT [FK_DeviceGraphics] FOREIGN KEY([DeviceID])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceGraphic] CHECK CONSTRAINT [FK_DeviceGraphics]
GO
ALTER TABLE [dbo].[DeviceLog]  WITH CHECK ADD  CONSTRAINT [FK_DeviceID] FOREIGN KEY([DeviceID])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceLog] CHECK CONSTRAINT [FK_DeviceID]
GO
ALTER TABLE [dbo].[DeviceMessage]  WITH CHECK ADD  CONSTRAINT [FK_DeviceMessage_Device] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceMessage] CHECK CONSTRAINT [FK_DeviceMessage_Device]
GO
ALTER TABLE [dbo].[DeviceScreen]  WITH CHECK ADD  CONSTRAINT [FK_DeviceScreen_Device] FOREIGN KEY([DeviceID])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceScreen] CHECK CONSTRAINT [FK_DeviceScreen_Device]
GO
ALTER TABLE [dbo].[DeviceScreenshot]  WITH CHECK ADD  CONSTRAINT [FK_DeviceScreenshot_Device] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceScreenshot] CHECK CONSTRAINT [FK_DeviceScreenshot_Device]
GO
ALTER TABLE [dbo].[DeviceScreenshot]  WITH CHECK ADD  CONSTRAINT [FK_DeviceScreenshot_DeviceScreen] FOREIGN KEY([ScreenId])
REFERENCES [dbo].[DeviceScreen] ([ID])
GO
ALTER TABLE [dbo].[DeviceScreenshot] CHECK CONSTRAINT [FK_DeviceScreenshot_DeviceScreen]
GO
ALTER TABLE [dbo].[DeviceUsage]  WITH CHECK ADD  CONSTRAINT [FK_DeviceUsage_DeviceUsage] FOREIGN KEY([DeviceUsageId])
REFERENCES [dbo].[DeviceUsage] ([DeviceUsageId])
GO
ALTER TABLE [dbo].[DeviceUsage] CHECK CONSTRAINT [FK_DeviceUsage_DeviceUsage]
GO
ALTER TABLE [dbo].[DeviceWarning]  WITH CHECK ADD  CONSTRAINT [FK_DeviceWarning_Device] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceWarning] CHECK CONSTRAINT [FK_DeviceWarning_Device]
GO
USE [master]
GO
ALTER DATABASE [home] SET  READ_WRITE 
GO
