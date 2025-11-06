using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizGroupItems_QuizSets_QuizSetId",
                table: "QuizGroupItems");

            migrationBuilder.DropColumn(
                name: "QuizSet",
                table: "QuizGroupItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "QuizSetId",
                table: "QuizGroupItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizGroupItems_QuizSets_QuizSetId",
                table: "QuizGroupItems",
                column: "QuizSetId",
                principalTable: "QuizSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizGroupItems_QuizSets_QuizSetId",
                table: "QuizGroupItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "QuizSetId",
                table: "QuizGroupItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "QuizSet",
                table: "QuizGroupItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddForeignKey(
                name: "FK_QuizGroupItems_QuizSets_QuizSetId",
                table: "QuizGroupItems",
                column: "QuizSetId",
                principalTable: "QuizSets",
                principalColumn: "Id");
        }
    }
}
