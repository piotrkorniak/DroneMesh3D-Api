using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneMesh3D.Core.Migrations;

/// <inheritdoc />
public partial class AddUserAndAuth : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "UserId",
            table: "FlightPlans",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "UserId",
            table: "Areas",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                GoogleId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                AvatarUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FlightPlans_UserId",
            table: "FlightPlans",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Areas_UserId",
            table: "Areas",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_GoogleId",
            table: "Users",
            column: "GoogleId",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Areas_Users_UserId",
            table: "Areas",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_FlightPlans_Users_UserId",
            table: "FlightPlans",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Areas_Users_UserId",
            table: "Areas");

        migrationBuilder.DropForeignKey(
            name: "FK_FlightPlans_Users_UserId",
            table: "FlightPlans");

        migrationBuilder.DropTable(
            name: "Users");

        migrationBuilder.DropIndex(
            name: "IX_FlightPlans_UserId",
            table: "FlightPlans");

        migrationBuilder.DropIndex(
            name: "IX_Areas_UserId",
            table: "Areas");

        migrationBuilder.DropColumn(
            name: "UserId",
            table: "FlightPlans");

        migrationBuilder.DropColumn(
            name: "UserId",
            table: "Areas");
    }
}
