USE [home]
GO
/****** Object:  Table [dbo].[Device]    Script Date: 22.10.2022 18:12:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Device](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GUID] [varchar](50) NOT NULL,
	[Name] [varchar](max) NOT NULL,
	[IP] [nvarchar](50) NULL,
	[LastSeen] [datetime] NOT NULL,
	[Status] [bit] NOT NULL,
	[DeviceTypeId] [int] NOT NULL,
	[OSType] [int] NOT NULL,
	[DeviceGroup] [varchar](50) NULL,
	[Location] [varchar](50) NULL,
	[IsLive] [bit] NULL,
	[EnvironmentId] [int] NOT NULL,
	[ServiceClientVersion] [varchar](50) NOT NULL,
	[IsScreenshotRequired] [bit] NOT NULL,
	[DeviceUsageId] [int] NULL,
 CONSTRAINT [PK_Device_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceBattery]    Script Date: 22.10.2022 18:12:58 ******/
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
/****** Object:  Table [dbo].[DeviceCommand]    Script Date: 22.10.2022 18:12:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceCommand](
	[DeviceCommandId] [int] IDENTITY(1,1) NOT NULL,
	[DeviceId] [int] NULL,
	[Executable] [text] NULL,
	[Paramter] [text] NULL,
	[Timestamp] [datetime] NOT NULL,
	[IsExceuted] [bit] NOT NULL,
 CONSTRAINT [PK_DeviceCommand] PRIMARY KEY CLUSTERED 
(
	[DeviceCommandId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceDiskDrive]    Script Date: 22.10.2022 18:12:58 ******/
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
	[MediaSignature] [int] NULL,
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
/****** Object:  Table [dbo].[DeviceEnvironment]    Script Date: 22.10.2022 18:12:58 ******/
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
	[FreeRAM] [text] NULL,
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
/****** Object:  Table [dbo].[DeviceGraphic]    Script Date: 22.10.2022 18:12:58 ******/
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
/****** Object:  Table [dbo].[DeviceLog]    Script Date: 22.10.2022 18:12:58 ******/
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
/****** Object:  Table [dbo].[DeviceMessage]    Script Date: 22.10.2022 18:12:58 ******/
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
/****** Object:  Table [dbo].[DeviceOSType]    Script Date: 22.10.2022 18:12:58 ******/
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
/****** Object:  Table [dbo].[DeviceScreenshot]    Script Date: 22.10.2022 18:12:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceScreenshot](
	[ScreenshotId] [int] IDENTITY(1,1) NOT NULL,
	[DeviceId] [int] NULL,
	[ScreenshotFileName] [varchar](260) NOT NULL,
	[Timestamp] [datetime] NOT NULL,
 CONSTRAINT [PK_DeviceScreenshot] PRIMARY KEY CLUSTERED 
(
	[ScreenshotId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceType]    Script Date: 22.10.2022 18:12:58 ******/
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
/****** Object:  Table [dbo].[DeviceUsage]    Script Date: 22.10.2022 18:12:58 ******/
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
/****** Object:  Table [dbo].[DeviceWarning]    Script Date: 22.10.2022 18:12:58 ******/
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
ALTER TABLE [dbo].[DeviceScreenshot]  WITH CHECK ADD  CONSTRAINT [FK_DeviceScreenshot_Device] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Device] ([Id])
GO
ALTER TABLE [dbo].[DeviceScreenshot] CHECK CONSTRAINT [FK_DeviceScreenshot_Device]
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
