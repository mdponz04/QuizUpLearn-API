using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizQuizSet");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizQuizSet",
                columns: table => new
                {
                    QuizSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizzesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuizSet", x => new { x.QuizSetId, x.QuizzesId });
                    table.ForeignKey(
                        name: "FK_QuizQuizSet_QuizSets_QuizSetId",
                        column: x => x.QuizSetId,
                        principalTable: "QuizSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizQuizSet_Quizzes_QuizzesId",
                        column: x => x.QuizzesId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuizSet_QuizzesId",
                table: "QuizQuizSet",
                column: "QuizzesId");
        }
    }
}
