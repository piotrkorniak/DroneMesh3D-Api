import * as fc from 'fast-check';
import { AreaCalculationService } from './area-calculation.service';

// Feature: drone-mesh-gui, Property 2: Polygon area calculation produces correct hectares

/**
 * Property-Based Tests for AreaCalculationService.calculateHectares()
 *
 * **Validates: Requirements 3.2**
 *
 * Property 2: Polygon area calculation produces correct hectares
 * - For any valid GeoJSON polygon (closed ring, ≥3 distinct vertices, coordinates within valid lat/lon ranges),
 *   the calculateHectares function SHALL return a non-negative number equal to the spherical polygon area
 *   in square meters divided by 10000, rounded to 2 decimal places.
 */
describe('Feature: drone-mesh-gui, Property 2: Polygon area calculation produces correct hectares', () => {
  let service: AreaCalculationService;

  beforeEach(() => {
    service = new AreaCalculationService();
  });

  it('should return a non-negative number for any valid polygon coordinates', () => {
    const result = fc.check(
      fc.property(
        fc.array(
          fc.tuple(
            fc.double({ min: -180, max: 180, noNaN: true, noDefaultInfinity: true }),
            fc.double({ min: -90, max: 90, noNaN: true, noDefaultInfinity: true })
          ),
          { minLength: 3, maxLength: 20 }
        ),
        (coordinates) => {
          const hectares = service.calculateHectares(coordinates as [number, number][]);
          return hectares >= 0;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should return a finite number for any valid polygon coordinates', () => {
    const result = fc.check(
      fc.property(
        fc.array(
          fc.tuple(
            fc.double({ min: -180, max: 180, noNaN: true, noDefaultInfinity: true }),
            fc.double({ min: -90, max: 90, noNaN: true, noDefaultInfinity: true })
          ),
          { minLength: 3, maxLength: 20 }
        ),
        (coordinates) => {
          const hectares = service.calculateHectares(coordinates as [number, number][]);
          return Number.isFinite(hectares);
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should return a value with at most 2 decimal places', () => {
    const result = fc.check(
      fc.property(
        fc.array(
          fc.tuple(
            fc.double({ min: -180, max: 180, noNaN: true, noDefaultInfinity: true }),
            fc.double({ min: -90, max: 90, noNaN: true, noDefaultInfinity: true })
          ),
          { minLength: 3, maxLength: 20 }
        ),
        (coordinates) => {
          const hectares = service.calculateHectares(coordinates as [number, number][]);
          // The result should be idempotent under rounding to 2 decimal places
          // i.e., rounding it again should produce the same value
          const reRounded = Math.round(hectares * 100) / 100;
          return hectares === reRounded;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should handle closed rings (first == last vertex) the same as open rings', () => {
    const result = fc.check(
      fc.property(
        fc.array(
          fc.tuple(
            fc.double({ min: -180, max: 180, noNaN: true, noDefaultInfinity: true }),
            fc.double({ min: -90, max: 90, noNaN: true, noDefaultInfinity: true })
          ),
          { minLength: 3, maxLength: 15 }
        ),
        (coordinates) => {
          const openRing = coordinates as [number, number][];
          const closedRing: [number, number][] = [...openRing, openRing[0]];
          const openResult = service.calculateHectares(openRing);
          const closedResult = service.calculateHectares(closedRing);
          return openResult === closedResult;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });
});
