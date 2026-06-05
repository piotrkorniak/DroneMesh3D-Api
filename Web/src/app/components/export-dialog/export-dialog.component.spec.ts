import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { HttpResponse, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { ExportDialogComponent } from './export-dialog.component';
import { FlightPlansApiService } from '../../api/services/flight-plans.service';
import { ComponentRef } from '@angular/core';

describe('ExportDialogComponent', () => {
  let component: ExportDialogComponent;
  let componentRef: ComponentRef<ExportDialogComponent>;
  let fixture: ComponentFixture<ExportDialogComponent>;
  let flightPlansApiSpy: jasmine.SpyObj<FlightPlansApiService>;

  beforeEach(async () => {
    flightPlansApiSpy = jasmine.createSpyObj('FlightPlansApiService', ['exportMissionFile']);
    flightPlansApiSpy.exportMissionFile.and.returnValue(of(new HttpResponse({
      body: new Blob(['test'], { type: 'text/csv' }),
      headers: new HttpHeaders({ 'Content-Disposition': 'attachment; filename="mission.csv"' }),
    })));

    await TestBed.configureTestingModule({
      imports: [ExportDialogComponent],
      providers: [
        { provide: FlightPlansApiService, useValue: flightPlansApiSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ExportDialogComponent);
    component = fixture.componentInstance;
    componentRef = fixture.componentRef;
    componentRef.setInput('planId', 'plan-123');
    fixture.detectChanges();
  });

  describe('accessibility', () => {
    it('should render modal with role="dialog"', () => {
      const dialog = fixture.nativeElement.querySelector('[role="dialog"]');
      expect(dialog).toBeTruthy();
    });

    it('should have aria-modal="true"', () => {
      const dialog = fixture.nativeElement.querySelector('[role="dialog"]');
      expect(dialog.getAttribute('aria-modal')).toBe('true');
    });

    it('should have aria-labelledby pointing to the dialog title', () => {
      const dialog = fixture.nativeElement.querySelector('[role="dialog"]');
      expect(dialog.getAttribute('aria-labelledby')).toBe('export-dialog-title');

      const title = fixture.nativeElement.querySelector('#export-dialog-title');
      expect(title).toBeTruthy();
      expect(title.textContent.trim()).toBe('Eksportuj plan lotu');
    });
  });

  describe('format selection', () => {
    it('should default radio group to LitchiCsv', () => {
      const radios = fixture.nativeElement.querySelectorAll('input[type="radio"]');
      const litchiRadio = Array.from<HTMLInputElement>(radios).find(
        (r) => r.value === 'LitchiCsv'
      )!;
      expect(litchiRadio.checked).toBeTrue();
    });

    it('should select different format when radio is changed', () => {
      const radios = fixture.nativeElement.querySelectorAll('input[type="radio"]');
      const kmlRadio = Array.from<HTMLInputElement>(radios).find(
        (r) => r.value === 'Kml'
      )!;

      kmlRadio.click();
      fixture.detectChanges();

      expect(component.selectedFormat()).toBe('Kml');
      expect(kmlRadio.checked).toBeTrue();
    });

    it('should allow selecting DjiWpml format', () => {
      const radios = fixture.nativeElement.querySelectorAll('input[type="radio"]');
      const djiRadio = Array.from<HTMLInputElement>(radios).find(
        (r) => r.value === 'DjiWpml'
      )!;

      djiRadio.click();
      fixture.detectChanges();

      expect(component.selectedFormat()).toBe('DjiWpml');
    });
  });

  describe('download flow', () => {
    it('should call FlightPlansApiService.exportMissionFile() on download button click', () => {
      const downloadBtn = fixture.nativeElement.querySelector('.export-dialog__download-btn');
      downloadBtn.click();
      fixture.detectChanges();

      expect(flightPlansApiSpy.exportMissionFile).toHaveBeenCalledWith('plan-123', 'LitchiCsv');
    });

    it('should pass selected format to exportMissionFile()', () => {
      // Change format to Kml
      const radios = fixture.nativeElement.querySelectorAll('input[type="radio"]');
      const kmlRadio = Array.from<HTMLInputElement>(radios).find(
        (r) => r.value === 'Kml'
      )!;
      kmlRadio.click();
      fixture.detectChanges();

      const downloadBtn = fixture.nativeElement.querySelector('.export-dialog__download-btn');
      downloadBtn.click();
      fixture.detectChanges();

      expect(flightPlansApiSpy.exportMissionFile).toHaveBeenCalledWith('plan-123', 'Kml');
    });

    it('should show "Pobieranie..." and disable button during export', () => {
      const exportSubject = new Subject<HttpResponse<Blob>>();
      flightPlansApiSpy.exportMissionFile.and.returnValue(exportSubject.asObservable());

      const downloadBtn = fixture.nativeElement.querySelector('.export-dialog__download-btn') as HTMLButtonElement;
      downloadBtn.click();
      fixture.detectChanges();

      expect(downloadBtn.disabled).toBeTrue();
      expect(downloadBtn.textContent!.trim()).toContain('Pobieranie...');
    });

    it('should emit closed event after successful download', () => {
      spyOn(component.closed, 'emit');

      const downloadBtn = fixture.nativeElement.querySelector('.export-dialog__download-btn');
      downloadBtn.click();
      fixture.detectChanges();

      expect(component.closed.emit).toHaveBeenCalled();
    });
  });

  describe('close behavior', () => {
    it('should emit closed on backdrop click', () => {
      spyOn(component.closed, 'emit');

      const backdrop = fixture.nativeElement.querySelector('.export-dialog__backdrop');
      backdrop.click();
      fixture.detectChanges();

      expect(component.closed.emit).toHaveBeenCalled();
    });

    it('should emit closed on Escape key', () => {
      spyOn(component.closed, 'emit');

      const dialog = fixture.nativeElement.querySelector('[role="dialog"]');
      const event = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true });
      dialog.dispatchEvent(event);
      fixture.detectChanges();

      expect(component.closed.emit).toHaveBeenCalled();
    });

    it('should emit closed on X button click', () => {
      spyOn(component.closed, 'emit');

      const closeBtn = fixture.nativeElement.querySelector('.export-dialog__close-btn');
      closeBtn.click();
      fixture.detectChanges();

      expect(component.closed.emit).toHaveBeenCalled();
    });
  });

  describe('error handling', () => {
    it('should show inline error on HTTP 404 failure (keep dialog open)', () => {
      spyOn(component.closed, 'emit');
      flightPlansApiSpy.exportMissionFile.and.returnValue(
        throwError(() => new HttpErrorResponse({ status: 404 }))
      );

      const downloadBtn = fixture.nativeElement.querySelector('.export-dialog__download-btn');
      downloadBtn.click();
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('.export-dialog__error');
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent.trim()).toContain('nie został znaleziony');
      expect(component.closed.emit).not.toHaveBeenCalled();
    });

    it('should show inline error on HTTP 422 failure', () => {
      flightPlansApiSpy.exportMissionFile.and.returnValue(
        throwError(() => new HttpErrorResponse({ status: 422 }))
      );

      const downloadBtn = fixture.nativeElement.querySelector('.export-dialog__download-btn');
      downloadBtn.click();
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('.export-dialog__error');
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent.trim()).toContain('Nieprawidłowe dane');
    });

    it('should show inline error on HTTP 500 failure', () => {
      flightPlansApiSpy.exportMissionFile.and.returnValue(
        throwError(() => new HttpErrorResponse({ status: 500 }))
      );

      const downloadBtn = fixture.nativeElement.querySelector('.export-dialog__download-btn');
      downloadBtn.click();
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('.export-dialog__error');
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent.trim()).toContain('nieoczekiwany błąd');
    });

    it('should show "Serwer niedostępny" on network error', () => {
      flightPlansApiSpy.exportMissionFile.and.returnValue(
        throwError(() => new HttpErrorResponse({ status: 0 }))
      );

      const downloadBtn = fixture.nativeElement.querySelector('.export-dialog__download-btn');
      downloadBtn.click();
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('.export-dialog__error');
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent.trim()).toContain('Serwer niedostępny');
    });

    it('should re-enable download button after error', () => {
      flightPlansApiSpy.exportMissionFile.and.returnValue(
        throwError(() => new HttpErrorResponse({ status: 500 }))
      );

      const downloadBtn = fixture.nativeElement.querySelector('.export-dialog__download-btn') as HTMLButtonElement;
      downloadBtn.click();
      fixture.detectChanges();

      expect(downloadBtn.disabled).toBeFalse();
    });

    it('should show error with role="alert"', () => {
      flightPlansApiSpy.exportMissionFile.and.returnValue(
        throwError(() => new HttpErrorResponse({ status: 500 }))
      );

      const downloadBtn = fixture.nativeElement.querySelector('.export-dialog__download-btn');
      downloadBtn.click();
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('[role="alert"]');
      expect(errorEl).toBeTruthy();
    });
  });

  describe('focus trap', () => {
    it('should cycle focus from last to first element on Tab at last element', () => {
      const focusableEls = fixture.nativeElement.querySelectorAll(
        'button:not([disabled]), input:not([disabled])'
      );
      const lastEl = focusableEls[focusableEls.length - 1] as HTMLElement;
      const firstEl = focusableEls[0] as HTMLElement;

      lastEl.focus();
      expect(document.activeElement).toBe(lastEl);

      const dialog = fixture.nativeElement.querySelector('[role="dialog"]');
      const tabEvent = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true });
      dialog.dispatchEvent(tabEvent);
      fixture.detectChanges();

      expect(document.activeElement).toBe(firstEl);
    });

    it('should cycle focus from first to last element on Shift+Tab at first element', () => {
      const focusableEls = fixture.nativeElement.querySelectorAll(
        'button:not([disabled]), input:not([disabled])'
      );
      const firstEl = focusableEls[0] as HTMLElement;
      const lastEl = focusableEls[focusableEls.length - 1] as HTMLElement;

      firstEl.focus();
      expect(document.activeElement).toBe(firstEl);

      const dialog = fixture.nativeElement.querySelector('[role="dialog"]');
      const shiftTabEvent = new KeyboardEvent('keydown', {
        key: 'Tab',
        shiftKey: true,
        bubbles: true,
      });
      dialog.dispatchEvent(shiftTabEvent);
      fixture.detectChanges();

      expect(document.activeElement).toBe(lastEl);
    });
  });
});
