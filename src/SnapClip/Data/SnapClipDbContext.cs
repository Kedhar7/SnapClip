using Microsoft.EntityFrameworkCore;
using SnapClip.Models;

namespace SnapClip.Data;

/// <summary>
/// Entity Framework Core database context for SnapClip's local SQLite storage.
/// </summary>
public sealed class SnapClipDbContext : DbContext
{
    public DbSet<ClipItem> Clips => Set<ClipItem>();
    public DbSet<ClipCategory> Categories => Set<ClipCategory>();
    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();

    public SnapClipDbContext(DbContextOptions<SnapClipDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClipItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>();
            entity.HasIndex(e => e.CapturedAt);
            entity.HasIndex(e => e.IsPinned);
            entity.HasIndex(e => e.Category);
        });

        modelBuilder.Entity<ClipCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<TelemetryEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventName);
        });
    }
}
