import { Injectable, signal } from '@angular/core';

export interface ToastNotification {
  id: string;
  type: 'success' | 'error' | 'info';
  message: string;
  createdAt: number;
  autoDismiss: boolean;
}

/** Maximum number of simultaneously visible toasts. */
const MAX_VISIBLE = 3;

/** Maximum number of pending toasts in the queue. */
const MAX_PENDING = 10;

/** Auto-dismiss delay in milliseconds for success/info toasts. */
const AUTO_DISMISS_MS = 5000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  /** Currently visible toasts (max 3). */
  readonly visibleToasts = signal<ToastNotification[]>([]);

  /** Pending queue for toasts waiting to become visible (FIFO, max 10). */
  readonly pendingQueue = signal<ToastNotification[]>([]);

  /** Internal map of auto-dismiss timeout handles keyed by toast ID. */
  private readonly timers = new Map<string, ReturnType<typeof setTimeout>>();

  /**
   * Show a toast notification.
   * If fewer than 3 toasts are visible, the toast is shown immediately.
   * Otherwise it's placed in the pending queue (max 10, FIFO — discarded if full).
   */
  show(type: 'success' | 'error' | 'info', message: string): void {
    const toast: ToastNotification = {
      id: crypto.randomUUID(),
      type,
      message,
      createdAt: Date.now(),
      autoDismiss: type !== 'error',
    };

    if (this.visibleToasts().length < MAX_VISIBLE) {
      this.visibleToasts.update(toasts => [...toasts, toast]);
      this.scheduleAutoDismiss(toast);
    } else {
      // Add to pending queue if capacity allows; discard if full
      if (this.pendingQueue().length < MAX_PENDING) {
        this.pendingQueue.update(queue => [...queue, toast]);
      }
      // else: silently discard
    }
  }

  /**
   * Dismiss a toast by ID.
   * Removes it from visible toasts (clearing its timer), then promotes
   * the oldest pending toast to visible if one exists.
   */
  dismiss(id: string): void {
    // Clear any auto-dismiss timer
    this.clearTimer(id);

    const currentVisible = this.visibleToasts();
    const index = currentVisible.findIndex(t => t.id === id);
    if (index === -1) {
      // If not in visible, try removing from pending queue
      this.pendingQueue.update(queue => queue.filter(t => t.id !== id));
      return;
    }

    // Remove from visible
    this.visibleToasts.update(toasts => toasts.filter(t => t.id !== id));

    // Promote the oldest pending toast (FIFO)
    this.promoteNextFromQueue();
  }

  /** Promote the oldest pending toast to visible, if any. */
  private promoteNextFromQueue(): void {
    const queue = this.pendingQueue();
    if (queue.length === 0) return;

    const [next, ...remaining] = queue;
    this.pendingQueue.set(remaining);
    this.visibleToasts.update(toasts => [...toasts, next]);
    this.scheduleAutoDismiss(next);
  }

  /** Schedule auto-dismiss for success/info toasts. */
  private scheduleAutoDismiss(toast: ToastNotification): void {
    if (!toast.autoDismiss) return;

    const timer = setTimeout(() => {
      this.timers.delete(toast.id);
      this.dismiss(toast.id);
    }, AUTO_DISMISS_MS);

    this.timers.set(toast.id, timer);
  }

  /** Clear a pending auto-dismiss timer. */
  private clearTimer(id: string): void {
    const timer = this.timers.get(id);
    if (timer != null) {
      clearTimeout(timer);
      this.timers.delete(id);
    }
  }
}
