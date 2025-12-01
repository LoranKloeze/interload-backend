using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterLoad.Migrations
{
    /// <inheritdoc />
    public partial class AddPrepSubProjectForToSubProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrepSubProjectForId",
                table: "SubProjects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubProjects_PrepSubProjectForId",
                table: "SubProjects",
                column: "PrepSubProjectForId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubProjects_SubProjects_PrepSubProjectForId",
                table: "SubProjects",
                column: "PrepSubProjectForId",
                principalTable: "SubProjects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubProjects_SubProjects_PrepSubProjectForId",
                table: "SubProjects");

            migrationBuilder.DropIndex(
                name: "IX_SubProjects_PrepSubProjectForId",
                table: "SubProjects");

            migrationBuilder.DropColumn(
                name: "PrepSubProjectForId",
                table: "SubProjects");
        }
    }
}
