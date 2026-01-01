using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourcePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RepoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RepoPasswordEncrypted = table.Column<string>(type: "text", nullable: false),
                    SshPrivateKeyEncrypted = table.Column<string>(type: "text", nullable: false),
                    ScheduleCron = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastRunStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastRunAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackupJobs_MonitoredServers_AgentId",
                        column: x => x.AgentId,
                        principalTable: "MonitoredServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackupLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SnapshotId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FilesNew = table.Column<int>(type: "integer", nullable: true),
                    DataAdded = table.Column<long>(type: "bigint", nullable: true),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackupLogs_BackupJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "BackupJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackupJobs_AgentId",
                table: "BackupJobs",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupJobs_AgentId_IsActive",
                table: "BackupJobs",
                columns: new[] { "AgentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BackupJobs_IsActive",
                table: "BackupJobs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BackupLogs_CreatedAtUtc",
                table: "BackupLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BackupLogs_JobId",
                table: "BackupLogs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupLogs_JobId_CreatedAtUtc",
                table: "BackupLogs",
                columns: new[] { "JobId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupLogs");

            migrationBuilder.DropTable(
                name: "BackupJobs");
        }
    }
}
