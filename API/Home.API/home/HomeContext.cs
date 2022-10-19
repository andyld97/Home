﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Home.API.home.Models;

namespace Home.API.home
{
    public partial class HomeContext : DbContext
    {
        public HomeContext()
        {
        }

        public HomeContext(DbContextOptions<HomeContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Device> Device { get; set; }
        public virtual DbSet<DeviceBattery> DeviceBattery { get; set; }
        public virtual DbSet<DeviceDiskDrive> DeviceDiskDrive { get; set; }
        public virtual DbSet<DeviceEnvironment> DeviceEnvironment { get; set; }
        public virtual DbSet<DeviceGraphic> DeviceGraphic { get; set; }
        public virtual DbSet<DeviceLog> DeviceLog { get; set; }
        public virtual DbSet<DeviceOstype> DeviceOstype { get; set; }
        public virtual DbSet<DeviceScreenshot> DeviceScreenshot { get; set; }
        public virtual DbSet<DeviceType> DeviceType { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Device>(entity =>
            {
                entity.HasIndex(e => e.EnvironmentId, "IX_Device")
                    .IsUnique();

                entity.Property(e => e.DeviceGroup)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Guid)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("GUID");

                entity.Property(e => e.Ip)
                    .HasMaxLength(50)
                    .HasColumnName("IP");

                entity.Property(e => e.LastSeen).HasColumnType("datetime");

                entity.Property(e => e.Location)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .IsUnicode(false);

                entity.Property(e => e.Ostype).HasColumnName("OSType");

                entity.Property(e => e.ServiceClientVersion)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.DeviceType)
                    .WithMany(p => p.Device)
                    .HasForeignKey(d => d.DeviceTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DeviceTypes");

                entity.HasOne(d => d.Environment)
                    .WithOne(p => p.Device)
                    .HasForeignKey<Device>(d => d.EnvironmentId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Environment");

                entity.HasOne(d => d.OstypeNavigation)
                    .WithMany(p => p.Device)
                    .HasForeignKey(d => d.Ostype)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OSTypes");
            });

            modelBuilder.Entity<DeviceBattery>(entity =>
            {
                entity.HasKey(e => e.BatteryId);

                entity.Property(e => e.BatteryId).HasColumnName("BatteryID");
            });

            modelBuilder.Entity<DeviceDiskDrive>(entity =>
            {
                entity.HasKey(e => e.DiskDriveId)
                    .HasName("PK_DiskDriveId");

                entity.Property(e => e.DeviceId).HasColumnName("DeviceID");

                entity.Property(e => e.DiskInterface).HasColumnType("text");

                entity.Property(e => e.DiskModel).HasColumnType("text");

                entity.Property(e => e.DiskName).HasColumnType("text");

                entity.Property(e => e.DriveId)
                    .HasColumnType("text")
                    .HasColumnName("DriveID");

                entity.Property(e => e.DriveName).HasColumnType("text");

                entity.Property(e => e.FileSystem).HasColumnType("text");

                entity.Property(e => e.Guid)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("GUID");

                entity.Property(e => e.MediaStatus).HasColumnType("text");

                entity.Property(e => e.MediaType).HasColumnType("text");

                entity.Property(e => e.PhysicalName).HasColumnType("text");

                entity.Property(e => e.VolumeName).HasColumnType("text");

                entity.Property(e => e.VolumeSerial).HasColumnType("text");

                entity.HasOne(d => d.Device)
                    .WithMany(p => p.DeviceDiskDrive)
                    .HasForeignKey(d => d.DeviceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DeviceDiskDrives_Device");
            });

            modelBuilder.Entity<DeviceEnvironment>(entity =>
            {
                entity.HasKey(e => e.DeviceId);

                entity.Property(e => e.DeviceId).HasColumnName("DeviceID");

                entity.Property(e => e.BatteryId).HasColumnName("BatteryID");

                entity.Property(e => e.Cpucount)
                    .HasColumnName("CPUCount")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Cpuname)
                    .HasColumnType("text")
                    .HasColumnName("CPUName");

                entity.Property(e => e.Cpuusage).HasColumnName("CPUUsage");

                entity.Property(e => e.Description).HasColumnType("text");

                entity.Property(e => e.DomainName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FreeRam)
                    .HasColumnType("text")
                    .HasColumnName("FreeRAM");

                entity.Property(e => e.Is64BitOs).HasColumnName("Is64BitOS");

                entity.Property(e => e.MachineName)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Motherboard).HasColumnType("text");

                entity.Property(e => e.Osname)
                    .HasColumnType("text")
                    .HasColumnName("OSName");

                entity.Property(e => e.Osversion)
                    .HasColumnType("text")
                    .HasColumnName("OSVersion");

                entity.Property(e => e.Product).HasColumnType("text");

                entity.Property(e => e.StartTimestamp).HasColumnType("datetime");

                entity.Property(e => e.TotalRam).HasColumnName("TotalRAM");

                entity.Property(e => e.UserName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Vendor).HasColumnType("text");

                entity.HasOne(d => d.Battery)
                    .WithMany(p => p.DeviceEnvironment)
                    .HasForeignKey(d => d.BatteryId)
                    .HasConstraintName("FK_DeviceEnvironment_DeviceBattery");
            });

            modelBuilder.Entity<DeviceGraphic>(entity =>
            {
                entity.HasKey(e => e.DeviceGraphicsId)
                    .HasName("PK_DeviceGraphicsID");

                entity.Property(e => e.DeviceGraphicsId).HasColumnName("DeviceGraphicsID");

                entity.Property(e => e.DeviceId).HasColumnName("DeviceID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("text");

                entity.HasOne(d => d.Device)
                    .WithMany(p => p.DeviceGraphic)
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
                    .HasColumnType("text");

                entity.Property(e => e.DeviceId).HasColumnName("DeviceID");

                entity.Property(e => e.Timestamp).HasColumnType("datetime");

                entity.HasOne(d => d.Device)
                    .WithMany(p => p.DeviceLog)
                    .HasForeignKey(d => d.DeviceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DeviceID");
            });

            modelBuilder.Entity<DeviceOstype>(entity =>
            {
                entity.HasKey(e => e.OstypeId)
                    .HasName("PK_OSTypes");

                entity.ToTable("DeviceOSType");

                entity.Property(e => e.OstypeId)
                    .ValueGeneratedNever()
                    .HasColumnName("OSTypeID");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .IsUnicode(false);
            });

            modelBuilder.Entity<DeviceScreenshot>(entity =>
            {
                entity.HasKey(e => e.ScreenshotId);

                entity.Property(e => e.ScreenshotFileName)
                    .IsRequired()
                    .HasMaxLength(260)
                    .IsUnicode(false);

                entity.Property(e => e.Timestamp).HasColumnType("datetime");

                entity.HasOne(d => d.Device)
                    .WithMany(p => p.DeviceScreenshot)
                    .HasForeignKey(d => d.DeviceId)
                    .HasConstraintName("FK_DeviceScreenshot_Device");
            });

            modelBuilder.Entity<DeviceType>(entity =>
            {
                entity.HasKey(e => e.TypeId)
                    .HasName("PK_DeviceTypes");

                entity.HasIndex(e => e.TypeId, "IX_DeviceTypes")
                    .IsUnique();

                entity.Property(e => e.TypeId)
                    .ValueGeneratedNever()
                    .HasColumnName("TypeID");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}