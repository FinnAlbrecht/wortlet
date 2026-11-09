using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wortlet.Infrastructure.Persistence.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class GroupVisibilityAndRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Groups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Private"); // bestehende Gruppen bleiben privat (nur per Code)

            migrationBuilder.CreateTable(
                name: "GroupJoinRequests",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupJoinRequests", x => new { x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GroupJoinRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupJoinRequests_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Visibility",
                table: "Groups",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_GroupJoinRequests_UserId",
                table: "GroupJoinRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupJoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_Groups_Visibility",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Groups");
        }
    }
}
