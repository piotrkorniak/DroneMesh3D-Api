import { Pipe, PipeTransform } from '@angular/core';

/**
 * Transforms an ISO date string to a relative time string (within 30 days)
 * or an absolute DD.MM.YYYY format (older than 30 days).
 *
 * Uses Intl.RelativeTimeFormat with Polish locale for relative time labels.
 */
@Pipe({
  name: 'relativeTime',
  standalone: true,
  pure: true,
})
export class RelativeTimePipe implements PipeTransform {
  private readonly formatter = new Intl.RelativeTimeFormat('pl', { numeric: 'always', style: 'short' });

  transform(value: string): string {
    const date = new Date(value);
    const now = new Date();
    const diffMs = date.getTime() - now.getTime();
    const diffSeconds = Math.round(diffMs / 1000);
    const diffMinutes = Math.round(diffMs / (1000 * 60));
    const diffHours = Math.round(diffMs / (1000 * 60 * 60));
    const diffDays = Math.round(diffMs / (1000 * 60 * 60 * 24));

    const THIRTY_DAYS_MS = 30 * 24 * 60 * 60 * 1000;

    if (Math.abs(diffMs) > THIRTY_DAYS_MS) {
      return this.formatAbsoluteDate(date);
    }

    const absDiffMinutes = Math.abs(diffMinutes);
    const absDiffHours = Math.abs(diffHours);
    const absDiffDays = Math.abs(diffDays);

    if (absDiffDays >= 1) {
      return this.formatter.format(diffDays, 'day');
    }
    if (absDiffHours >= 1) {
      return this.formatter.format(diffHours, 'hour');
    }
    if (absDiffMinutes >= 1) {
      return this.formatter.format(diffMinutes, 'minute');
    }
    return this.formatter.format(diffSeconds, 'second');
  }

  private formatAbsoluteDate(date: Date): string {
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}.${month}.${year}`;
  }
}
