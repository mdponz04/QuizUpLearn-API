using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_QuizSets_QuizSetId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_QuizSetId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "QuizSetId",
                table: "Quizzes");

            migrationBuilder.AddColumn<Guid>(
                name: "GrammarId",
                table: "Quizzes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VocabularyId",
                table: "Quizzes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Grammars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Tense = table.Column<string>(type: "text", nullable: true),
                    GrammarDifficulty = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grammars", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "QuizQuizSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuizSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizQuizSets_QuizSets_QuizSetId",
                        column: x => x.QuizSetId,
                        principalTable: "QuizSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizQuizSets_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vocabularies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyWord = table.Column<string>(type: "text", nullable: false),
                    VocabularyDifficulty = table.Column<int>(type: "integer", nullable: false),
                    ToeicPart = table.Column<string>(type: "text", nullable: true),
                    PassageType = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vocabularies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_GrammarId",
                table: "Quizzes",
                column: "GrammarId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_VocabularyId",
                table: "Quizzes",
                column: "VocabularyId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuizSet_QuizzesId",
                table: "QuizQuizSet",
                column: "QuizzesId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuizSets_QuizId",
                table: "QuizQuizSets",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuizSets_QuizSetId",
                table: "QuizQuizSets",
                column: "QuizSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Grammars_GrammarId",
                table: "Quizzes",
                column: "GrammarId",
                principalTable: "Grammars",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Vocabularies_VocabularyId",
                table: "Quizzes",
                column: "VocabularyId",
                principalTable: "Vocabularies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Grammars_GrammarId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Vocabularies_VocabularyId",
                table: "Quizzes");

            migrationBuilder.DropTable(
                name: "Grammars");

            migrationBuilder.DropTable(
                name: "QuizQuizSet");

            migrationBuilder.DropTable(
                name: "QuizQuizSets");

            migrationBuilder.DropTable(
                name: "Vocabularies");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_GrammarId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_VocabularyId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "GrammarId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "VocabularyId",
                table: "Quizzes");

            migrationBuilder.AddColumn<Guid>(
                name: "QuizSetId",
                table: "Quizzes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_QuizSetId",
                table: "Quizzes",
                column: "QuizSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_QuizSets_QuizSetId",
                table: "Quizzes",
                column: "QuizSetId",
                principalTable: "QuizSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
