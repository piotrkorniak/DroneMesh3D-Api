import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { SkeletonComponent } from './skeleton.component';

// Test host component using signals to avoid ExpressionChangedAfterItHasBeenChecked
@Component({
  standalone: true,
  imports: [SkeletonComponent],
  template: `<app-skeleton [lines]="lines()" [height]="height()" />`,
})
class TestHostComponent {
  lines = signal(3);
  height = signal('1rem');
}

describe('SkeletonComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let hostComponent: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    hostComponent = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    const skeleton = fixture.nativeElement.querySelector('app-skeleton');
    expect(skeleton).toBeTruthy();
  });

  it('should render default 3 skeleton lines', () => {
    const lines = fixture.nativeElement.querySelectorAll('.skeleton-line');
    expect(lines.length).toBe(3);
  });

  it('should render the specified number of lines', () => {
    hostComponent.lines.set(5);
    fixture.detectChanges();

    const lines = fixture.nativeElement.querySelectorAll('.skeleton-line');
    expect(lines.length).toBe(5);
  });

  it('should apply the specified height to each line', () => {
    hostComponent.height.set('2rem');
    fixture.detectChanges();

    const lines = fixture.nativeElement.querySelectorAll('.skeleton-line') as NodeListOf<HTMLElement>;
    lines.forEach((line: HTMLElement) => {
      expect(line.style.height).toBe('2rem');
    });
  });

  it('should apply default height of 1rem', () => {
    const lines = fixture.nativeElement.querySelectorAll('.skeleton-line') as NodeListOf<HTMLElement>;
    lines.forEach((line: HTMLElement) => {
      expect(line.style.height).toBe('1rem');
    });
  });

  it('should mark all skeleton lines as aria-hidden', () => {
    const lines = fixture.nativeElement.querySelectorAll('.skeleton-line') as NodeListOf<HTMLElement>;
    lines.forEach((line: HTMLElement) => {
      expect(line.getAttribute('aria-hidden')).toBe('true');
    });
  });

  it('should render 0 lines when lines input is 0', () => {
    hostComponent.lines.set(0);
    fixture.detectChanges();

    const lines = fixture.nativeElement.querySelectorAll('.skeleton-line');
    expect(lines.length).toBe(0);
  });

  it('should render 1 line when lines input is 1', () => {
    hostComponent.lines.set(1);
    fixture.detectChanges();

    const lines = fixture.nativeElement.querySelectorAll('.skeleton-line');
    expect(lines.length).toBe(1);
  });
});
