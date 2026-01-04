using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_badgeRules_Badges_BadgeId",
                table: "badgeRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_badgeRules",
                table: "badgeRules");

            migrationBuilder.RenameTable(
                name: "badgeRules",
                newName: "BadgeRules");

            migrationBuilder.RenameIndex(
                name: "IX_badgeRules_BadgeId",
                table: "BadgeRules",
                newName: "IX_BadgeRules_BadgeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BadgeRules",
                table: "BadgeRules",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BadgeRules_Badges_BadgeId",
                table: "BadgeRules",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BadgeRules_Badges_BadgeId",
                table: "BadgeRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BadgeRules",
                table: "BadgeRules");

            migrationBuilder.RenameTable(
                name: "BadgeRules",
                newName: "badgeRules");

            migrationBuilder.RenameIndex(
                name: "IX_BadgeRules_BadgeId",
                table: "badgeRules",
                newName: "IX_badgeRules_BadgeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_badgeRules",
                table: "badgeRules",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_badgeRules_Badges_BadgeId",
                table: "badgeRules",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
