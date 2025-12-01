using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InterLoad.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Collected = table.Column<int>(type: "integer", nullable: false),
                    KeycloakUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeycloakUserName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollectActionCollectDemand",
                columns: table => new
                {
                    CollectActionsId = table.Column<int>(type: "integer", nullable: false),
                    CollectDemandsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectActionCollectDemand", x => new { x.CollectActionsId, x.CollectDemandsId });
                    table.ForeignKey(
                        name: "FK_CollectActionCollectDemand_CollectActions_CollectActionsId",
                        column: x => x.CollectActionsId,
                        principalTable: "CollectActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectActionCollectDemand_CollectDemands_CollectDemandsId",
                        column: x => x.CollectDemandsId,
                        principalTable: "CollectDemands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectActionCollectDemand_CollectDemandsId",
                table: "CollectActionCollectDemand",
                column: "CollectDemandsId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectActions_KeycloakUserId",
                table: "CollectActions",
                column: "KeycloakUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectActions_KeycloakUserName",
                table: "CollectActions",
                column: "KeycloakUserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectActionCollectDemand");

            migrationBuilder.DropTable(
                name: "CollectActions");
        }
    }
}
