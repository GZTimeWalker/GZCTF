using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class NewConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "ExerciseInstances");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "ExerciseChallenges");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "GameInstances",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "GameChallenges",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ExerciseInstances",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ExerciseChallenges",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ExerciseInstances");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ExerciseChallenges");

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "GameInstances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "GameChallenges",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "ExerciseInstances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "ExerciseChallenges",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
