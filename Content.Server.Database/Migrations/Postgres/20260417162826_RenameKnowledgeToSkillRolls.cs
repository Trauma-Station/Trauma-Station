using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RenameKnowledgeToSkillRolls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "knowledge_mastery",
                table: "profile",
                newName: "skill_rolls");

            migrationBuilder.AddColumn<string>(
                name: "attribute_purchases",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attribute_purchases",
                table: "profile");

            migrationBuilder.RenameColumn(
                name: "skill_rolls",
                table: "profile",
                newName: "knowledge_mastery");
        }
    }
}
