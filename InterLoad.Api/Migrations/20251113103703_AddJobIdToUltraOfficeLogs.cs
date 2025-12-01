using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterLoad.Migrations
{
    /// <inheritdoc />
    public partial class AddJobIdToUltraOfficeLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HangfireJobId",
                table: "UltraOfficeSyncLogs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HangfireJobId",
                table: "UltraOfficeSyncLogs");
        }
    }
}
