import * as fc from 'fast-check';
import { sortByCreatedAtDesc } from './sort-by-date';

// Feature: drone-mesh-gui, Property 1: List items are sorted by creation date descending

/**
 * Property-Based Tests for sortByCreatedAtDesc utility
 *
 * **Validates: Requirements 3.1, 5.1**
 *
 * Property 1: List items are sorted by creation date descending
 * - For any non-empty array of items with createdAt dates, after sorting,
 *   item[i].createdAt >= item[i+1].createdAt for all consecutive pairs.
 */
describe('Feature: drone-mesh-gui, Property 1: List items are sorted by creation date descending', () => {

  it('should satisfy item[i].createdAt >= item[i+1].createdAt for all consecutive pairs', () => {
    const result = fc.check(
      fc.property(
        fc.array(fc.record({ createdAt: fc.date() }), { minLength: 1 }),
        (items) => {
          const sorted = sortByCreatedAtDesc(items);
          for (let i = 0; i < sorted.length - 1; i++) {
            const dateA = new Date(sorted[i].createdAt).getTime();
            const dateB = new Date(sorted[i + 1].createdAt).getTime();
            if (dateA < dateB) {
              return false;
            }
          }
          return true;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should not mutate the original array', () => {
    const result = fc.check(
      fc.property(
        fc.array(fc.record({ createdAt: fc.date() }), { minLength: 1 }),
        (items) => {
          const originalCopy = [...items];
          sortByCreatedAtDesc(items);
          return items.length === originalCopy.length &&
            items.every((item, idx) => item === originalCopy[idx]);
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should preserve the length of the array', () => {
    const result = fc.check(
      fc.property(
        fc.array(fc.record({ createdAt: fc.date() })),
        (items) => {
          const sorted = sortByCreatedAtDesc(items);
          return sorted.length === items.length;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });
});
