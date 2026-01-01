using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class ServerMonitoringDbContext : DbContext
{
    // This constructor is required for EF Core migrations and DI
    public ServerMonitoringDbContext(DbContextOptions<ServerMonitoringDbContext> options)
        : base(options)
    {
    }

    // Register our entities as database tables
    public DbSet<MonitoredServer> MonitoredServers { get; set; }
    public DbSet<MetricSample> MetricSamples { get; set; }
    public DbSet<DiskPartitionMetric> DiskPartitionMetrics { get; set; }
    public DbSet<NetworkInterfaceMetric> NetworkInterfaceMetrics { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceStatusHistory> ServiceStatusHistories { get; set; }
    public DbSet<AlertRule> AlertRules { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ProcessSnapshot> ProcessSnapshots { get; set; }
    public DbSet<ProcessInfo> ProcessInfos { get; set; }
    public DbSet<NotificationSettings> NotificationSettings { get; set; }
    public DbSet<TelegramChatId> TelegramChatIds { get; set; }
    public DbSet<BackupJob> BackupJobs { get; set; }
    public DbSet<BackupLog> BackupLogs { get; set; }
    public DbSet<WatchlistService> WatchlistServices { get; set; }
    public DbSet<WatchlistProcess> WatchlistProcesses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure MonitoredServer
        modelBuilder.Entity<MonitoredServer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Hostname).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AgentToken).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.AgentToken).IsUnique();
            entity.HasIndex(e => e.Hostname);
        });

        // Configure MetricSample
        modelBuilder.Entity<MetricSample>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.MonitoredServer)
                .WithMany(s => s.Metrics)
                .HasForeignKey(e => e.MonitoredServerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TimestampUtc);
            entity.HasIndex(e => new { e.MonitoredServerId, e.TimestampUtc });
        });

        // Configure DiskPartitionMetric
        modelBuilder.Entity<DiskPartitionMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.MetricSample)
                .WithMany(m => m.DiskPartitions)
                .HasForeignKey(e => e.MetricSampleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Device).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MountPoint).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileSystemType).IsRequired().HasMaxLength(50);
        });

        // Configure NetworkInterfaceMetric
        modelBuilder.Entity<NetworkInterfaceMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.MetricSample)
                .WithMany(m => m.NetworkInterfaces)
                .HasForeignKey(e => e.MetricSampleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MacAddress).HasMaxLength(50);
        });

        // Configure Service
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.MonitoredServer)
                .WithMany(s => s.Services)
                .HasForeignKey(e => e.MonitoredServerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => new { e.MonitoredServerId, e.Name }).IsUnique();
        });

        // Configure ServiceStatusHistory
        modelBuilder.Entity<ServiceStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Service)
                .WithMany(s => s.StatusHistory)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.TimestampUtc);
            entity.HasIndex(e => new { e.ServiceId, e.TimestampUtc });
        });

        // Configure AlertRule
        modelBuilder.Entity<AlertRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.MonitoredServer)
                .WithMany()
                .HasForeignKey(e => e.MonitoredServerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Metric).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Comparison).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TargetId).HasMaxLength(200);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.MonitoredServerId, e.IsActive });
        });

        // Configure Alert
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.MonitoredServer)
                .WithMany(s => s.Alerts)
                .HasForeignKey(e => e.MonitoredServerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AlertRule)
                .WithMany()
                .HasForeignKey(e => e.AlertRuleId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasOne(e => e.AcknowledgedByUser)
                .WithMany(u => u.AcknowledgedAlerts)
                .HasForeignKey(e => e.AcknowledgedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
            
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasIndex(e => e.IsAcknowledged);
            entity.HasIndex(e => new { e.MonitoredServerId, e.CreatedAtUtc });
        });

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure ProcessSnapshot
        modelBuilder.Entity<ProcessSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.MonitoredServer)
                .WithMany(s => s.ProcessSnapshots)
                .HasForeignKey(e => e.MonitoredServerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TimestampUtc);
            entity.HasIndex(e => new { e.MonitoredServerId, e.TimestampUtc });
        });

        // Configure ProcessInfo
        modelBuilder.Entity<ProcessInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.ProcessSnapshot)
                .WithMany(p => p.Processes)
                .HasForeignKey(e => e.ProcessSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
        });

        // Configure NotificationSettings
        modelBuilder.Entity<NotificationSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TelegramBotToken).HasMaxLength(500);
        });

        // Configure TelegramChatId
        modelBuilder.Entity<TelegramChatId>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.NotificationSettings)
                .WithMany(n => n.TelegramChatIds)
                .HasForeignKey(e => e.NotificationSettingsId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.ChatId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Label).HasMaxLength(200);
            entity.HasIndex(e => e.ChatId);
        });

        // Configure BackupJob
        modelBuilder.Entity<BackupJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Agent)
                .WithMany()
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SourcePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RepoUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RepoPasswordEncrypted).IsRequired();
            entity.Property(e => e.SshPrivateKeyEncrypted).IsRequired();
            entity.Property(e => e.ScheduleCron).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastRunStatus).IsRequired().HasMaxLength(50);
            
            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.AgentId, e.IsActive });
        });

        // Configure BackupLog
        modelBuilder.Entity<BackupLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Job)
                .WithMany(j => j.Logs)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SnapshotId).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasIndex(e => new { e.JobId, e.CreatedAtUtc });
        });

        // Configure WatchlistService
        modelBuilder.Entity<WatchlistService>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.MonitoredServer)
                .WithMany()
                .HasForeignKey(e => e.MonitoredServerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(200);
            
            entity.HasIndex(e => new { e.MonitoredServerId, e.ServiceName }).IsUnique();
            entity.HasIndex(e => e.MonitoredServerId);
        });

        // Configure WatchlistProcess
        modelBuilder.Entity<WatchlistProcess>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.MonitoredServer)
                .WithMany()
                .HasForeignKey(e => e.MonitoredServerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.ProcessName).IsRequired().HasMaxLength(500);
            
            entity.HasIndex(e => new { e.MonitoredServerId, e.ProcessName }).IsUnique();
            entity.HasIndex(e => e.MonitoredServerId);
        });
    }
}
