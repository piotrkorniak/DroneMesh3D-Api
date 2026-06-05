import * as fc from 'fast-check';
import { truncateMessage } from './truncate-message';

// Feature: drone-mesh-gui, Property 11: Toast message truncation at 150 characters

/**
 * Property-Based Tests for truncateMessage utility
 *
 * **Validates: Requirements 8.9**
 *
 * Property 11: Toast message truncation at 150 characters
 * - If message.length > 150 then truncateMessage returns message.substring(0, 150) + "…",
 *   else returns full message.
 */
describe('Feature: drone-mesh-gui, Property 11: Toast message truncation at 150 characters', () => {

  it('should return message.substring(0, 150) + "…" when message.length > 150', () => {
    const result = fc.check(
      fc.property(
        fc.string({ minLength: 151, maxLength: 500 }),
        (message) => {
          const truncated = truncateMessage(message);
          return truncated === message.substring(0, 150) + '\u2026';
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should return full message when message.length <= 150', () => {
    const result = fc.check(
      fc.property(
        fc.string({ minLength: 0, maxLength: 150 }),
        (message) => {
          const truncated = truncateMessage(message);
          return truncated === message;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should produce a result of length exactly 151 (150 + ellipsis) when input > 150 chars', () => {
    const result = fc.check(
      fc.property(
        fc.string({ minLength: 151, maxLength: 500 }),
        (message) => {
          const truncated = truncateMessage(message);
          return truncated.length === 151;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });

  it('should not modify messages of exactly 150 characters', () => {
    const result = fc.check(
      fc.property(
        fc.string({ minLength: 150, maxLength: 150 }),
        (message) => {
          const truncated = truncateMessage(message);
          return truncated === message;
        }
      ),
      { numRuns: 100 }
    );

    expect(result.failed).toBeFalse();
  });
});
