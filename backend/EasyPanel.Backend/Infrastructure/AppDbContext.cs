using EasyPanel.Backend.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Instance> Instances => Set<Instance>();
    public DbSet<PortAllocation> PortAllocations => Set<PortAllocation>();
    public DbSet<StaffServerPermission> StaffServerPermissions => Set<StaffServerPermission>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("users");
            entity.Property(user => user.Email).HasColumnType("citext");
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Node>(entity =>
        {
            entity.ToTable("nodes");
        });

        modelBuilder.Entity<Instance>(entity =>
        {
            entity.ToTable("instances");
            entity.HasOne(instance => instance.Node)
                .WithMany()
                .HasForeignKey(instance => instance.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PortAllocation>(entity =>
        {
            entity.ToTable("port_allocations");
            entity.HasOne<Instance>()
                .WithMany()
                .HasForeignKey(allocation => allocation.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(allocation => allocation.NodeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(allocation => new { allocation.NodeId, allocation.Port, allocation.Protocol }).IsUnique();
        });

        modelBuilder.Entity<StaffServerPermission>(entity =>
        {
            entity.ToTable("staff_server_permissions");
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(permission => permission.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Instance>()
                .WithMany()
                .HasForeignKey(permission => permission.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(permission => new { permission.UserId, permission.InstanceId }).IsUnique();
        });

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("audit_log_entries");
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(logEntry => logEntry.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Instance>()
                .WithMany()
                .HasForeignKey(logEntry => logEntry.InstanceId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(logEntry => logEntry.NodeId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(logEntry => new { logEntry.InstanceId, logEntry.CreatedAtUtc });
            entity.HasIndex(logEntry => new { logEntry.ActorUserId, logEntry.CreatedAtUtc });
        });
    }
}
