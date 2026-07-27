using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Cutter4.Domain;
using Cutter4.Persistence.Entities;

namespace Cutter4.Persistence;

public partial class CutterContext : DbContext
{
    public CutterContext()
    {
    }

    public CutterContext(DbContextOptions<CutterContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ConfigurationEntity> Configurations { get; set; }

    public virtual DbSet<ConfigurationItemEntity> CutItems { get; set; }

    public virtual DbSet<KapuEntity> Kapuk { get; set; }

    public virtual DbSet<OrderEntity> Orders { get; set; }

    public virtual DbSet<PanelEntity> Panels { get; set; }

    public virtual DbSet<StorageEditEntity> StorageEdits { get; set; }

    public virtual DbSet<StorageEditItemEntity> StorageEditItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigurationEntity>(entity =>
        {
            entity.ToTable("configurations");

            entity.HasIndex(e => new { e.Id, e.OrderId, e.Color }, "IX_configurations_id_order_id_color_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color).HasColumnName("color_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PanelId).HasColumnName("panel_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Configurations)
                .HasForeignKey(d => d.OrderId);

            entity.HasOne(d => d.Panel).WithMany(p => p.Configurations)
                .HasPrincipalKey(p => new { p.Id, p.Color })
                .HasForeignKey(d => new { d.PanelId, d.Color })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ConfigurationItemEntity>(entity =>
        {
            entity.ToTable("configuration_items");

            entity.HasIndex(e => new { e.KapuId, e.ConfigurationId }, "IX_configuration_items_kapu_id_configuration_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color).HasColumnName("color_id");
            entity.Property(e => e.ConfigurationId).HasColumnName("configuration_id");
            entity.Property(e => e.KapuId).HasColumnName("kapu_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");

            entity.HasOne(d => d.Configuration).WithMany(p => p.ConfigurationItems)
                .HasPrincipalKey(p => new { p.Id, p.OrderId, p.Color })
                .HasForeignKey(d => new { d.ConfigurationId, d.OrderId, d.Color });

            entity.HasOne(d => d.Kapu).WithMany(p => p.ConfigurationItems)
                .HasPrincipalKey(p => new { p.Id, p.OrderId, p.Color })
                .HasForeignKey(d => new { d.KapuId, d.OrderId, d.Color });
        });

        modelBuilder.Entity<KapuEntity>(entity =>
        {
            entity.ToTable("kapuk");

            entity.HasIndex(e => e.SerialNumber, "IX_kapuk_serial_number").IsUnique();

            entity.HasIndex(e => new { e.Id, e.OrderId, e.Color }, "IX_kapuk_id_order_id_color_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color).HasColumnName("color_id");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.Length).HasColumnName("length");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.SerialNumber).HasColumnName("serial_number");

            entity.HasOne(d => d.Order).WithMany(p => p.Kapuk)
                .HasForeignKey(d => d.OrderId);
        });

        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.ToTable("orders");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Date).HasColumnName("date");
        });

        modelBuilder.Entity<PanelEntity>(entity =>
        {
            entity.ToTable("panels");

            entity.HasIndex(e => new { e.Id, e.Color }, "IX_panels_id_color_id").IsUnique();

            entity.HasIndex(e => new { e.Length, e.Width, e.Color }, "IX_panels_length_width_color_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color).HasColumnName("color_id");
            entity.Property(e => e.Length).HasColumnName("length");
            entity.Property(e => e.StorageCount).HasColumnName("storage_count");
            entity.Property(e => e.Width)
                .IsRequired()
                .HasColumnType("enum('500mm','610mm')")
                .HasConversion(
                    v => v == PanelWidths.Width500mm ? "500mm" : "610mm",
                    v => v == "500mm" ? PanelWidths.Width500mm : PanelWidths.Width610mm)
                .HasColumnName("width");
        });

        modelBuilder.Entity<StorageEditEntity>(entity =>
        {
            entity.ToTable("storage_edits");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Message).HasColumnName("message");
        });

        modelBuilder.Entity<StorageEditItemEntity>(entity =>
        {
            entity.ToTable("storage_edit_items");

            entity.HasIndex(e => new { e.PanelId, e.EditId }, "IX_storage_edit_items_panel_id_edit_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeltaCount).HasColumnName("delta_count");
            entity.Property(e => e.EditId).HasColumnName("edit_id");
            entity.Property(e => e.PanelId).HasColumnName("panel_id");

            entity.HasOne(d => d.Edit).WithMany(p => p.StorageEditItems)
                .HasForeignKey(d => d.EditId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Panel).WithMany(p => p.StorageEditItems)
                .HasForeignKey(d => d.PanelId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
