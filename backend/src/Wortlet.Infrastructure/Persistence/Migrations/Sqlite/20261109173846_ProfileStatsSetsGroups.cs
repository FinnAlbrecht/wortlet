using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wortlet.Infrastructure.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ProfileStatsSetsGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceShareId",
                table: "Folders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AvatarImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvatarImages_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    JoinCode = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PracticeDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Answers = table.Column<int>(type: "INTEGER", nullable: false),
                    Correct = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeDays_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    AvatarIcon = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    AvatarColor = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    HasImage = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WordSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordSets_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WordSets_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WordStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FolderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WordKey = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    Wrong = table.Column<int>(type: "INTEGER", nullable: false),
                    LastCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPracticedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordStats_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => new { x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GroupMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SharedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    FolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SetId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SharedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupShares_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupShares_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupShares_WordSets_SetId",
                        column: x => x.SetId,
                        principalTable: "WordSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SetId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    PracticeMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    WordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstRoundCorrect = table.Column<int>(type: "INTEGER", nullable: false),
                    Grade = table.Column<double>(type: "REAL", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizResults_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizResults_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QuizResults_WordSets_SetId",
                        column: x => x.SetId,
                        principalTable: "WordSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WordSetItems",
                columns: table => new
                {
                    FolderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WordKey = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    WordSetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordSetItems", x => new { x.WordSetId, x.FolderId, x.WordKey });
                    table.ForeignKey(
                        name: "FK_WordSetItems_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WordSetItems_WordSets_WordSetId",
                        column: x => x.WordSetId,
                        principalTable: "WordSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Folders_OwnerId_SourceShareId",
                table: "Folders",
                columns: new[] { "OwnerId", "SourceShareId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_JoinCode",
                table: "Groups",
                column: "JoinCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupShares_FolderId",
                table: "GroupShares",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupShares_GroupId",
                table: "GroupShares",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupShares_SetId",
                table: "GroupShares",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeDays_OwnerId_Day",
                table: "PracticeDays",
                columns: new[] { "OwnerId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_FolderId",
                table: "QuizResults",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_OwnerId_FinishedAt",
                table: "QuizResults",
                columns: new[] { "OwnerId", "FinishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_SetId",
                table: "QuizResults",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_WordSetItems_FolderId",
                table: "WordSetItems",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_WordSets_LanguageId",
                table: "WordSets",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_WordSets_OwnerId_LanguageId",
                table: "WordSets",
                columns: new[] { "OwnerId", "LanguageId" });

            migrationBuilder.CreateIndex(
                name: "IX_WordStats_FolderId_WordKey",
                table: "WordStats",
                columns: new[] { "FolderId", "WordKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordStats_OwnerId",
                table: "WordStats",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvatarImages");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "GroupShares");

            migrationBuilder.DropTable(
                name: "PracticeDays");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "QuizResults");

            migrationBuilder.DropTable(
                name: "WordSetItems");

            migrationBuilder.DropTable(
                name: "WordStats");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "WordSets");

            migrationBuilder.DropIndex(
                name: "IX_Folders_OwnerId_SourceShareId",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "SourceShareId",
                table: "Folders");
        }
    }
}
