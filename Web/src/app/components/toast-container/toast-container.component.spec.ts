import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastContainerComponent } from './toast-container.component';
import { ToastService } from '../../services/toast.service';

describe('ToastContainerComponent', () => {
  let component: ToastContainerComponent;
  let fixture: ComponentFixture<ToastContainerComponent>;
  let toastService: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToastContainerComponent],
    }).compileComponents();

    toastService = TestBed.inject(ToastService);
    fixture = TestBed.createComponent(ToastContainerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render no toasts initially', () => {
    const toastElements = fixture.nativeElement.querySelectorAll('.toast');
    expect(toastElements.length).toBe(0);
  });

  it('should render a success toast with correct styling', () => {
    toastService.show('success', 'Operation completed');
    fixture.detectChanges();

    const toast = fixture.nativeElement.querySelector('.toast');
    expect(toast).toBeTruthy();
    expect(toast.classList.contains('toast--success')).toBeTrue();
    expect(toast.getAttribute('role')).toBe('status');
    expect(toast.getAttribute('aria-live')).toBe('polite');
  });

  it('should render an error toast with role="alert" and aria-live="assertive"', () => {
    toastService.show('error', 'Something went wrong');
    fixture.detectChanges();

    const toast = fixture.nativeElement.querySelector('.toast');
    expect(toast).toBeTruthy();
    expect(toast.classList.contains('toast--error')).toBeTrue();
    expect(toast.getAttribute('role')).toBe('alert');
    expect(toast.getAttribute('aria-live')).toBe('assertive');
  });

  it('should render an info toast with correct ARIA attributes', () => {
    toastService.show('info', 'Some info');
    fixture.detectChanges();

    const toast = fixture.nativeElement.querySelector('.toast');
    expect(toast).toBeTruthy();
    expect(toast.classList.contains('toast--info')).toBeTrue();
    expect(toast.getAttribute('role')).toBe('status');
    expect(toast.getAttribute('aria-live')).toBe('polite');
  });

  it('should display a close button on each toast', () => {
    toastService.show('success', 'Test message');
    fixture.detectChanges();

    const closeBtn = fixture.nativeElement.querySelector('.toast__close');
    expect(closeBtn).toBeTruthy();
    expect(closeBtn.getAttribute('aria-label')).toBe('Dismiss notification');
  });

  it('should dismiss a toast when close button is clicked', () => {
    toastService.show('success', 'Test message');
    fixture.detectChanges();

    const closeBtn = fixture.nativeElement.querySelector('.toast__close');
    closeBtn.click();
    fixture.detectChanges();

    const toasts = fixture.nativeElement.querySelectorAll('.toast');
    expect(toasts.length).toBe(0);
  });

  it('should truncate messages longer than 150 characters', () => {
    const longMessage = 'A'.repeat(200);
    toastService.show('info', longMessage);
    fixture.detectChanges();

    const messageEl = fixture.nativeElement.querySelector('.toast__message');
    expect(messageEl.textContent.trim().length).toBe(151); // 150 chars + ellipsis
    expect(messageEl.textContent.trim().endsWith('\u2026')).toBeTrue();
  });

  it('should set title attribute with full text for messages > 150 chars', () => {
    const longMessage = 'B'.repeat(200);
    toastService.show('info', longMessage);
    fixture.detectChanges();

    const toast = fixture.nativeElement.querySelector('.toast');
    expect(toast.getAttribute('title')).toBe(longMessage);
  });

  it('should NOT set title attribute for messages <= 150 chars', () => {
    toastService.show('info', 'Short message');
    fixture.detectChanges();

    const toast = fixture.nativeElement.querySelector('.toast');
    expect(toast.getAttribute('title')).toBeNull();
  });

  it('should render multiple toasts (max 3 visible)', () => {
    toastService.show('success', 'First');
    toastService.show('error', 'Second');
    toastService.show('info', 'Third');
    fixture.detectChanges();

    const toasts = fixture.nativeElement.querySelectorAll('.toast');
    expect(toasts.length).toBe(3);
  });

  it('should show correct icon for each toast type', () => {
    toastService.show('success', 'Success msg');
    toastService.show('error', 'Error msg');
    toastService.show('info', 'Info msg');
    fixture.detectChanges();

    const icons = fixture.nativeElement.querySelectorAll('.toast__icon');
    expect(icons.length).toBe(3);

    // Each icon should contain an SVG
    icons.forEach((icon: HTMLElement) => {
      expect(icon.querySelector('svg')).toBeTruthy();
    });
  });

  it('should be positioned fixed at bottom-right', () => {
    const host = fixture.nativeElement as HTMLElement;
    const computedStyle = getComputedStyle(host);
    expect(computedStyle.position).toBe('fixed');
  });
});
