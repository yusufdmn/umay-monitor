using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServerSystemInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Architecture",
                table: "MonitoredServers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CpuCores",
                table: "MonitoredServers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CpuModel",
                table: "MonitoredServers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CpuThreads",
                table: "MonitoredServers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kernel",
                table: "MonitoredServers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Os",
                table: "MonitoredServers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OsVersion",
                table: "MonitoredServers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Architecture",
                table: "MonitoredServers");

            migrationBuilder.DropColumn(
                name: "CpuCores",
                table: "MonitoredServers");

            migrationBuilder.DropColumn(
                name: "CpuModel",
                table: "MonitoredServers");

            migrationBuilder.DropColumn(
                name: "CpuThreads",
                table: "MonitoredServers");

            migrationBuilder.DropColumn(
                name: "Kernel",
                table: "MonitoredServers");

            migrationBuilder.DropColumn(
                name: "Os",
                table: "MonitoredServers");

            migrationBuilder.DropColumn(
                name: "OsVersion",
                table: "MonitoredServers");
        }
    }
}
