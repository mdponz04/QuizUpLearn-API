using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "QuizSetId",
                table: "Events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Events_QuizSetId",
                table: "Events",
                column: "QuizSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_QuizSets_QuizSetId",
                table: "Events",
                column: "QuizSetId",
                principalTable: "QuizSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_QuizSets_QuizSetId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_QuizSetId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "QuizSetId",
                table: "Events");
        }
    }
}
