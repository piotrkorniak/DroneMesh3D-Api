import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  inject,
  input,
  Output,
  signal,
  AfterViewInit,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { FlightPlansApiService } from '../../api/services/flight-plans.service';
import { ExportFormat } from '../../api/models/export-format';

/**
 * ExportDialogComponent — modal dialog for exporting a flight plan mission file.
 *
 * Features:
 * - Radio group for format selection (LitchiCsv, Kml, DjiWpml)
 * - Download button calling FlightPlansApiService.exportMissionFile()
 * - Focus trap: Tab/Shift+Tab cycle within dialog
 * - Closes on backdrop click, Escape key, or X button
 * - Inline error display on export failure (keeps dialog open)
 * - ARIA: role="dialog", aria-modal="true", aria-labelledby
 */
@Component({
  selector: 'app-export-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './export-dialog.component.html',
  styleUrl: './export-dialog.component.scss',
})
export class ExportDialogComponent implements AfterViewInit, OnDestroy {
  private readonly flightPlansApi = inject(FlightPlansApiService);

  /** Plan ID to export */
  readonly planId = input.required<string>();

  /** Emitted when the dialog should close */
  @Output() readonly closed = new EventEmitter<void>();

  @ViewChild('dialogEl', { static: true }) dialogElRef!: ElementRef<HTMLElement>;

  readonly selectedFormat = signal<ExportFormat>('LitchiCsv');
  readonly exporting = signal(false);
  readonly error = signal<string | null>(null);

  private abortController: AbortController | null = null;
  private timeoutId: ReturnType<typeof setTimeout> | null = null;
  private previouslyFocusedElement: Element | null = null;

  ngAfterViewInit(): void {
    this.previouslyFocusedElement = document.activeElement;
    // Focus the dialog element for keyboard accessibility
    this.dialogElRef.nativeElement.setAttribute('tabindex', '-1');
    this.dialogElRef.nativeElement.focus();
  }

  ngOnDestroy(): void {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }
    if (this.abortController) {
      this.abortController.abort();
    }
    // Return focus to triggering element
    if (this.previouslyFocusedElement instanceof HTMLElement) {
      this.previouslyFocusedElement.focus();
    }
  }

  selectFormat(format: ExportFormat): void {
    this.selectedFormat.set(format);
  }

  download(): void {
    if (this.exporting()) return;

    this.exporting.set(true);
    this.error.set(null);

    // Set up 30s timeout
    this.abortController = new AbortController();
    this.timeoutId = setTimeout(() => {
      this.abortController?.abort();
      this.exporting.set(false);
      this.error.set('Przekroczono czas oczekiwania');
    }, 30000);

    this.flightPlansApi.exportMissionFile(this.planId(), this.selectedFormat()).subscribe({
      next: (response) => {
        this.clearTimeout();
        this.exporting.set(false);

        // Extract filename from Content-Disposition header
        const contentDisposition = response.headers?.get('Content-Disposition') ?? '';
        const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
        const filename = filenameMatch ? filenameMatch[1].replace(/['"]/g, '') : 'mission-file';

        // Trigger browser download
        if (response.body) {
          const url = URL.createObjectURL(response.body);
          const a = document.createElement('a');
          a.href = url;
          a.download = filename;
          a.click();
          URL.revokeObjectURL(url);
        }

        this.close();
      },
      error: (err) => {
        this.clearTimeout();
        this.exporting.set(false);

        if (err?.status === 404) {
          this.error.set('Plan lotu nie został znaleziony');
        } else if (err?.status === 422) {
          this.error.set('Nieprawidłowe dane — nie można wygenerować pliku');
        } else if (err?.status === 500) {
          this.error.set('Wystąpił nieoczekiwany błąd');
        } else {
          this.error.set('Serwer niedostępny');
        }
      },
    });
  }

  close(): void {
    this.closed.emit();
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      return;
    }

    // Focus trap: Tab/Shift+Tab cycling
    if (event.key === 'Tab') {
      const focusableElements = this.getFocusableElements();
      if (focusableElements.length === 0) return;

      const firstEl = focusableElements[0];
      const lastEl = focusableElements[focusableElements.length - 1];
      const activeEl = document.activeElement;

      if (event.shiftKey) {
        if (activeEl === firstEl || !this.dialogElRef.nativeElement.contains(activeEl)) {
          event.preventDefault();
          lastEl.focus();
        }
      } else {
        if (activeEl === lastEl || !this.dialogElRef.nativeElement.contains(activeEl)) {
          event.preventDefault();
          firstEl.focus();
        }
      }
    }
  }

  private getFocusableElements(): HTMLElement[] {
    const selector = 'button:not([disabled]), input:not([disabled]), [tabindex]:not([tabindex="-1"])';
    return Array.from(this.dialogElRef.nativeElement.querySelectorAll<HTMLElement>(selector));
  }

  private clearTimeout(): void {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
      this.timeoutId = null;
    }
  }
}
