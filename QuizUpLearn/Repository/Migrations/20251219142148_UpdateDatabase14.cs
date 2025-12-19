using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "QuizSets");

            migrationBuilder.DropColumn(
                name: "IsAIGenerated",
                table: "QuizSets");

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "Quizzes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAIGenerated",
                table: "Quizzes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "IsAIGenerated",
                table: "Quizzes");

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "QuizSets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAIGenerated",
                table: "QuizSets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
