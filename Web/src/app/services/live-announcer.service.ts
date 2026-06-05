import { Injectable, signal } from '@angular/core';

/**
 * LiveAnnouncerService provides a mechanism for components to announce
 * dynamic content changes to assistive technologies via an aria-live region.
 *
 * The announcement text is exposed as a signal consumed by the AppComponent template.
 * After each announcement, the text is cleared after a short delay to allow
 * subsequent announcements to be picked up by screen readers.
 *
 * Requirement 11.8: Dynamic list updates announced via aria-live="polite".
 */
@Injectable({ providedIn: 'root' })
export class LiveAnnouncerService {
  private readonly _announcement = signal('');
  private clearTimeout: ReturnType<typeof setTimeout> | null = null;

  /** Current announcement text */
  readonly announcement = this._announcement.asReadonly();

  /**
   * Announce a message to assistive technologies.
   * The message is set on the aria-live region and cleared after 1 second
   * to allow repeated announcements of the same text.
   */
  announce(message: string): void {
    // Clear any pending timeout
    if (this.clearTimeout) {
      clearTimeout(this.clearTimeout);
    }

    // Clear first to ensure screen readers pick up repeated identical messages
    this._announcement.set('');

    // Use setTimeout to ensure the DOM update cycle catches the change
    setTimeout(() => {
      this._announcement.set(message);

      // Clear after 1 second so subsequent announcements work
      this.clearTimeout = setTimeout(() => {
        this._announcement.set('');
        this.clearTimeout = null;
      }, 1000);
    }, 100);
  }
}
