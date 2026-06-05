import * as fc from 'fast-check';
import { computeKeyboardNav } from './list-keyboard-nav.directive';

// Feature: drone-mesh-gui, Property 5: Keyboard list navigation cycles correctly

/**
 * Property-Based Tests for keyboard list navigation
 *
 * **Validates: Requirements 3.9, 5.8**
 *
 * Property 5: Keyboard list navigation cycles correctly
 * - For any listbox with N > 0 items and current focus at index i,
 *   pressing ArrowDown SHALL move focus to index (i + 1) % N,
 *   and pressing ArrowUp SHALL move focus to index (i - 1 + N) % N.
 *   Pressing Enter or Space on the focused item SHALL select it.
 */
describe('Feature: drone-mesh-gui, Property 5: Keyboard list navigation cycles correctly', () => {

  it('ArrowDown moves focus to (currentIndex + 1) % listLength', () => {
    const result = fc.check(
      fc.property(
        fc.record({
          listLength: fc.integer({ min: 1, max: 100 }),
          currentIndex: fc.integer({ min: 0 }),
        }),
        ({ listLength, currentIndex }) => {
          const normalizedIndex = currentIndex % listLength;
          const navResult = computeKeyboardNav(listLength, normalizedIndex, 'ArrowDown');
          const expected = (normalizedIndex + 1) % listLength;
          return navResult.focusedIndex === expected && navResult.selected === false;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('ArrowUp moves focus to (currentIndex - 1 + listLength) % listLength', () => {
    const result = fc.check(
      fc.property(
        fc.record({
          listLength: fc.integer({ min: 1, max: 100 }),
          currentIndex: fc.integer({ min: 0 }),
        }),
        ({ listLength, currentIndex }) => {
          const normalizedIndex = currentIndex % listLength;
          const navResult = computeKeyboardNav(listLength, normalizedIndex, 'ArrowUp');
          const expected = (normalizedIndex - 1 + listLength) % listLength;
          return navResult.focusedIndex === expected && navResult.selected === false;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('Enter on focused item selects it without changing focus', () => {
    const result = fc.check(
      fc.property(
        fc.record({
          listLength: fc.integer({ min: 1, max: 100 }),
          currentIndex: fc.integer({ min: 0 }),
        }),
        ({ listLength, currentIndex }) => {
          const normalizedIndex = currentIndex % listLength;
          const navResult = computeKeyboardNav(listLength, normalizedIndex, 'Enter');
          return navResult.focusedIndex === normalizedIndex && navResult.selected === true;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('Space on focused item selects it without changing focus', () => {
    const result = fc.check(
      fc.property(
        fc.record({
          listLength: fc.integer({ min: 1, max: 100 }),
          currentIndex: fc.integer({ min: 0 }),
        }),
        ({ listLength, currentIndex }) => {
          const normalizedIndex = currentIndex % listLength;
          const navResult = computeKeyboardNav(listLength, normalizedIndex, ' ');
          return navResult.focusedIndex === normalizedIndex && navResult.selected === true;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('ArrowDown at last index wraps to first index (0)', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 1, max: 100 }),
        (listLength) => {
          const lastIndex = listLength - 1;
          const navResult = computeKeyboardNav(listLength, lastIndex, 'ArrowDown');
          return navResult.focusedIndex === 0;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('ArrowUp at first index wraps to last index (N-1)', () => {
    const result = fc.check(
      fc.property(
        fc.integer({ min: 1, max: 100 }),
        (listLength) => {
          const navResult = computeKeyboardNav(listLength, 0, 'ArrowUp');
          return navResult.focusedIndex === listLength - 1;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });

  it('navigation result focusedIndex is always within valid range [0, N-1]', () => {
    const result = fc.check(
      fc.property(
        fc.record({
          listLength: fc.integer({ min: 1, max: 100 }),
          currentIndex: fc.integer({ min: 0 }),
          key: fc.constantFrom('ArrowDown' as const, 'ArrowUp' as const),
        }),
        ({ listLength, currentIndex, key }) => {
          const normalizedIndex = currentIndex % listLength;
          const navResult = computeKeyboardNav(listLength, normalizedIndex, key);
          return navResult.focusedIndex >= 0 && navResult.focusedIndex < listLength;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).withContext(
      result.failed ? `Counterexample: ${JSON.stringify(result.counterexample)}` : ''
    ).toBeFalse();
  });
});
