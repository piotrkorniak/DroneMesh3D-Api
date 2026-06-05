import * as fc from 'fast-check';
import { computeFocusTrap } from './focus-trap.directive';

// Feature: drone-mesh-gui, Property 8: Focus trap cycles within modal boundaries

/**
 * Property-Based Tests for focus trap cycling within modal boundaries
 *
 * **Validates: Requirements 7.10, 11.5**
 *
 * Property 8: Focus trap cycles within modal boundaries
 * - For any modal dialog containing K >= 1 focusable elements,
 *   pressing Tab when the last focusable element is focused SHALL move focus
 *   to the first focusable element, and pressing Shift+Tab when the first
 *   focusable element is focused SHALL move focus to the last focusable element.
 */
describe('Feature: drone-mesh-gui, Property 8: Focus trap cycles within modal boundaries', () => {

  it('Tab at last index wraps to first index (0)', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 1, max: 20 }),
        (elementCount) => {
          const lastIndex = elementCount - 1;
          const nextIndex = computeFocusTrap(elementCount, lastIndex, 'Tab');
          return nextIndex === 0;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('Shift+Tab at first index wraps to last index (elementCount - 1)', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 1, max: 20 }),
        (elementCount) => {
          const nextIndex = computeFocusTrap(elementCount, 0, 'Shift+Tab');
          return nextIndex === elementCount - 1;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('Tab at any non-last index moves to index + 1', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 2, max: 20 }).chain((elementCount) =>
          fc.record({
            elementCount: fc.constant(elementCount),
            currentIndex: fc.integer({ min: 0, max: elementCount - 2 }),
          })
        ),
        ({ elementCount, currentIndex }) => {
          const nextIndex = computeFocusTrap(elementCount, currentIndex, 'Tab');
          return nextIndex === currentIndex + 1;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('Shift+Tab at any non-first index moves to index - 1', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 2, max: 20 }).chain((elementCount) =>
          fc.record({
            elementCount: fc.constant(elementCount),
            currentIndex: fc.integer({ min: 1, max: elementCount - 1 }),
          })
        ),
        ({ elementCount, currentIndex }) => {
          const nextIndex = computeFocusTrap(elementCount, currentIndex, 'Shift+Tab');
          return nextIndex === currentIndex - 1;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('focus index always stays within valid range [0, elementCount - 1]', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 1, max: 20 }).chain((elementCount) =>
          fc.record({
            elementCount: fc.constant(elementCount),
            currentIndex: fc.integer({ min: 0, max: elementCount - 1 }),
            key: fc.constantFrom('Tab' as const, 'Shift+Tab' as const),
          })
        ),
        ({ elementCount, currentIndex, key }) => {
          const nextIndex = computeFocusTrap(elementCount, currentIndex, key);
          return nextIndex >= 0 && nextIndex < elementCount;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('Tab followed by Shift+Tab returns to original index (round-trip)', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 1, max: 20 }).chain((elementCount) =>
          fc.record({
            elementCount: fc.constant(elementCount),
            currentIndex: fc.integer({ min: 0, max: elementCount - 1 }),
          })
        ),
        ({ elementCount, currentIndex }) => {
          const afterTab = computeFocusTrap(elementCount, currentIndex, 'Tab');
          const afterShiftTab = computeFocusTrap(elementCount, afterTab, 'Shift+Tab');
          return afterShiftTab === currentIndex;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('pressing Tab elementCount times returns to the original index (full cycle)', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 1, max: 20 }).chain((elementCount) =>
          fc.record({
            elementCount: fc.constant(elementCount),
            startIndex: fc.integer({ min: 0, max: elementCount - 1 }),
          })
        ),
        ({ elementCount, startIndex }) => {
          let index = startIndex;
          for (let i = 0; i < elementCount; i++) {
            index = computeFocusTrap(elementCount, index, 'Tab');
          }
          return index === startIndex;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

});
