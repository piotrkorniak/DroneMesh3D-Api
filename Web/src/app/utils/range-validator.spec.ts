import * as fc from 'fast-check';
import { FormControl } from '@angular/forms';
import { rangeValidator } from './range-validator';

// Feature: drone-mesh-gui, Property 4: Numeric range validation correctly classifies values

/**
 * Property-Based Tests for rangeValidator utility
 *
 * **Validates: Requirements 4.3, 4.4**
 *
 * Property 4: Numeric range validation correctly classifies values
 * - For any value v and range [min, max] where min < max:
 *   valid (returns null) when min <= v <= max,
 *   invalid (returns error object) otherwise.
 */
describe('Feature: drone-mesh-gui, Property 4: Numeric range validation correctly classifies values', () => {

  it('should return null (valid) when min <= value <= max', () => {
    const result = fc.check(
      fc.property(
        fc.double({ min: -1e6, max: 1e6, noNaN: true, noDefaultInfinity: true }).chain(min =>
          fc.double({ min: min + 0.001, max: min + 1e6, noNaN: true, noDefaultInfinity: true }).chain(max =>
            fc.double({ min, max, noNaN: true, noDefaultInfinity: true }).map(value => ({ min, max, value }))
          )
        ),
        ({ min, max, value }) => {
          const validator = rangeValidator(min, max);
          const control = new FormControl(value);
          const errors = validator(control);
          return errors === null;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should return invalid when value < min', () => {
    const result = fc.check(
      fc.property(
        fc.double({ min: -1e6, max: 1e6, noNaN: true, noDefaultInfinity: true }).chain(min =>
          fc.double({ min: min + 0.001, max: min + 1e6, noNaN: true, noDefaultInfinity: true }).chain(max =>
            fc.double({ min: min - 1e6, max: min - 0.001, noNaN: true, noDefaultInfinity: true }).map(value => ({ min, max, value }))
          )
        ),
        ({ min, max, value }) => {
          const validator = rangeValidator(min, max);
          const control = new FormControl(value);
          const errors = validator(control);
          return errors !== null && errors['range'] !== undefined;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should return invalid when value > max', () => {
    const result = fc.check(
      fc.property(
        fc.double({ min: -1e6, max: 1e6, noNaN: true, noDefaultInfinity: true }).chain(min =>
          fc.double({ min: min + 0.001, max: min + 1e6, noNaN: true, noDefaultInfinity: true }).chain(max =>
            fc.double({ min: max + 0.001, max: max + 1e6, noNaN: true, noDefaultInfinity: true }).map(value => ({ min, max, value }))
          )
        ),
        ({ min, max, value }) => {
          const validator = rangeValidator(min, max);
          const control = new FormControl(value);
          const errors = validator(control);
          return errors !== null && errors['range'] !== undefined;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should return null (valid) for boundary values min and max themselves', () => {
    const result = fc.check(
      fc.property(
        fc.double({ min: -1e6, max: 1e6, noNaN: true, noDefaultInfinity: true }).chain(min =>
          fc.double({ min: min + 0.001, max: min + 1e6, noNaN: true, noDefaultInfinity: true }).map(max => ({ min, max }))
        ),
        ({ min, max }) => {
          const validator = rangeValidator(min, max);
          const controlMin = new FormControl(min);
          const controlMax = new FormControl(max);
          return validator(controlMin) === null && validator(controlMax) === null;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });
});
