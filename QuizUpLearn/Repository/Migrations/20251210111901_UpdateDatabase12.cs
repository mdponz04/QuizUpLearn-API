using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizGroupItems_QuizSets_QuizSetId",
                table: "QuizGroupItems");

            migrationBuilder.DropIndex(
                name: "IX_QuizGroupItems_QuizSetId",
                table: "QuizGroupItems");

            migrationBuilder.DropColumn(
                name: "QuizSetId",
                table: "QuizGroupItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "QuizSetId",
                table: "QuizGroupItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_QuizGroupItems_QuizSetId",
                table: "QuizGroupItems",
                column: "QuizSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizGroupItems_QuizSets_QuizSetId",
                table: "QuizGroupItems",
                column: "QuizSetId",
                principalTable: "QuizSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
