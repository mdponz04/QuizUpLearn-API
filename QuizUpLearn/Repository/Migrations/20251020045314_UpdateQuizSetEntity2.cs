using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuizSetEntity2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TOEICPart",
                table: "QuizSets");

            migrationBuilder.DropColumn(
                name: "TimeLimit",
                table: "QuizSets");

            migrationBuilder.DropColumn(
                name: "TotalQuestions",
                table: "QuizSets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TOEICPart",
                table: "QuizSets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TimeLimit",
                table: "QuizSets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestions",
                table: "QuizSets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
