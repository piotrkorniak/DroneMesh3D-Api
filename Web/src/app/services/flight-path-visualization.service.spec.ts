import * as fc from 'fast-check';
import { TestBed } from '@angular/core/testing';
import { fromLonLat } from 'ol/proj';
import Point from 'ol/geom/Point';
import LineString from 'ol/geom/LineString';

import { FlightPathVisualizationService } from './flight-path-visualization.service';
import { SelectionStateService } from './selection-state.service';
import { WaypointDto } from '../api/models/waypoint-dto';

// Feature: drone-mesh-gui, Property 7: Flight path visualization preserves waypoint count, order, and labels

/**
 * Property-Based Tests for FlightPathVisualizationService
 *
 * **Validates: Requirements 6.2, 6.3**
 *
 * Property 7: Flight path visualization preserves waypoint count, order, and labels
 * - For any array of N waypoints (N >= 1), the Flight_Path_Visualization SHALL create
 *   exactly N point features with labels numbered 1 through N (in the original array order),
 *   and exactly one LineString feature with N coordinate pairs matching the input waypoint
 *   coordinates (transformed to map projection) in the same sequence.
 */
describe('Feature: drone-mesh-gui, Property 7: Flight path visualization preserves waypoint count, order, and labels', () => {
  let service: FlightPathVisualizationService;

  /** Generator for valid WaypointDto arrays with at least 1 element */
  const waypointArbitrary = fc.record({
    latitude: fc.double({ min: -85, max: 85, noNaN: true }),
    longitude: fc.double({ min: -180, max: 180, noNaN: true }),
    altitudeAglM: fc.double({ min: 0, max: 500, noNaN: true }),
    gimbalPitchDegrees: fc.double({ min: -90, max: 0, noNaN: true }),
    gimbalYawDegrees: fc.double({ min: -180, max: 180, noNaN: true }),
  });

  const waypointsArbitrary = fc.array(waypointArbitrary, { minLength: 1, maxLength: 50 });

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        FlightPathVisualizationService,
        SelectionStateService,
      ],
    });
    service = TestBed.inject(FlightPathVisualizationService);
  });

  afterEach(() => {
    service.clearFlightPath();
  });

  it('should create exactly N point features plus 1 LineString feature for N waypoints', () => {
    const result = fc.check(
      fc.property(
        waypointsArbitrary,
        (waypoints: WaypointDto[]) => {
          service.clearFlightPath();
          service.renderFlightPath(waypoints);

          const features = service.flightPathSource.getFeatures();
          const pointFeatures = features.filter(f => f.getGeometry() instanceof Point);
          const lineFeatures = features.filter(f => f.getGeometry() instanceof LineString);

          // Exactly N point features + 1 LineString = N+1 total features
          return features.length === waypoints.length + 1 &&
            pointFeatures.length === waypoints.length &&
            lineFeatures.length === 1;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(result.counterexample ? JSON.stringify(result.counterexample) : '').toBeFalse();
  });

  it('should label point features with numbers 1 through N in order', () => {
    const result = fc.check(
      fc.property(
        waypointsArbitrary,
        (waypoints: WaypointDto[]) => {
          service.clearFlightPath();
          service.renderFlightPath(waypoints);

          const features = service.flightPathSource.getFeatures();
          const pointFeatures = features
            .filter(f => f.getGeometry() instanceof Point)
            .sort((a, b) => (a.get('waypointIndex') as number) - (b.get('waypointIndex') as number));

          // Must have exactly N point features
          if (pointFeatures.length !== waypoints.length) return false;

          // Check that labels are numbered 1 through N in order
          for (let i = 0; i < pointFeatures.length; i++) {
            const waypointIndex = pointFeatures[i].get('waypointIndex');
            if (waypointIndex !== i + 1) {
              return false;
            }
          }
          return true;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(result.counterexample ? JSON.stringify(result.counterexample) : '').toBeFalse();
  });

  it('should create a LineString with exactly N coordinate pairs matching transformed waypoint coordinates in sequence', () => {
    const result = fc.check(
      fc.property(
        waypointsArbitrary,
        (waypoints: WaypointDto[]) => {
          service.clearFlightPath();
          service.renderFlightPath(waypoints);

          const features = service.flightPathSource.getFeatures();
          const lineFeature = features.find(f => f.getGeometry() instanceof LineString);
          if (!lineFeature) return false;

          const lineGeom = lineFeature.getGeometry() as LineString;
          const coordinates = lineGeom.getCoordinates();

          // LineString should have exactly N coordinate pairs
          if (coordinates.length !== waypoints.length) return false;

          // Each coordinate should match the transformed input (EPSG:4326 → EPSG:3857)
          for (let i = 0; i < waypoints.length; i++) {
            const expected = fromLonLat([waypoints[i].longitude, waypoints[i].latitude]) as [number, number];
            const actual = coordinates[i];

            // Allow floating-point tolerance
            if (Math.abs(actual[0] - expected[0]) > 0.01 ||
                Math.abs(actual[1] - expected[1]) > 0.01) {
              return false;
            }
          }
          return true;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(result.counterexample ? JSON.stringify(result.counterexample) : '').toBeFalse();
  });

  it('should result in 0 features after clearFlightPath()', () => {
    const result = fc.check(
      fc.property(
        waypointsArbitrary,
        (waypoints: WaypointDto[]) => {
          service.clearFlightPath();
          service.renderFlightPath(waypoints);

          // Verify features exist before clear
          const beforeClear = service.flightPathSource.getFeatures().length;
          if (beforeClear === 0) return false;

          service.clearFlightPath();
          return service.flightPathSource.getFeatures().length === 0;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(result.counterexample ? JSON.stringify(result.counterexample) : '').toBeFalse();
  });

  it('should transform coordinates from EPSG:4326 to EPSG:3857 for point features', () => {
    const result = fc.check(
      fc.property(
        waypointsArbitrary,
        (waypoints: WaypointDto[]) => {
          service.clearFlightPath();
          service.renderFlightPath(waypoints);

          const features = service.flightPathSource.getFeatures();
          const pointFeatures = features
            .filter(f => f.getGeometry() instanceof Point)
            .sort((a, b) => (a.get('waypointIndex') as number) - (b.get('waypointIndex') as number));

          if (pointFeatures.length !== waypoints.length) return false;

          for (let i = 0; i < waypoints.length; i++) {
            const expected = fromLonLat([waypoints[i].longitude, waypoints[i].latitude]) as [number, number];
            const pointGeom = pointFeatures[i].getGeometry() as Point;
            const actual = pointGeom.getCoordinates();

            // Allow floating-point tolerance
            if (Math.abs(actual[0] - expected[0]) > 0.01 ||
                Math.abs(actual[1] - expected[1]) > 0.01) {
              return false;
            }
          }
          return true;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(result.counterexample ? JSON.stringify(result.counterexample) : '').toBeFalse();
  });
});
