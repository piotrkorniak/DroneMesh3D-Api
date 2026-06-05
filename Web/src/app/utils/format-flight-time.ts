/**
 * Formats a total number of seconds into a human-readable flight time string.
 * Format: "X min Y s"
 *
 * @param totalSeconds - Non-negative integer representing total flight time in seconds
 * @returns Formatted string e.g. "4 min 32 s"
 */
export function formatFlightTime(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes} min ${seconds} s`;
}
