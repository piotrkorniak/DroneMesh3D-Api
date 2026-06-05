# Implementation Plan: Flight Path Calculation

## Overview

Implements the flight path calculation engine for DroneMesh3D supporting Grid (Lawnmower) and POI (Orbit) flight modes. The implementation follows the existing MediatR/CQRS architecture with EF Core 10 + PostGIS persistence, OneOf return types, and FsCheck property-based testing.

## Tasks

- [x] 1. Define value objects, enums, and core interfaces
  - [x] 1.1 Create FlightMode enum and value object records
    - Create `Core/FlightPath/FlightMode.cs` enum (Grid, Poi)
    - Create `Core/FlightPath/CameraParameters.cs` sealed record
    - Create `Core/FlightPath/GridModeParameters.cs` sealed record
    - Create `Core/FlightPath/PoiModeParameters.cs` sealed record
    - Create `Core/FlightPath/Waypoint.cs` sealed record
    - Create `Core/FlightPath/FlightStatistics.cs` sealed record
    - Create `Core/FlightPath/FlightPlanResult.cs` sealed record
    - _Requirements: 1.1, 2.1, 3.1, 7.1, 7.2_

  - [x] 1.2 Create IFlightPathCalculator and IFlightPlanRepository interfaces
    - Create `Core/Interfaces/IFlightPathCalculator.cs` with `CalculateGrid` and `CalculatePoi` methods
    - Create `Core/Interfaces/IFlightPlanRepository.cs` with `AddAsync` and `GetByIdAsync` methods
    - _Requirements: 1.1, 8.1, 8.3_

- [x] 2. Implement GeodesicMathService
  - [x] 2.1 Create GeodesicMathService static class with pure geodesic functions
    - Create `Core/FlightPath/GeodesicMathService.cs`
    - Implement `DestinationPoint(lat, lon, bearingDeg, distanceM)` using Haversine/Vincenty
    - Implement `BearingBetween(lat1, lon1, lat2, lon2)`
    - Implement `DistanceBetween(lat1, lon1, lat2, lon2)`
    - Implement `ComputeGsd(altitudeM, sensorWidthMm, focalLengthMm, imageWidthPx)`
    - Implement `ComputePhotoFootprint(gsd, imageWidthPx, imageHeightPx)`
    - Implement `ComputePhotoSpacing(footprintHeightM, frontOverlap)`
    - Implement `ComputeLineSpacing(footprintWidthM, sideOverlap)`
    - Implement `LongestAxisHeading(polygon)` using oriented minimum bounding rectangle
    - _Requirements: 2.2, 2.3, 2.4, 2.5, 2.6, 10.2_

  - [x] 2.2 Write property tests for GSD and spacing calculations
    - **Property 1: GSD and footprint computation correctness**
    - **Property 2: Spacing computation from overlap**
    - **Validates: Requirements 2.2, 2.3, 2.4, 2.5**
    - Create `Tests/Core.Tests/FlightPath/GsdCalculationPropertyTests.cs`
    - Create `Tests/Core.Tests/FlightPath/Arbitraries/CameraParametersArbitrary.cs`

- [x] 3. Implement GridFlightPathStrategy
  - [x] 3.1 Create GridFlightPathStrategy sealed class
    - Create `Core/FlightPath/GridFlightPathStrategy.cs`
    - Compute GSD, photo footprint, photo spacing, and line spacing
    - Determine scan heading (longest axis or user-specified with 0-360° validation)
    - Generate parallel scan lines across the rotated bounding box of the polygon
    - Clip lines to polygon boundary using NTS intersection
    - Distribute waypoints along clipped lines at computed photo spacing
    - Generate turnaround points between lines
    - Set gimbal pitch to nadir (-90°) or user-specified value clamped to [-90°, -45°]
    - Compute flight statistics (total distance, estimated time, photo count, covered area)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 4.1, 4.3, 7.1, 7.2, 7.3_

  - [x] 3.2 Write property tests for grid heading and waypoint containment
    - **Property 3: Grid heading defaults to longest polygon axis**
    - **Property 4: All grid waypoints lie within polygon boundary**
    - **Validates: Requirements 2.6, 2.7**
    - Create `Tests/Core.Tests/FlightPath/GridGeometryPropertyTests.cs`
    - Create `Tests/Core.Tests/FlightPath/Arbitraries/ValidPolygonArbitrary.cs`
    - Create `Tests/Core.Tests/FlightPath/Arbitraries/GridModeParametersArbitrary.cs`

- [x] 4. Implement PoiFlightPathStrategy
  - [x] 4.1 Create PoiFlightPathStrategy sealed class
    - Create `Core/FlightPath/PoiFlightPathStrategy.cs`
    - Distribute waypoints equally on a circle (360° / photoCount)
    - Compute geographic position from center + radius + bearing using GeodesicMathService
    - Set gimbal yaw toward center point for each waypoint
    - Compute gimbal pitch from geometry (radius, altitude, structure height) clamped to [-90°, -45°]
    - If overlap specified instead of photoCount, derive photoCount from FOV and radius
    - Compute flight statistics (total distance, estimated time, photo count, covered area)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.2, 4.3, 6.5, 7.1, 7.2, 7.3_

  - [x] 4.2 Write property tests for POI geometry
    - **Property 5: POI waypoints form equidistant closed orbit**
    - **Property 6: POI gimbal yaw points toward center**
    - **Validates: Requirements 3.2, 3.3, 3.4**
    - Create `Tests/Core.Tests/FlightPath/PoiGeometryPropertyTests.cs`
    - Create `Tests/Core.Tests/FlightPath/Arbitraries/PoiModeParametersArbitrary.cs`

  - [x] 4.3 Write property test for gimbal pitch bounds
    - **Property 7: Gimbal pitch bounded to [-90°, -45°] for all modes**
    - **Validates: Requirements 4.1, 4.2, 4.3**
    - Create `Tests/Core.Tests/FlightPath/GimbalPitchPropertyTests.cs`

- [x] 5. Implement FlightPathCalculator (strategy dispatcher)
  - [x] 5.1 Create FlightPathCalculator that implements IFlightPathCalculator
    - Create `Core/FlightPath/FlightPathCalculator.cs`
    - Inject `GridFlightPathStrategy` and `PoiFlightPathStrategy`
    - Dispatch to appropriate strategy based on FlightMode
    - _Requirements: 1.1, 4.4_

- [x] 6. Checkpoint - Core calculation engine
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Implement validation
  - [x] 7.1 Create CalculateFlightPathCommandValidator
    - Create `Api/Validators/CalculateFlightPathCommandValidator.cs`
    - Validate Mode is defined enum value (Grid or Poi)
    - Validate Grid parameters present when Mode == Grid; Poi parameters present when Mode == Poi
    - Validate altitude: 0 < altitude ≤ 120 m
    - Validate front overlap: 75–80%
    - Validate side overlap: 65–75%
    - Validate heading (if provided): 0–360° (or fallback silently to longest axis)
    - Validate gimbal pitch: -90° to -45°
    - Validate POI radius > 0
    - Validate camera parameters: all positive values
    - Wire validator into existing `ValidationBehavior` MediatR pipeline
    - _Requirements: 1.2, 1.3, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 6.4, 9.4_

  - [x] 7.2 Write property tests for validation boundaries
    - **Property 8: Altitude validation boundary at 120m**
    - **Property 9: Overlap validation boundaries**
    - **Property 12: Invalid parameters produce validation error**
    - **Validates: Requirements 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 6.4, 9.4**
    - Create `Tests/Core.Tests/FlightPath/ValidationPropertyTests.cs`

- [x] 8. Implement persistence layer
  - [x] 8.1 Create FlightPlanEntity and EF Core configuration
    - Create `Core/Entities/FlightPlanEntity.cs` sealed class with all properties (Id, AreaId, Mode, ParametersJson, WaypointsJson, TotalDistanceM, EstimatedFlightTimeS, PhotoCount, CoveredAreaM2, CreatedAt, navigation to AreaEntity)
    - Add `FlightPlanEntity` configuration in `AppDbContext` (HasKey, HasDefaultValueSql for Id and CreatedAt, Mode conversion to string, jsonb column types, FK to AreaEntity with cascade delete)
    - _Requirements: 8.1, 8.2_

  - [x] 8.2 Create FlightPlanRepository
    - Create `Core/Repositories/FlightPlanRepository.cs` implementing `IFlightPlanRepository`
    - Implement `AddAsync` with `SaveChangesAsync`
    - Implement `GetByIdAsync` with eager loading of navigation properties
    - _Requirements: 8.1, 8.3_

  - [x] 8.3 Create EF Core migration for FlightPlanEntity
    - Add EF Core migration `AddFlightPlanEntity`
    - Verify generated migration includes proper indexes and FK constraints
    - _Requirements: 8.1, 8.2_

- [x] 9. Implement MediatR command and query handlers
  - [x] 9.1 Create CalculateFlightPathCommand and CalculateFlightPathCommandHandler
    - Create `Api/Commands/CalculateFlightPathCommand.cs` record implementing `IRequest<OneOf<FlightPlanResponse, ValidationErrorResponse, ErrorResponse>>`
    - Create `Api/Handlers/CalculateFlightPathCommandHandler.cs` with primary constructor injecting `IAreaRepository`, `IFlightPathCalculator`, `IFlightPlanRepository`
    - Load AreaEntity by ID (return 404 if not found)
    - Dispatch to IFlightPathCalculator based on mode
    - Map result to FlightPlanEntity and persist
    - Return `FlightPlanResponse` on success, `ErrorResponse` on DB failure
    - _Requirements: 1.2, 8.1, 8.4, 9.3, 9.4, 9.5, 9.6_

  - [x] 9.2 Create GetFlightPlanQuery and GetFlightPlanQueryHandler
    - Create `Api/Queries/GetFlightPlanQuery.cs` record implementing `IRequest<FlightPlanResponse?>`
    - Create `Api/Handlers/GetFlightPlanQueryHandler.cs` with primary constructor injecting `IFlightPlanRepository`
    - Load FlightPlanEntity and map to response, return null if not found
    - _Requirements: 8.3, 9.2_

- [x] 10. Implement API endpoint and DTOs
  - [x] 10.1 Create request/response DTOs
    - Create `Api/DTOs/CalculateFlightPathRequest.cs` sealed record
    - Create `Api/DTOs/FlightPlanResponse.cs` sealed record (with waypoints list, statistics, createdAt, id, areaId, mode)
    - Create `Api/DTOs/GridModeParametersDto.cs` and `Api/DTOs/PoiModeParametersDto.cs` sealed records
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 10.2 Create FlightPlansEndpoint
    - Create `Api/Endpoints/FlightPlansEndpoint.cs` static class
    - Map `POST /api/flight-plans` dispatching `CalculateFlightPathCommand` via MediatR
    - Map `GET /api/flight-plans/{id:guid}` dispatching `GetFlightPlanQuery` via MediatR
    - Handle OneOf result matching: Created (201), UnprocessableEntity (422), NotFound (404), Problem (500)
    - Register endpoint group in Program.cs
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

- [x] 11. Register services in DI container
  - [x] 11.1 Wire up DI registrations in Program.cs
    - Register `IFlightPathCalculator` → `FlightPathCalculator` as scoped/transient
    - Register `IFlightPlanRepository` → `FlightPlanRepository` as scoped
    - Register `GridFlightPathStrategy` and `PoiFlightPathStrategy`
    - Register validator for `CalculateFlightPathCommand`
    - Call `MapFlightPlansEndpoints()` on the app
    - _Requirements: 9.1, 9.2, 9.3_

- [x] 12. Checkpoint - Full pipeline integration
  - Ensure all tests pass, ask the user if questions arise.

- [x] 13. Property tests for POI photo count and result completeness
  - [x] 13.1 Write property test for POI photo count overlap calculation
    - **Property 10: POI photo count satisfies desired overlap**
    - **Validates: Requirements 6.5**
    - Create `Tests/Core.Tests/FlightPath/PoiPhotoCountPropertyTests.cs`

  - [x] 13.2 Write property test for result completeness
    - **Property 11: Result completeness and coordinate validity**
    - **Validates: Requirements 7.1, 7.2, 7.3, 10.2**
    - Create `Tests/Core.Tests/FlightPath/ResultCompletenessPropertyTests.cs`

- [x] 14. Write unit and integration tests
  - [x] 14.1 Write unit tests for calculation edge cases
    - Test hand-computed reference values for GSD, spacing, and waypoint positions
    - Test boundary conditions: minimum polygon (triangle), max altitude (120m exactly), boundary overlaps
    - Test error conditions: area not found (404), DB failure (500), missing mode
    - Create `Tests/Core.Tests/FlightPath/GeodesicMathServiceTests.cs`
    - Create `Tests/Core.Tests/FlightPath/GridFlightPathStrategyTests.cs`
    - Create `Tests/Core.Tests/FlightPath/PoiFlightPathStrategyTests.cs`
    - _Requirements: 2.2, 2.3, 2.4, 2.5, 3.2, 5.1, 5.2_

  - [x] 14.2 Write integration tests for API endpoints
    - Create `Tests/Api.Tests/Integration/FlightPlansEndpointTests.cs`
    - Test POST /api/flight-plans with valid Grid request → 201
    - Test POST /api/flight-plans with valid POI request → 201
    - Test POST with invalid parameters → 422
    - Test POST with non-existent area → 404
    - Test GET /api/flight-plans/{id} → 200 with saved plan
    - Test GET with non-existent ID → 404
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 8.3_

- [x] 15. Final checkpoint - All tests green
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The GeodesicMathService is a static class with pure functions — no DI needed
- Strategy pattern keeps Grid and POI calculations isolated and independently testable
- All value objects use sealed records with primary constructors per project conventions
- OneOf return types follow the existing CreateAreaCommand pattern

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["2.1", "8.1"] },
    { "id": 2, "tasks": ["2.2", "3.1", "8.2", "8.3"] },
    { "id": 3, "tasks": ["3.2", "4.1", "7.1"] },
    { "id": 4, "tasks": ["4.2", "4.3", "5.1", "7.2"] },
    { "id": 5, "tasks": ["9.1", "9.2", "10.1"] },
    { "id": 6, "tasks": ["10.2", "11.1"] },
    { "id": 7, "tasks": ["13.1", "13.2", "14.1"] },
    { "id": 8, "tasks": ["14.2"] }
  ]
}
```
