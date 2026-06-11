using Microsoft.EntityFrameworkCore;
using RestaurantOS.Domain.Entities;
using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Table> Tables => Set<Table>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Table>(e =>
        {
            e.HasKey(t => t.TableId);
            e.HasIndex(t => t.TableNumber).IsUnique();
            e.Property(t => t.Name).HasMaxLength(50);
            e.Property(t => t.Section).HasMaxLength(50);
            e.Property(t => t.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.HasKey(s => s.SessionId);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.Notes).HasMaxLength(500);
            e.Property(s => s.TotalAmount).HasPrecision(18, 2);
            e.Property(s => s.DiscountAmount).HasPrecision(18, 2);
            e.Property(s => s.TaxAmount).HasPrecision(18, 2);
            e.Property(s => s.FinalAmount).HasPrecision(18, 2);
            e.Property(s => s.RowVersion).IsRowVersion();
            e.HasOne(s => s.Table).WithMany(t => t.Sessions).HasForeignKey(s => s.TableId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.OpenedByUser).WithMany(u => u.OpenedSessions).HasForeignKey(s => s.OpenedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.ClosedByUser).WithMany(u => u.ClosedSessions).HasForeignKey(s => s.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(o => o.OrderItemId);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(o => o.UnitPrice).HasPrecision(18, 2);
            e.Property(o => o.Discount).HasPrecision(18, 2);
            e.Property(o => o.LineTotal).HasPrecision(18, 2);
            e.Property(o => o.Notes).HasMaxLength(200);
            e.Property(o => o.RowVersion).IsRowVersion();
            e.HasOne(o => o.Session).WithMany(s => s.OrderItems).HasForeignKey(o => o.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(o => o.MenuItem).WithMany(m => m.OrderItems).HasForeignKey(o => o.MenuItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.AddedByUser).WithMany(u => u.OrderItems).HasForeignKey(o => o.AddedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.PaymentId);
            e.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.ChangeGiven).HasPrecision(18, 2);
            e.Property(p => p.ReferenceNo).HasMaxLength(100);
            e.Property(p => p.VoidReason).HasMaxLength(200);
            e.Property(p => p.RowVersion).IsRowVersion();
            e.HasOne(p => p.Session).WithMany(s => s.Payments).HasForeignKey(p => p.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.ReceivedByUser).WithMany(u => u.Payments).HasForeignKey(p => p.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MenuCategory>(e =>
        {
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.Name).HasMaxLength(100);
            e.Property(c => c.Icon).HasMaxLength(50);
            e.Property(c => c.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<MenuItem>(e =>
        {
            e.HasKey(m => m.MenuItemId);
            e.Property(m => m.Name).HasMaxLength(150);
            e.Property(m => m.Description).HasMaxLength(500);
            e.Property(m => m.Price).HasPrecision(18, 2);
            e.Property(m => m.TaxRate).HasPrecision(5, 2);
            e.Property(m => m.ImagePath).HasMaxLength(500);
            e.Property(m => m.RowVersion).IsRowVersion();
            e.HasOne(m => m.Category).WithMany(c => c.MenuItems).HasForeignKey(m => m.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.UserId);
            e.Property(u => u.FullName).HasMaxLength(150);
            e.Property(u => u.Username).HasMaxLength(50);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.PasswordHash).HasMaxLength(255);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.LogId);
            e.Property(a => a.Action).HasMaxLength(100);
            e.Property(a => a.EntityType).HasMaxLength(50);
            e.Property(a => a.EntityId).HasMaxLength(100);
            e.Property(a => a.IPAddress).HasMaxLength(45);
            e.HasOne(a => a.User).WithMany(u => u.AuditLogs).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Setting>(e =>
        {
            e.HasKey(s => s.Key);
            e.Property(s => s.Key).HasMaxLength(100);
            e.Property(s => s.Description).HasMaxLength(300);
        });
    }
}
