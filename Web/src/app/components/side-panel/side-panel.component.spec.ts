import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SidePanelComponent } from './side-panel.component';
import { PanelStateService } from '../../services/panel-state.service';

describe('SidePanelComponent', () => {
  let component: SidePanelComponent;
  let fixture: ComponentFixture<SidePanelComponent>;
  let panelState: PanelStateService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SidePanelComponent],
    }).compileComponents();

    panelState = TestBed.inject(PanelStateService);
    fixture = TestBed.createComponent(SidePanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('expanded/collapsed rendering', () => {
    it('should render with side-panel--expanded class when expanded (default)', () => {
      const panel = fixture.nativeElement.querySelector('.side-panel');
      expect(panel.classList.contains('side-panel--expanded')).toBeTrue();
      expect(panel.classList.contains('side-panel--collapsed')).toBeFalse();
    });

    it('should render with side-panel--collapsed class when collapsed', () => {
      panelState.collapse();
      fixture.detectChanges();

      const panel = fixture.nativeElement.querySelector('.side-panel');
      expect(panel.classList.contains('side-panel--collapsed')).toBeTrue();
      expect(panel.classList.contains('side-panel--expanded')).toBeFalse();
    });
  });

  describe('toggle button', () => {
    it('should have aria-label="Toggle side panel"', () => {
      const toggleBtn = fixture.nativeElement.querySelector('.side-panel__toggle');
      expect(toggleBtn.getAttribute('aria-label')).toBe('Toggle side panel');
    });

    it('should have aria-expanded="true" when expanded', () => {
      const toggleBtn = fixture.nativeElement.querySelector('.side-panel__toggle');
      expect(toggleBtn.getAttribute('aria-expanded')).toBe('true');
    });

    it('should have aria-expanded="false" when collapsed', () => {
      panelState.collapse();
      fixture.detectChanges();

      const toggleBtn = fixture.nativeElement.querySelector('.side-panel__toggle');
      expect(toggleBtn.getAttribute('aria-expanded')).toBe('false');
    });

    it('should toggle expanded state when clicked', () => {
      expect(panelState.isExpanded()).toBeTrue();

      const toggleBtn = fixture.nativeElement.querySelector('.side-panel__toggle');
      toggleBtn.click();
      fixture.detectChanges();

      expect(panelState.isExpanded()).toBeFalse();
      expect(toggleBtn.getAttribute('aria-expanded')).toBe('false');
    });
  });

  describe('section headers', () => {
    it('should have aria-expanded="true" on section headers when sections are not collapsed', () => {
      const sectionHeaders = fixture.nativeElement.querySelectorAll('.side-panel__section-header');
      sectionHeaders.forEach((header: HTMLElement) => {
        expect(header.getAttribute('aria-expanded')).toBe('true');
      });
    });

    it('should have aria-expanded="false" on Area_List section header when section is collapsed', () => {
      panelState.collapseSection('Area_List');
      fixture.detectChanges();

      const sectionHeaders = fixture.nativeElement.querySelectorAll('.side-panel__section-header');
      // First section is Area_List
      expect(sectionHeaders[0].getAttribute('aria-expanded')).toBe('false');
    });

    it('should toggle section visibility when section header is clicked', () => {
      // Area_List section is the first one
      const sectionHeaders = fixture.nativeElement.querySelectorAll('.side-panel__section-header');
      const areaListHeader = sectionHeaders[0];

      // Initially expanded
      expect(areaListHeader.getAttribute('aria-expanded')).toBe('true');

      areaListHeader.click();
      fixture.detectChanges();

      // Now collapsed
      expect(areaListHeader.getAttribute('aria-expanded')).toBe('false');
      expect(panelState.isSectionCollapsed('Area_List')).toBeTrue();
    });

    it('should hide section content when section is collapsed', () => {
      // Section content should exist when expanded
      let sectionContent = fixture.nativeElement.querySelectorAll('.side-panel__section-content');
      expect(sectionContent.length).toBe(3); // All three sections visible

      panelState.collapseSection('Area_List');
      fixture.detectChanges();

      sectionContent = fixture.nativeElement.querySelectorAll('.side-panel__section-content');
      expect(sectionContent.length).toBe(2); // Only two sections visible
    });
  });

  describe('mobile mode', () => {
    beforeEach(() => {
      // Simulate mobile viewport
      spyOnProperty(window, 'innerWidth').and.returnValue(600);
      component.onResize();
      fixture.detectChanges();
    });

    it('should show close button when expanded in mobile mode', () => {
      panelState.expand();
      fixture.detectChanges();

      const closeBtn = fixture.nativeElement.querySelector('.side-panel__close');
      expect(closeBtn).toBeTruthy();
      expect(closeBtn.getAttribute('aria-label')).toBe('Close side panel');
    });

    it('should show backdrop when expanded in mobile mode', () => {
      panelState.expand();
      fixture.detectChanges();

      const backdrop = fixture.nativeElement.querySelector('.side-panel__backdrop');
      expect(backdrop).toBeTruthy();
    });

    it('should not show backdrop when collapsed in mobile mode', () => {
      panelState.collapse();
      fixture.detectChanges();

      const backdrop = fixture.nativeElement.querySelector('.side-panel__backdrop');
      expect(backdrop).toBeFalsy();
    });

    it('should collapse panel when backdrop is clicked', () => {
      panelState.expand();
      fixture.detectChanges();

      const backdrop = fixture.nativeElement.querySelector('.side-panel__backdrop');
      backdrop.click();
      fixture.detectChanges();

      expect(panelState.isExpanded()).toBeFalse();
    });

    it('should collapse panel when close button is clicked', () => {
      panelState.expand();
      fixture.detectChanges();

      const closeBtn = fixture.nativeElement.querySelector('.side-panel__close');
      closeBtn.click();
      fixture.detectChanges();

      expect(panelState.isExpanded()).toBeFalse();
    });
  });
});
