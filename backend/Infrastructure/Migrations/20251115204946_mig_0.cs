using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonitoredServers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Hostname = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    AgentToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoredServers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Metric = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ThresholdValue = table.Column<double>(type: "double precision", nullable: false),
                    Comparison = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MonitoredServerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertRules_MonitoredServers_MonitoredServerId",
                        column: x => x.MonitoredServerId,
                        principalTable: "MonitoredServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetricSamples",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "double precision", nullable: false),
                    RamUsagePercent = table.Column<double>(type: "double precision", nullable: false),
                    RamUsedGb = table.Column<double>(type: "double precision", nullable: false),
                    UptimeSeconds = table.Column<long>(type: "bigint", nullable: false),
                    Load1m = table.Column<double>(type: "double precision", nullable: false),
                    Load5m = table.Column<double>(type: "double precision", nullable: false),
                    Load15m = table.Column<double>(type: "double precision", nullable: false),
                    DiskReadSpeedMBps = table.Column<double>(type: "double precision", nullable: false),
                    DiskWriteSpeedMBps = table.Column<double>(type: "double precision", nullable: false),
                    MonitoredServerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricSamples_MonitoredServers_MonitoredServerId",
                        column: x => x.MonitoredServerId,
                        principalTable: "MonitoredServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MonitoredServerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessSnapshots_MonitoredServers_MonitoredServerId",
                        column: x => x.MonitoredServerId,
                        principalTable: "MonitoredServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MonitoredServerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_MonitoredServers_MonitoredServerId",
                        column: x => x.MonitoredServerId,
                        principalTable: "MonitoredServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedByUserId = table.Column<int>(type: "integer", nullable: true),
                    MonitoredServerId = table.Column<int>(type: "integer", nullable: false),
                    AlertRuleId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Alerts_MonitoredServers_MonitoredServerId",
                        column: x => x.MonitoredServerId,
                        principalTable: "MonitoredServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alerts_Users_AcknowledgedByUserId",
                        column: x => x.AcknowledgedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DiskPartitionMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Device = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MountPoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSystemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalGb = table.Column<double>(type: "double precision", nullable: false),
                    UsedGb = table.Column<double>(type: "double precision", nullable: false),
                    UsagePercent = table.Column<double>(type: "double precision", nullable: false),
                    MetricSampleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiskPartitionMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiskPartitionMetrics_MetricSamples_MetricSampleId",
                        column: x => x.MetricSampleId,
                        principalTable: "MetricSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetworkInterfaceMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MacAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ipv4 = table.Column<string>(type: "text", nullable: true),
                    Ipv6 = table.Column<string>(type: "text", nullable: true),
                    UploadSpeedMbps = table.Column<double>(type: "double precision", nullable: false),
                    DownloadSpeedMbps = table.Column<double>(type: "double precision", nullable: false),
                    MetricSampleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkInterfaceMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkInterfaceMetrics_MetricSamples_MetricSampleId",
                        column: x => x.MetricSampleId,
                        principalTable: "MetricSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessInfos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Pid = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CpuPercent = table.Column<double>(type: "double precision", nullable: false),
                    RamMb = table.Column<double>(type: "double precision", nullable: false),
                    User = table.Column<string>(type: "text", nullable: true),
                    ProcessSnapshotId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessInfos_ProcessSnapshots_ProcessSnapshotId",
                        column: x => x.ProcessSnapshotId,
                        principalTable: "ProcessSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceStatusHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceStatusHistories_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_IsActive",
                table: "AlertRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_MonitoredServerId",
                table: "AlertRules",
                column: "MonitoredServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AcknowledgedByUserId",
                table: "Alerts",
                column: "AcknowledgedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AlertRuleId",
                table: "Alerts",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_CreatedAtUtc",
                table: "Alerts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsAcknowledged",
                table: "Alerts",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_MonitoredServerId_CreatedAtUtc",
                table: "Alerts",
                columns: new[] { "MonitoredServerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DiskPartitionMetrics_MetricSampleId",
                table: "DiskPartitionMetrics",
                column: "MetricSampleId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricSamples_MonitoredServerId_TimestampUtc",
                table: "MetricSamples",
                columns: new[] { "MonitoredServerId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricSamples_TimestampUtc",
                table: "MetricSamples",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredServers_AgentToken",
                table: "MonitoredServers",
                column: "AgentToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredServers_Hostname",
                table: "MonitoredServers",
                column: "Hostname");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkInterfaceMetrics_MetricSampleId",
                table: "NetworkInterfaceMetrics",
                column: "MetricSampleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInfos_ProcessSnapshotId",
                table: "ProcessInfos",
                column: "ProcessSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessSnapshots_MonitoredServerId_TimestampUtc",
                table: "ProcessSnapshots",
                columns: new[] { "MonitoredServerId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessSnapshots_TimestampUtc",
                table: "ProcessSnapshots",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Services_MonitoredServerId_Name",
                table: "Services",
                columns: new[] { "MonitoredServerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStatusHistories_ServiceId_TimestampUtc",
                table: "ServiceStatusHistories",
                columns: new[] { "ServiceId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStatusHistories_TimestampUtc",
                table: "ServiceStatusHistories",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "DiskPartitionMetrics");

            migrationBuilder.DropTable(
                name: "NetworkInterfaceMetrics");

            migrationBuilder.DropTable(
                name: "ProcessInfos");

            migrationBuilder.DropTable(
                name: "ServiceStatusHistories");

            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "MetricSamples");

            migrationBuilder.DropTable(
                name: "ProcessSnapshots");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "MonitoredServers");
        }
    }
}
