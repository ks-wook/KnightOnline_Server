using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class MainStage_isUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MainStage_MainStageDbId",
                table: "MainStage",
                column: "MainStageDbId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MainStage_MainStageDbId",
                table: "MainStage");
        }
    }
}
