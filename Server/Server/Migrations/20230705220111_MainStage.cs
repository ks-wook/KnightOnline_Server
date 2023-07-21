using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class MainStage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MainStage",
                columns: table => new
                {
                    MainStageDbId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsCleared = table.Column<bool>(nullable: false),
                    OwnerDbId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainStage", x => x.MainStageDbId);
                    table.ForeignKey(
                        name: "FK_MainStage_Player_OwnerDbId",
                        column: x => x.OwnerDbId,
                        principalTable: "Player",
                        principalColumn: "PlayerDbId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MainStage_OwnerDbId",
                table: "MainStage",
                column: "OwnerDbId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MainStage");
        }
    }
}
