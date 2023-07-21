using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class MainStage_Del_isCleared : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCleared",
                table: "MainStage");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCleared",
                table: "MainStage",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
