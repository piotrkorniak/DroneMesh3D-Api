import * as fc from 'fast-check';
import { formatFlightTime } from './format-flight-time';

// Feature: drone-mesh-gui, Property 6: Flight time formatting is correct

/**
 * Property-Based Tests for formatFlightTime utility
 *
 * **Validates: Requirements 5.2**
 *
 * Property 6: Flight time formatting is correct
 * - For any non-negative integer totalSeconds,
 *   formatFlightTime(totalSeconds) === `"${Math.floor(totalSeconds / 60)} min ${totalSeconds % 60} s"`
 */
describe('Feature: drone-mesh-gui, Property 6: Flight time formatting is correct', () => {

  it('should format totalSeconds as "${minutes} min ${seconds} s"', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 0, max: 360000 }),
        (totalSeconds) => {
          const expected = `${Math.floor(totalSeconds / 60)} min ${totalSeconds % 60} s`;
          const actual = formatFlightTime(totalSeconds);
          return actual === expected;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should produce 0 min 0 s for input 0', () => {
    expect(formatFlightTime(0)).toBe('0 min 0 s');
  });

  it('should produce correct formatting for exactly 60 seconds', () => {
    expect(formatFlightTime(60)).toBe('1 min 0 s');
  });

  it('should produce correct formatting for values under 60', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 0, max: 59 }),
        (totalSeconds) => {
          const actual = formatFlightTime(totalSeconds);
          return actual === `0 min ${totalSeconds} s`;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });
});
