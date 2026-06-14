using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneMesh3D.Core.Migrations;

/// <inheritdoc />
public partial class RemoveUserIdFromFlightPlan : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_FlightPlans_Users_UserId",
            table: "FlightPlans");

        migrationBuilder.DropIndex(
            name: "IX_FlightPlans_UserId",
            table: "FlightPlans");

        migrationBuilder.DropColumn(
            name: "UserId",
            table: "FlightPlans");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "UserId",
            table: "FlightPlans",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateIndex(
            name: "IX_FlightPlans_UserId",
            table: "FlightPlans",
            column: "UserId");

        migrationBuilder.AddForeignKey(
            name: "FK_FlightPlans_Users_UserId",
            table: "FlightPlans",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
