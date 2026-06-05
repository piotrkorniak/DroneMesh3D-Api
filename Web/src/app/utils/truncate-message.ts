/**
 * Truncates a message string to a maximum of 150 characters.
 * If the message exceeds 150 characters, appends an ellipsis ("…").
 * Otherwise, returns the full message unchanged.
 *
 * @param message - The message string to truncate
 * @returns The truncated or full message
 */
export function truncateMessage(message: string): string {
  if (message.length > 150) {
    return message.substring(0, 150) + '\u2026';
  }
  return message;
}
