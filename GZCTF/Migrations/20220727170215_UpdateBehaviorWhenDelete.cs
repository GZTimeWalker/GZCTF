using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class UpdateBehaviorWhenDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Attachments_AttachmentId",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_FlagContexts_Attachments_AttachmentId",
                table: "FlagContexts");

            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Participations_ParticipationId",
                table: "Instances");

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Attachments_AttachmentId",
                table: "Challenges",
                column: "AttachmentId",
                principalTable: "Attachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FlagContexts_Attachments_AttachmentId",
                table: "FlagContexts",
                column: "AttachmentId",
                principalTable: "Attachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Participations_ParticipationId",
                table: "Instances",
                column: "ParticipationId",
                principalTable: "Participations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Attachments_AttachmentId",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_FlagContexts_Attachments_AttachmentId",
                table: "FlagContexts");

            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Participations_ParticipationId",
                table: "Instances");

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Attachments_AttachmentId",
                table: "Challenges",
                column: "AttachmentId",
                principalTable: "Attachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FlagContexts_Attachments_AttachmentId",
                table: "FlagContexts",
                column: "AttachmentId",
                principalTable: "Attachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Participations_ParticipationId",
                table: "Instances",
                column: "ParticipationId",
                principalTable: "Participations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
