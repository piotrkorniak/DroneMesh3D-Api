using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneMesh3D.Core.Migrations;

/// <inheritdoc />
public partial class ReplaceJsonStringsWithOwnedTypes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Rename WaypointsJson → Waypoints (data stays intact, same jsonb format)
        migrationBuilder.RenameColumn(
            name: "WaypointsJson",
            table: "FlightPlans",
            newName: "Waypoints");

        // 2. Add new nullable columns for split parameters
        migrationBuilder.AddColumn<string>(
            name: "GridParameters",
            table: "FlightPlans",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PoiParameters",
            table: "FlightPlans",
            type: "jsonb",
            nullable: true);

        // 3. Migrate data: copy ParametersJson into the correct column based on Mode
        migrationBuilder.Sql("""
            UPDATE "FlightPlans"
            SET "GridParameters" = "ParametersJson"
            WHERE "Mode" = 'Grid';
            """);

        migrationBuilder.Sql("""
            UPDATE "FlightPlans"
            SET "PoiParameters" = "ParametersJson"
            WHERE "Mode" = 'Poi';
            """);

        // 4. Drop the old ParametersJson column
        migrationBuilder.DropColumn(
            name: "ParametersJson",
            table: "FlightPlans");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 1. Re-create ParametersJson column
        migrationBuilder.AddColumn<string>(
            name: "ParametersJson",
            table: "FlightPlans",
            type: "jsonb",
            nullable: false,
            defaultValue: "");

        // 2. Merge GridParameters/PoiParameters back into ParametersJson
        migrationBuilder.Sql("""
            UPDATE "FlightPlans"
            SET "ParametersJson" = "GridParameters"
            WHERE "Mode" = 'Grid' AND "GridParameters" IS NOT NULL;
            """);

        migrationBuilder.Sql("""
            UPDATE "FlightPlans"
            SET "ParametersJson" = "PoiParameters"
            WHERE "Mode" = 'Poi' AND "PoiParameters" IS NOT NULL;
            """);

        // 3. Drop new columns
        migrationBuilder.DropColumn(
            name: "GridParameters",
            table: "FlightPlans");

        migrationBuilder.DropColumn(
            name: "PoiParameters",
            table: "FlightPlans");

        // 4. Rename Waypoints back to WaypointsJson
        migrationBuilder.RenameColumn(
            name: "Waypoints",
            table: "FlightPlans",
            newName: "WaypointsJson");
    }
}
