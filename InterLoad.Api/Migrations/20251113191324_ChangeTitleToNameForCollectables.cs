using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterLoad.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTitleToNameForCollectables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Collectables",
                newName: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Collectables",
                newName: "Title");
        }
    }
}
