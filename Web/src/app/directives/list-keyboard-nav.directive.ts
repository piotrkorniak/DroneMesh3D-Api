/**
 * ListKeyboardNavDirective — provides reusable keyboard navigation logic
 * for ARIA listbox patterns.
 *
 * Encapsulates the cycling arithmetic:
 * - ArrowDown: (currentIndex + 1) % listLength
 * - ArrowUp: (currentIndex - 1 + listLength) % listLength
 * - Enter/Space: selects the currently focused item
 */

/**
 * Result of processing a keyboard navigation event.
 */
export interface KeyboardNavResult {
  /** The new focused index after the key press */
  focusedIndex: number;
  /** Whether the focused item should be selected (Enter/Space) */
  selected: boolean;
}

/**
 * Pure function that computes the next focused index and selection state
 * given a list length, current focus index, and the key pressed.
 *
 * @param listLength Number of items in the list (must be > 0)
 * @param currentIndex Current focused index (0-based, must be in [0, listLength-1])
 * @param key The keyboard key pressed
 * @returns The navigation result with new focusedIndex and whether selection occurred
 */
export function computeKeyboardNav(
  listLength: number,
  currentIndex: number,
  key: 'ArrowDown' | 'ArrowUp' | 'Enter' | ' '
): KeyboardNavResult {
  if (listLength <= 0) {
    throw new Error('listLength must be greater than 0');
  }

  // Normalize currentIndex to valid range
  const normalizedIndex = ((currentIndex % listLength) + listLength) % listLength;

  switch (key) {
    case 'ArrowDown':
      return {
        focusedIndex: (normalizedIndex + 1) % listLength,
        selected: false,
      };
    case 'ArrowUp':
      return {
        focusedIndex: (normalizedIndex - 1 + listLength) % listLength,
        selected: false,
      };
    case 'Enter':
    case ' ':
      return {
        focusedIndex: normalizedIndex,
        selected: true,
      };
    default:
      return {
        focusedIndex: normalizedIndex,
        selected: false,
      };
  }
}
