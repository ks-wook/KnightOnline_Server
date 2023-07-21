using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class MainStage_StageName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StageName",
                table: "MainStage",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StageName",
                table: "MainStage");
        }
    }
}
