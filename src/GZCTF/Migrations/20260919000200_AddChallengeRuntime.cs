using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserChallengeInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StoppedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAttachmentKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AssignedAttachmentSha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AssignedFlag = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RuntimeMetadataJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChallengeInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserChallengeInstances_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserChallengeInstances_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeHelpUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    HintId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsWriteup = table.Column<bool>(type: "boolean", nullable: false),
                    ViewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeHelpUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeHelpUsages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChallengeHelpUsages_ChallengeHints_HintId",
                        column: x => x.HintId,
                        principalTable: "ChallengeHints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChallengeHelpUsages_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChallengeHelpUsages_UserChallengeInstances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "UserChallengeInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedFlagHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Accepted = table.Column<bool>(type: "boolean", nullable: false),
                    FirstSolve = table.Column<bool>(type: "boolean", nullable: false),
                    SolveMode = table.Column<byte>(type: "smallint", nullable: true),
                    RejectionCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChallengeSubmissions_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChallengeSubmissions_UserChallengeInstances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "UserChallengeInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeHelpUsages_ChallengeId",
                table: "ChallengeHelpUsages",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeHelpUsages_HintId",
                table: "ChallengeHelpUsages",
                column: "HintId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeHelpUsages_InstanceId",
                table: "ChallengeHelpUsages",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeHelpUsages_UserId_ChallengeId",
                table: "ChallengeHelpUsages",
                columns: new[] { "UserId", "ChallengeId" },
                unique: true,
                filter: "\"IsWriteup\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeHelpUsages_UserId_ChallengeId_ViewedAtUtc",
                table: "ChallengeHelpUsages",
                columns: new[] { "UserId", "ChallengeId", "ViewedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_ChallengeId_Accepted_SubmittedAtUtc",
                table: "ChallengeSubmissions",
                columns: new[] { "ChallengeId", "Accepted", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_InstanceId",
                table: "ChallengeSubmissions",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_UserId_ChallengeId_SubmittedAtUtc",
                table: "ChallengeSubmissions",
                columns: new[] { "UserId", "ChallengeId", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserChallengeInstances_ChallengeId",
                table: "UserChallengeInstances",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChallengeInstances_UserId_ChallengeId",
                table: "UserChallengeInstances",
                columns: new[] { "UserId", "ChallengeId" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_UserChallengeInstances_UserId_CreatedAtUtc",
                table: "UserChallengeInstances",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeHelpUsages");

            migrationBuilder.DropTable(
                name: "ChallengeSubmissions");

            migrationBuilder.DropTable(
                name: "UserChallengeInstances");
        }
    }
}
