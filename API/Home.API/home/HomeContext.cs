﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Home.API.home.Models;
using Microsoft.EntityFrameworkCore;

namespace Home.API.home;

public partial class HomeContext : DbContext
{
    public HomeContext(DbContextOptions<HomeContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Device> Device { get; set; }

    public virtual DbSet<DeviceBattery> DeviceBattery { get; set; }

    public virtual DbSet<DeviceBios> DeviceBios { get; set; }

    public virtual DbSet<DeviceChange> DeviceChange { get; set; }

    public virtual DbSet<DeviceCommand> DeviceCommand { get; set; }

    public virtual DbSet<DeviceDiskDrive> DeviceDiskDrive { get; set; }

    public virtual DbSet<DeviceEnvironment> DeviceEnvironment { get; set; }

    public virtual DbSet<DeviceGraphic> DeviceGraphic { get; set; }

    public virtual DbSet<DeviceLog> DeviceLog { get; set; }

    public virtual DbSet<DeviceMessage> DeviceMessage { get; set; }

    public virtual DbSet<DeviceOstype> DeviceOstype { get; set; }

    public virtual DbSet<DeviceScreen> DeviceScreen { get; set; }

    public virtual DbSet<DeviceScreenshot> DeviceScreenshot { get; set; }

    public virtual DbSet<DeviceType> DeviceType { get; set; }

    public virtual DbSet<DeviceUsage> DeviceUsage { get; set; }

    public virtual DbSet<DeviceWarning> DeviceWarning { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Device_1");

            entity.HasIndex(e => e.EnvironmentId, "IX_Device").IsUnique();

            entity.Property(e => e.DeviceGroup)
                .HasMaxLength(50)
                .UseCollation("Latin1_General_100_CI_AS_SC_UTF8");
            entity.Property(e => e.Guid)
                .IsRequired()
                .HasMaxLength(50)
                .UseCollation("Latin1_General_100_CI_AS_SC_UTF8")
                .HasColumnName("GUID");
            entity.Property(e => e.Ip)
                .HasMaxLength(50)
                .UseCollation("Latin1_General_100_CI_AS_SC_UTF8")
                .HasColumnName("IP");
            entity.Property(e => e.LastSeen).HasColumnType("datetime");
            entity.Property(e => e.Location)
                .HasMaxLength(50)
                .UseCollation("Latin1_General_100_CI_AS_SC_UTF8");
            entity.Property(e => e.MacAddress)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255)
                .UseCollation("Latin1_General_100_CI_AS_SC_UTF8");
            entity.Property(e => e.Ostype).HasColumnName("OSType");
            entity.Property(e => e.ServiceClientVersion)
                .IsRequired()
                .HasMaxLength(50)
                .UseCollation("Latin1_General_100_CI_AS_SC_UTF8");

            entity.HasOne(d => d.DeviceType).WithMany(p => p.Device)
                .HasForeignKey(d => d.DeviceTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceTypes");

            entity.HasOne(d => d.DeviceUsage).WithMany(p => p.Device)
                .HasForeignKey(d => d.DeviceUsageId)
                .HasConstraintName("FK_Device_DeviceUsage");

            entity.HasOne(d => d.Environment).WithOne(p => p.Device)
                .HasForeignKey<Device>(d => d.EnvironmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Environment");

            entity.HasOne(d => d.OstypeNavigation).WithMany(p => p.Device)
                .HasForeignKey(d => d.Ostype)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OSTypes");
        });

        modelBuilder.Entity<DeviceBattery>(entity =>
        {
            entity.HasKey(e => e.BatteryId);

            entity.Property(e => e.BatteryId).HasColumnName("BatteryID");
        });

        modelBuilder.Entity<DeviceBios>(entity =>
        {
            entity.ToTable("DeviceBIOS");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.ReleaseDate).HasColumnType("datetime");
            entity.Property(e => e.Vendor)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Version).HasMaxLength(255);

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceBios)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BIOS_Device");
        });

        modelBuilder.Entity<DeviceChange>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.Timestamp).HasColumnType("datetime");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceChange)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceChange_Device");
        });

        modelBuilder.Entity<DeviceCommand>(entity =>
        {
            entity.Property(e => e.Executable)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.Parameter)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.Timestamp).HasColumnType("datetime");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceCommand)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("FK_DeviceCommand_Device");
        });

        modelBuilder.Entity<DeviceDiskDrive>(entity =>
        {
            entity.HasKey(e => e.DiskDriveId).HasName("PK_DiskDriveId");

            entity.HasIndex(e => e.Guid, "IX_DeviceDiskDrive").IsUnique();

            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.DiskInterface)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.DiskModel)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.DiskName)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.DriveId)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text")
                .HasColumnName("DriveID");
            entity.Property(e => e.DriveName)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.FileSystem)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.Guid)
                .HasMaxLength(50)
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnName("GUID");
            entity.Property(e => e.MediaStatus)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.MediaType)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.PhysicalName)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.VolumeName)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.VolumeSerial)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceDiskDrive)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("FK_DeviceDiskDrives_Device");
        });

        modelBuilder.Entity<DeviceEnvironment>(entity =>
        {
            entity.HasKey(e => e.DeviceId);

            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.AvailableRam).HasColumnName("AvailableRAM");
            entity.Property(e => e.BatteryId).HasColumnName("BatteryID");
            entity.Property(e => e.Cpucount)
                .HasDefaultValue((short)1)
                .HasColumnName("CPUCount");
            entity.Property(e => e.Cpuname)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text")
                .HasColumnName("CPUName");
            entity.Property(e => e.Cpuusage).HasColumnName("CPUUsage");
            entity.Property(e => e.Description)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.DomainName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AS");
            entity.Property(e => e.Is64BitOs).HasColumnName("Is64BitOS");
            entity.Property(e => e.MachineName)
                .IsRequired()
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.Motherboard)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.Osname)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text")
                .HasColumnName("OSName");
            entity.Property(e => e.Osversion)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text")
                .HasColumnName("OSVersion");
            entity.Property(e => e.Product)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.StartTimestamp).HasColumnType("datetime");
            entity.Property(e => e.TotalRam).HasColumnName("TotalRAM");
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AS");
            entity.Property(e => e.Vendor)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");

            entity.HasOne(d => d.Battery).WithMany(p => p.DeviceEnvironment)
                .HasForeignKey(d => d.BatteryId)
                .HasConstraintName("FK_DeviceEnvironment_DeviceBattery");
        });

        modelBuilder.Entity<DeviceGraphic>(entity =>
        {
            entity.HasKey(e => e.DeviceGraphicsId).HasName("PK_DeviceGraphicsID");

            entity.Property(e => e.DeviceGraphicsId).HasColumnName("DeviceGraphicsID");
            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.Name)
                .IsRequired()
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceGraphic)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceGraphics");
        });

        modelBuilder.Entity<DeviceLog>(entity =>
        {
            entity.HasKey(e => e.LogEntryId);

            entity.Property(e => e.LogEntryId).HasColumnName("LogEntryID");
            entity.Property(e => e.Blob)
                .IsRequired()
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.Timestamp).HasColumnType("datetime");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceLog)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("FK_DeviceID");
        });

        modelBuilder.Entity<DeviceMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId);

            entity.Property(e => e.Content)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.Timestamp).HasColumnType("datetime");
            entity.Property(e => e.Title)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceMessage)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("FK_DeviceMessage_Device");
        });

        modelBuilder.Entity<DeviceOstype>(entity =>
        {
            entity.HasKey(e => e.OstypeId).HasName("PK_OSTypes");

            entity.ToTable("DeviceOSType");

            entity.Property(e => e.OstypeId)
                .ValueGeneratedNever()
                .HasColumnName("OSTypeID");
            entity.Property(e => e.Description)
                .IsRequired()
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AS");
            entity.Property(e => e.Name)
                .IsRequired()
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AS");
        });

        modelBuilder.Entity<DeviceScreen>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BuiltDate)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.DeviceName)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.Manufacturer)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.Resolution)
                .IsRequired()
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.ScreenId)
                .IsRequired()
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text")
                .HasColumnName("ScreenID");
            entity.Property(e => e.Serial)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceScreen)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceScreen_Device");
        });

        modelBuilder.Entity<DeviceScreenshot>(entity =>
        {
            entity.HasKey(e => e.ScreenshotId);

            entity.Property(e => e.ScreenshotFileName)
                .IsRequired()
                .HasMaxLength(260)
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AS");
            entity.Property(e => e.Timestamp).HasColumnType("datetime");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceScreenshot)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("FK_DeviceScreenshot_Device");

            entity.HasOne(d => d.Screen).WithMany(p => p.DeviceScreenshot)
                .HasForeignKey(d => d.ScreenId)
                .HasConstraintName("FK_DeviceScreenshot_DeviceScreen");
        });

        modelBuilder.Entity<DeviceType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK_DeviceTypes");

            entity.HasIndex(e => e.TypeId, "IX_DeviceTypes").IsUnique();

            entity.Property(e => e.TypeId)
                .ValueGeneratedNever()
                .HasColumnName("TypeID");
            entity.Property(e => e.Type)
                .IsRequired()
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AS");
        });

        modelBuilder.Entity<DeviceUsage>(entity =>
        {
            entity.Property(e => e.Battery)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.Cpu)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text")
                .HasColumnName("CPU");
            entity.Property(e => e.Disk)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text")
                .HasColumnName("DISK");
            entity.Property(e => e.Ram)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text")
                .HasColumnName("RAM");
        });

        modelBuilder.Entity<DeviceWarning>(entity =>
        {
            entity.HasKey(e => e.WarningId);

            entity.Property(e => e.AdditionalInfo)
                .UseCollation("Latin1_General_CI_AS")
                .HasColumnType("text");
            entity.Property(e => e.Timestamp).HasColumnType("datetime");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceWarning)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("FK_DeviceWarning_Device");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}