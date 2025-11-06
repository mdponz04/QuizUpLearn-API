using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class updateDatabase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "GroupItems",
                table: "QuizSets");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<Guid>(
                name: "QuizGroupItemId",
                table: "Quizzes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuizGroupItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizSet = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    AudioUrl = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    AudioScript = table.Column<string>(type: "text", nullable: true),
                    ImageDescription = table.Column<string>(type: "text", nullable: true),
                    PassageText = table.Column<string>(type: "text", nullable: true),
                    QuizSetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizGroupItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizGroupItems_QuizSets_QuizSetId",
                        column: x => x.QuizSetId,
                        principalTable: "QuizSets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_QuizGroupItemId",
                table: "Quizzes",
                column: "QuizGroupItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizGroupItems_QuizSetId",
                table: "QuizGroupItems",
                column: "QuizSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_QuizGroupItems_QuizGroupItemId",
                table: "Quizzes",
                column: "QuizGroupItemId",
                principalTable: "QuizGroupItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_QuizGroupItems_QuizGroupItemId",
                table: "Quizzes");

            migrationBuilder.DropTable(
                name: "QuizGroupItems");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_QuizGroupItemId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "QuizGroupItemId",
                table: "Quizzes");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<string>(
                name: "GroupId",
                table: "Quizzes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "GroupItems",
                table: "QuizSets",
                type: "hstore",
                nullable: true);
        }
    }
}
