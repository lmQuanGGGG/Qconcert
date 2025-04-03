using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;

namespace Qconcert.Models;

public partial class TicketBoxDb1Context : IdentityDbContext
{
    public TicketBoxDb1Context()
    {
    }

    public TicketBoxDb1Context(DbContextOptions<TicketBoxDb1Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Event> Events { get; set; }
    public DbSet<Ticket> Tickets { get; set; } // Tạo vé
    public DbSet<PaymentInfo> PaymentInfos { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=TicketBoxDb1Context");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC07C7E06C03");

            entity.HasIndex(e => e.Name, "UQ__Categori__737584F6773C92D3").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Events__3214EC07FF70E776");

            entity.Property(e => e.AddressDetail).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.District).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.OrganizerName).HasMaxLength(255);

            entity.Property(e => e.Province).HasMaxLength(255);
            entity.Property(e => e.Ward).HasMaxLength(255);

            entity.HasOne(d => d.Category).WithMany(p => p.Events)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Events_Categories");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
