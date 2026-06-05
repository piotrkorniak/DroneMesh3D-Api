/**
 * Sorts an array of items by their `createdAt` property in descending order (newest first).
 * Returns a new array without mutating the original.
 */
export function sortByCreatedAtDesc<T extends { createdAt: string | Date }>(items: T[]): T[] {
  return [...items].sort((a, b) => {
    const dateA = new Date(a.createdAt).getTime();
    const dateB = new Date(b.createdAt).getTime();
    return dateB - dateA;
  });
}
