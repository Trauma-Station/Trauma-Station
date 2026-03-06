using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class TraumaSkillProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "knowledge_mastery");

            migrationBuilder.AddColumn<string>(
                name: "knowledge_mastery",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "knowledge_mastery",
                table: "profile");

            migrationBuilder.CreateTable(
                name: "knowledge_mastery",
                columns: table => new
                {
                    skill = table.Column<string>(type: "TEXT", nullable: false),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_mastery", x => x.skill);
                    table.ForeignKey(
                        name: "FK_knowledge_mastery_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_mastery_profile_id",
                table: "knowledge_mastery",
                column: "profile_id");
        }
    }
}
