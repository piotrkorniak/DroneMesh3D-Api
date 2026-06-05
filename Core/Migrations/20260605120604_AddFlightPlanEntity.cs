using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneMesh3D.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightPlanEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlightPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false),
                    WaypointsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TotalDistanceM = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedFlightTimeS = table.Column<double>(type: "double precision", nullable: false),
                    PhotoCount = table.Column<int>(type: "integer", nullable: false),
                    CoveredAreaM2 = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightPlans_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlightPlans_AreaId",
                table: "FlightPlans",
                column: "AreaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightPlans");
        }
    }
}
