using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AdjustNamingConventions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Attachment_AttachmentId",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_FlagContexts_Attachment_AttachmentId",
                table: "FlagContexts");

            migrationBuilder.DropTable(
                name: "Attachment");

            migrationBuilder.RenameColumn(
                name: "PublicIP",
                table: "Containers",
                newName: "PublicIp");

            migrationBuilder.RenameColumn(
                name: "IP",
                table: "Containers",
                newName: "Ip");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Attachments_AttachmentId",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_FlagContexts_Attachments_AttachmentId",
                table: "FlagContexts");

            migrationBuilder.RenameColumn(
                name: "PublicIp",
                table: "Containers",
                newName: "PublicIP");

            migrationBuilder.RenameColumn(
                name: "Ip",
                table: "Containers",
                newName: "IP");

            migrationBuilder.CreateTable(
                name: "Attachment",
                columns: table => new
                {
                    TempId = table.Column<int>(type: "integer", nullable: false),
                    TempId1 = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.UniqueConstraint("AK_Attachment_TempId", x => x.TempId);
                    table.UniqueConstraint("AK_Attachment_TempId1", x => x.TempId1);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Attachment_AttachmentId",
                table: "Challenges",
                column: "AttachmentId",
                principalTable: "Attachment",
                principalColumn: "TempId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FlagContexts_Attachment_AttachmentId",
                table: "FlagContexts",
                column: "AttachmentId",
                principalTable: "Attachment",
                principalColumn: "TempId1",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
