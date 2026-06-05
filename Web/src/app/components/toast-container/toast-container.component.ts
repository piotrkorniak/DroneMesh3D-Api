import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastNotification, ToastService } from '../../services/toast.service';
import { truncateMessage } from '../../utils/truncate-message';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './toast-container.component.html',
  styleUrl: './toast-container.component.scss',
})
export class ToastContainerComponent {
  private readonly toastService = inject(ToastService);

  readonly visibleToasts = this.toastService.visibleToasts;

  toastRole(toast: ToastNotification): string {
    return toast.type === 'error' ? 'alert' : 'status';
  }

  toastAriaLive(toast: ToastNotification): string {
    return toast.type === 'error' ? 'assertive' : 'polite';
  }

  displayMessage(toast: ToastNotification): string {
    return truncateMessage(toast.message);
  }

  dismiss(id: string): void {
    this.toastService.dismiss(id);
  }
}
