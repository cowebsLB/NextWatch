using Microsoft.EntityFrameworkCore;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Data;

public sealed class NextWatchDbContext(DbContextOptions<NextWatchDbContext> options) : DbContext(options)
{
    public DbSet<MonitorTarget> Targets => Set<MonitorTarget>();
    public DbSet<CheckDefinition> Checks => Set<CheckDefinition>();
    public DbSet<CheckResult> Results => Set<CheckResult>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MonitorTarget>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Host).HasMaxLength(500);
            e.Property(x => x.Tag).HasMaxLength(100);
            e.HasIndex(x => x.Tag);
        });

        modelBuilder.Entity<CheckDefinition>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Target).WithMany(t => t.Checks).HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.NextRunUtc);
        });

        modelBuilder.Entity<CheckResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Check).WithMany(c => c.Results).HasForeignKey(x => x.CheckId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TimestampUtc);
            e.HasIndex(x => new { x.CheckId, x.TimestampUtc });
        });

        modelBuilder.Entity<AlertRule>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Check).WithMany(c => c.AlertRules).HasForeignKey(x => x.CheckId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlertEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.FiredAtUtc);
        });

        modelBuilder.Entity<AppSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasData(new AppSettings { Id = 1 });
        });
    }
}
