using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeSuspicionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "SuspicionEvents",
                type: "integer",
                nullable: true);
                
            migrationBuilder.Sql("UPDATE \"SuspicionEvents\" SET \"GameId\" = \"Participations\".\"GameId\" FROM \"Participations\" WHERE \"SuspicionEvents\".\"ParticipationId\" = \"Participations\".\"Id\"");

            migrationBuilder.AlterColumn<int>(
                name: "GameId",
                table: "SuspicionEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RelatedParticipationId",
                table: "SuspicionEvents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuspicionEvents_GameId",
                table: "SuspicionEvents",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspicionEvents_RelatedParticipationId",
                table: "SuspicionEvents",
                column: "RelatedParticipationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SuspicionEvents_Games_GameId",
                table: "SuspicionEvents",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SuspicionEvents_Participations_RelatedParticipationId",
                table: "SuspicionEvents",
                column: "RelatedParticipationId",
                principalTable: "Participations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuspicionEvents_Games_GameId",
                table: "SuspicionEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_SuspicionEvents_Participations_RelatedParticipationId",
                table: "SuspicionEvents");

            migrationBuilder.DropIndex(
                name: "IX_SuspicionEvents_GameId",
                table: "SuspicionEvents");

            migrationBuilder.DropIndex(
                name: "IX_SuspicionEvents_RelatedParticipationId",
                table: "SuspicionEvents");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "SuspicionEvents");

            migrationBuilder.DropColumn(
                name: "RelatedParticipationId",
                table: "SuspicionEvents");
        }
    }
}
