using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneMesh3D.Core.Migrations;

/// <inheritdoc />
public partial class AddPhotoBatchEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PhotoBatches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                FlightPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                PhotoCount = table.Column<int>(type: "integer", nullable: false),
                TotalSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                BucketPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PhotoBatches", x => x.Id);
                table.ForeignKey(
                    name: "FK_PhotoBatches_FlightPlans_FlightPlanId",
                    column: x => x.FlightPlanId,
                    principalTable: "FlightPlans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PhotoBatches_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PhotoBatches_FlightPlanId",
            table: "PhotoBatches",
            column: "FlightPlanId");

        migrationBuilder.CreateIndex(
            name: "IX_PhotoBatches_UserId",
            table: "PhotoBatches",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PhotoBatches");
    }
}
