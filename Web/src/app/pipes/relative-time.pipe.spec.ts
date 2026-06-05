import * as fc from 'fast-check';
import { RelativeTimePipe } from './relative-time.pipe';

// Feature: drone-mesh-gui, Property 3: Relative time formatting follows locale rules

/**
 * Property-Based Tests for RelativeTimePipe.transform()
 *
 * **Validates: Requirements 3.2**
 *
 * Property 3: Relative time formatting follows locale rules
 * - For any ISO date string representing a date within the last 30 days relative to "now",
 *   the transform() SHALL return a string matching the pattern of relative time
 *   (containing a numeric value and a time unit).
 * - For any ISO date string representing a date older than 30 days, the result SHALL match
 *   the DD.MM.YYYY format (2 digits, dot separator, 4-digit year).
 */
describe('Feature: drone-mesh-gui, Property 3: Relative time formatting follows locale rules', () => {
  let pipe: RelativeTimePipe;

  beforeEach(() => {
    pipe = new RelativeTimePipe();
  });

  /**
   * Generator that produces a timestamp offset (in ms) representing a date
   * within the last 28 days (well within the 30-day relative time window).
   * Using integer offsets from "now" avoids Invalid Date issues.
   */
  const recentOffsetArb = fc.integer({
    min: 2 * 60 * 1000,            // at least 2 minutes ago
    max: 28 * 24 * 60 * 60 * 1000  // up to 28 days ago
  });

  /**
   * Generator that produces a timestamp offset (in ms) representing a date
   * older than 35 days (well outside the 30-day relative time window).
   */
  const oldOffsetArb = fc.integer({
    min: 35 * 24 * 60 * 60 * 1000, // at least 35 days ago
    max: 365 * 24 * 60 * 60 * 1000 // up to 1 year ago
  });

  it('should return a string containing digits for dates within the last 30 days', () => {
    const result = fc.check(
      fc.property(
        recentOffsetArb,
        (offsetMs) => {
          const date = new Date(Date.now() - offsetMs);
          const isoString = date.toISOString();
          const output = pipe.transform(isoString);
          // Relative time output should contain at least one digit
          return /\d/.test(output);
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should return a non-empty string for dates within the last 30 days', () => {
    const result = fc.check(
      fc.property(
        recentOffsetArb,
        (offsetMs) => {
          const date = new Date(Date.now() - offsetMs);
          const isoString = date.toISOString();
          const output = pipe.transform(isoString);
          return output.length > 0;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should return DD.MM.YYYY format for dates older than 30 days', () => {
    const result = fc.check(
      fc.property(
        oldOffsetArb,
        (offsetMs) => {
          const date = new Date(Date.now() - offsetMs);
          const isoString = date.toISOString();
          const output = pipe.transform(isoString);
          // Should match DD.MM.YYYY pattern
          return /^\d{2}\.\d{2}\.\d{4}$/.test(output);
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should produce correct day and month values in DD.MM.YYYY format for old dates', () => {
    const result = fc.check(
      fc.property(
        oldOffsetArb,
        (offsetMs) => {
          const date = new Date(Date.now() - offsetMs);
          const isoString = date.toISOString();
          const output = pipe.transform(isoString);
          const match = output.match(/^(\d{2})\.(\d{2})\.(\d{4})$/);
          if (!match) return false;
          const day = parseInt(match[1], 10);
          const month = parseInt(match[2], 10);
          const year = parseInt(match[3], 10);
          // Validate day, month, year are reasonable
          return day >= 1 && day <= 31 && month >= 1 && month <= 12 && year >= 1900 && year <= 2100;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });
});
