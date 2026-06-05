/**
 * FocusTrapDirective — provides reusable focus trap logic for modal dialogs.
 *
 * Encapsulates the cycling arithmetic for focus within modal boundaries:
 * - Tab at last focusable element: wraps to first (index 0)
 * - Shift+Tab at first focusable element: wraps to last (index elementCount - 1)
 * - Tab at any other index: moves to index + 1
 * - Shift+Tab at any other index: moves to index - 1
 */

/**
 * Pure function that computes the next focused index within a focus trap
 * given the total number of focusable elements, the current index, and the key pressed.
 *
 * @param elementCount Number of focusable elements in the modal (must be >= 1)
 * @param currentIndex Current focused index (0-based, must be in [0, elementCount - 1])
 * @param key The keyboard action: 'Tab' or 'Shift+Tab'
 * @returns The next focused index after the key press
 */
export function computeFocusTrap(
  elementCount: number,
  currentIndex: number,
  key: 'Tab' | 'Shift+Tab'
): number {
  if (elementCount < 1) {
    throw new Error('elementCount must be at least 1');
  }

  if (currentIndex < 0 || currentIndex >= elementCount) {
    throw new Error(`currentIndex must be in range [0, ${elementCount - 1}]`);
  }

  switch (key) {
    case 'Tab':
      return (currentIndex + 1) % elementCount;
    case 'Shift+Tab':
      return (currentIndex - 1 + elementCount) % elementCount;
  }
}
