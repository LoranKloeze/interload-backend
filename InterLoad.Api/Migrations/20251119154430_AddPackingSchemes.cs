using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InterLoad.Migrations
{
    /// <inheritdoc />
    public partial class AddPackingSchemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UltraOfficePackingSchemeId = table.Column<int>(type: "integer", nullable: false),
                    MergeStockPeriods = table.Column<bool>(type: "boolean", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectGroups_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectGroupEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupNr = table.Column<int>(type: "integer", nullable: false),
                    UltraOfficePackingSchemeEntryId = table.Column<int>(type: "integer", nullable: false),
                    CollectGroupId = table.Column<int>(type: "integer", nullable: false),
                    SubProjectId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectGroupEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectGroupEntries_CollectGroups_CollectGroupId",
                        column: x => x.CollectGroupId,
                        principalTable: "CollectGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectGroupEntries_SubProjects_SubProjectId",
                        column: x => x.SubProjectId,
                        principalTable: "SubProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectGroupEntries_CollectGroupId",
                table: "CollectGroupEntries",
                column: "CollectGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectGroupEntries_SubProjectId",
                table: "CollectGroupEntries",
                column: "SubProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectGroups_ProjectId",
                table: "CollectGroups",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectGroupEntries");

            migrationBuilder.DropTable(
                name: "CollectGroups");
        }
    }
}
