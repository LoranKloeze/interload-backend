using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterLoad.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationAndWeightToCollectables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Collectables",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "Collectables",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Collectables_Location",
                table: "Collectables",
                column: "Location");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Collectables_Location",
                table: "Collectables");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Collectables");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Collectables");
        }
    }
}
