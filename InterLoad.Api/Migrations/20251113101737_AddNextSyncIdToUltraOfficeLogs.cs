using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterLoad.Migrations
{
    /// <inheritdoc />
    public partial class AddNextSyncIdToUltraOfficeLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "UltraOfficeSyncLogs");

            migrationBuilder.AddColumn<long>(
                name: "NextSyncId",
                table: "UltraOfficeSyncLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UsedSyncId",
                table: "UltraOfficeSyncLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextSyncId",
                table: "UltraOfficeSyncLogs");

            migrationBuilder.DropColumn(
                name: "UsedSyncId",
                table: "UltraOfficeSyncLogs");

            migrationBuilder.AddColumn<int>(
                name: "SyncId",
                table: "UltraOfficeSyncLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
