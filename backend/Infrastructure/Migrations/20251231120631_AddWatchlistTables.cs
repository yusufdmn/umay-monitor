using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchlistTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WatchlistProcesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MonitoredServerId = table.Column<int>(type: "integer", nullable: false),
                    ProcessName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchlistProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchlistProcesses_MonitoredServers_MonitoredServerId",
                        column: x => x.MonitoredServerId,
                        principalTable: "MonitoredServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WatchlistServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MonitoredServerId = table.Column<int>(type: "integer", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchlistServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchlistServices_MonitoredServers_MonitoredServerId",
                        column: x => x.MonitoredServerId,
                        principalTable: "MonitoredServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistProcesses_MonitoredServerId",
                table: "WatchlistProcesses",
                column: "MonitoredServerId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistProcesses_MonitoredServerId_ProcessName",
                table: "WatchlistProcesses",
                columns: new[] { "MonitoredServerId", "ProcessName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistServices_MonitoredServerId",
                table: "WatchlistServices",
                column: "MonitoredServerId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistServices_MonitoredServerId_ServiceName",
                table: "WatchlistServices",
                columns: new[] { "MonitoredServerId", "ServiceName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WatchlistProcesses");

            migrationBuilder.DropTable(
                name: "WatchlistServices");
        }
    }
}
