using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InterLoad.Migrations
{
    /// <inheritdoc />
    public partial class AddStockPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockPeriodId",
                table: "CollectDemands",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StockPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UltraOfficeId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockPeriods_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectDemands_StockPeriodId",
                table: "CollectDemands",
                column: "StockPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPeriods_ProjectId",
                table: "StockPeriods",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectDemands_StockPeriods_StockPeriodId",
                table: "CollectDemands",
                column: "StockPeriodId",
                principalTable: "StockPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectDemands_StockPeriods_StockPeriodId",
                table: "CollectDemands");

            migrationBuilder.DropTable(
                name: "StockPeriods");

            migrationBuilder.DropIndex(
                name: "IX_CollectDemands_StockPeriodId",
                table: "CollectDemands");

            migrationBuilder.DropColumn(
                name: "StockPeriodId",
                table: "CollectDemands");
        }
    }
}
