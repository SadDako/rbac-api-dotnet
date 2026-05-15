using Microsoft.EntityFrameworkCore;
using Rbac.Domain.Entities;

namespace Rbac.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<DeviceFingerprintRecord> DeviceFingerprints => Set<DeviceFingerprintRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();
            builder.HasQueryFilter(u => !u.SoftDeleted);
        });

        modelBuilder.Entity<Role>(builder =>
        {
            builder.HasIndex(r => r.Name).IsUnique();
            builder.HasQueryFilter(r => !r.SoftDeleted);
        });

        modelBuilder.Entity<Permission>(builder =>
        {
            builder.HasIndex(p => p.Name).IsUnique();
            builder.HasQueryFilter(p => !p.SoftDeleted);
        });

        modelBuilder.Entity<UserRole>(builder =>
        {
            builder.HasKey(ur => new { ur.UserId, ur.RoleId });
            builder.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            builder.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
        });

        modelBuilder.Entity<UserPermission>(builder =>
        {
            builder.HasKey(up => new { up.UserId, up.PermissionId });
            builder.HasOne(up => up.User).WithMany(u => u.UserPermissions).HasForeignKey(up => up.UserId);
            builder.HasOne(up => up.Permission).WithMany(p => p.UserPermissions).HasForeignKey(up => up.PermissionId);
        });

        modelBuilder.Entity<RolePermission>(builder =>
        {
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            builder.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId);
            builder.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.HasIndex(rt => rt.TokenHash).IsUnique();
            builder.HasIndex(rt => rt.TokenFamilyId);
            builder.HasOne(rt => rt.User).WithMany(u => u.RefreshTokens).HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Property(rt => rt.DeviceFingerprint).HasMaxLength(128);
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.HasOne(al => al.User).WithMany(u => u.AuditLogs).HasForeignKey(al => al.UserId).OnDelete(DeleteBehavior.SetNull);
            builder.Property(al => al.Endpoint).HasMaxLength(256);
            builder.Property(al => al.Action).HasMaxLength(128);
            builder.Property(al => al.CorrelationId).HasMaxLength(128);
        });

        modelBuilder.Entity<Session>(builder =>
        {
            builder.HasOne(s => s.User).WithMany(u => u.Sessions).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(s => new { s.UserId, s.DeviceId });
            builder.HasIndex(s => s.RefreshTokenId);
            builder.HasIndex(s => s.TokenFamilyId);
            builder.Property(s => s.Fingerprint).HasMaxLength(128);
            builder.Property(s => s.DeviceName).HasMaxLength(128);
            builder.Property(s => s.UserAgent).HasMaxLength(512);
        });

        modelBuilder.Entity<SecurityEvent>(builder =>
        {
            builder.HasOne(se => se.User).WithMany().HasForeignKey(se => se.UserId).OnDelete(DeleteBehavior.SetNull);
            builder.Property(se => se.Device).HasMaxLength(256);
            builder.Property(se => se.IPAddress).HasMaxLength(64);
            builder.Property(se => se.Country).HasMaxLength(128);
            builder.Property(se => se.Metadata).HasColumnType("jsonb");
        });

        modelBuilder.Entity<DeviceFingerprintRecord>(builder =>
        {
            builder.HasOne(df => df.User).WithMany(u => u.DeviceFingerprints).HasForeignKey(df => df.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(df => new { df.UserId, df.Fingerprint }).IsUnique();
            builder.Property(df => df.Fingerprint).HasMaxLength(128);
            builder.Property(df => df.UserAgent).HasMaxLength(512);
            builder.Property(df => df.DeviceName).HasMaxLength(128);
            builder.Property(df => df.Browser).HasMaxLength(128);
            builder.Property(df => df.OS).HasMaxLength(128);
            builder.Property(df => df.Platform).HasMaxLength(128);
            builder.Property(df => df.HeaderSignature).HasMaxLength(128);
        });

        base.OnModelCreating(modelBuilder);
    }
}
