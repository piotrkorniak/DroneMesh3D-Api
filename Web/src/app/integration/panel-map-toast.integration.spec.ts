import { TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { SelectionStateService } from '../services/selection-state.service';
import { FlightPathVisualizationService } from '../services/flight-path-visualization.service';
import { ToastService } from '../services/toast.service';
import { PanelStateService } from '../services/panel-state.service';
import { ToastContainerComponent } from '../components/toast-container/toast-container.component';
import { SidePanelComponent } from '../components/side-panel/side-panel.component';
import { AreaResponse } from '../api/models/area-response';
import { WaypointDto } from '../api/models/waypoint-dto';
import Point from 'ol/geom/Point';
import LineString from 'ol/geom/LineString';

/**
 * Integration tests for the full panel–map–toast interaction flow.
 *
 * These tests verify that the components communicate correctly through shared services:
 * - Area selection triggers map fit via SelectionStateService
 * - Plan selection triggers flight path render via FlightPathVisualizationService
 * - Form success/error shows toast via ToastService + ToastContainerComponent
 * - Responsive layout at different viewport widths
 * - Toast notifications rendered from any caller
 */

// Minimal host component combining ToastContainer for rendering tests
@Component({
  selector: 'app-test-host',
  standalone: true,
  imports: [ToastContainerComponent],
  template: `<app-toast-container />`,
})
class TestHostComponent {}

describe('Integration: Panel–Map–Toast interaction flow', () => {
  let selectionState: SelectionStateService;
  let flightPathViz: FlightPathVisualizationService;
  let toastService: ToastService;
  let panelState: PanelStateService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent, ToastContainerComponent, SidePanelComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        SelectionStateService,
        FlightPathVisualizationService,
        ToastService,
        PanelStateService,
      ],
    }).compileComponents();

    selectionState = TestBed.inject(SelectionStateService);
    flightPathViz = TestBed.inject(FlightPathVisualizationService);
    toastService = TestBed.inject(ToastService);
    panelState = TestBed.inject(PanelStateService);
  });

  afterEach(() => {
    // Clear flight path features between tests
    flightPathViz.flightPathSource.clear();
  });

  describe('Area selection triggers map fit', () => {
    it('should set selectedArea in SelectionStateService when an area is selected', () => {
      // Arrange: populate areas cache
      const mockArea: AreaResponse = {
        id: 'area-1',
        createdAt: '2024-01-15T10:00:00Z',
        geometry: {
          type: 'Polygon',
          coordinates: [[[20.0, 52.0], [20.1, 52.0], [20.1, 52.1], [20.0, 52.1], [20.0, 52.0]]],
        },
      };
      selectionState.areas.set([mockArea]);

      // Act: select an area (simulating what AreaListComponent does)
      selectionState.selectArea('area-1');

      // Assert: selectedArea computed signal returns the full area
      expect(selectionState.selectedAreaId()).toBe('area-1');
      expect(selectionState.selectedArea()).toEqual(mockArea);
    });

    it('should clear selectedPlanId when a new area is selected', () => {
      // Arrange: set a plan selection first
      selectionState.selectedPlanId.set('plan-1');

      // Act: select a new area
      selectionState.selectArea('area-2');

      // Assert: plan selection is reset
      expect(selectionState.selectedPlanId()).toBeNull();
      expect(selectionState.selectedPlan()).toBeNull();
    });

    it('should provide selectedArea with full geometry for map fit bounds', () => {
      const mockArea: AreaResponse = {
        id: 'area-abc',
        createdAt: '2024-06-01T08:00:00Z',
        geometry: {
          type: 'Polygon',
          coordinates: [[[19.9, 51.9], [20.2, 51.9], [20.2, 52.2], [19.9, 52.2], [19.9, 51.9]]],
        },
      };
      selectionState.areas.set([mockArea]);
      selectionState.selectArea('area-abc');

      // The MapComponent uses selectedArea().geometry.coordinates to compute fit bounds
      const area = selectionState.selectedArea();
      expect(area).not.toBeNull();
      expect(area!.geometry.coordinates[0].length).toBe(5);
    });
  });

  describe('Plan selection triggers flight path render', () => {
    it('should render waypoint features on VectorSource when renderFlightPath is called', () => {
      // Arrange: set up waypoints
      const mockWaypoints: WaypointDto[] = [
        { latitude: 52.0, longitude: 20.0, altitudeAglM: 100, gimbalPitchDegrees: -90, gimbalYawDegrees: 0 },
        { latitude: 52.01, longitude: 20.01, altitudeAglM: 100, gimbalPitchDegrees: -90, gimbalYawDegrees: 0 },
        { latitude: 52.02, longitude: 20.02, altitudeAglM: 100, gimbalPitchDegrees: -90, gimbalYawDegrees: 0 },
      ];

      // Act: render flight path directly (as FlightPathVisualizationService effect does on plan selection)
      flightPathViz.renderFlightPath(mockWaypoints);

      // Assert: features are added to the flight path source
      const features = flightPathViz.flightPathSource.getFeatures();
      // Should have 3 point features + 1 LineString feature = 4 total
      expect(features.length).toBe(4);

      // Verify point features have correct waypoint indices
      const pointFeatures = features.filter(f => f.getGeometry() instanceof Point);
      expect(pointFeatures.length).toBe(3);

      const lineFeatures = features.filter(f => f.getGeometry() instanceof LineString);
      expect(lineFeatures.length).toBe(1);
    });

    it('should clear flight path features when clearFlightPath is called', () => {
      // Arrange: render a flight path first
      const mockWaypoints: WaypointDto[] = [
        { latitude: 52.0, longitude: 20.0, altitudeAglM: 100, gimbalPitchDegrees: -90, gimbalYawDegrees: 0 },
      ];
      flightPathViz.renderFlightPath(mockWaypoints);
      expect(flightPathViz.flightPathSource.getFeatures().length).toBeGreaterThan(0);

      // Act: clear flight path (as happens on deselection/area change)
      flightPathViz.clearFlightPath();

      // Assert: all features cleared
      expect(flightPathViz.flightPathSource.getFeatures().length).toBe(0);
    });

    it('should replace previous flight path when rendering a new one', () => {
      // Arrange: render initial flight path
      const firstWaypoints: WaypointDto[] = [
        { latitude: 52.0, longitude: 20.0, altitudeAglM: 100, gimbalPitchDegrees: -90, gimbalYawDegrees: 0 },
        { latitude: 52.01, longitude: 20.01, altitudeAglM: 100, gimbalPitchDegrees: -90, gimbalYawDegrees: 0 },
      ];
      flightPathViz.renderFlightPath(firstWaypoints);
      expect(flightPathViz.flightPathSource.getFeatures().length).toBe(3); // 2 points + 1 line

      // Act: clear and render a different flight path (as happens when selecting different plan)
      flightPathViz.clearFlightPath();
      const secondWaypoints: WaypointDto[] = [
        { latitude: 53.0, longitude: 21.0, altitudeAglM: 80, gimbalPitchDegrees: -45, gimbalYawDegrees: 0 },
        { latitude: 53.01, longitude: 21.01, altitudeAglM: 80, gimbalPitchDegrees: -45, gimbalYawDegrees: 0 },
        { latitude: 53.02, longitude: 21.02, altitudeAglM: 80, gimbalPitchDegrees: -45, gimbalYawDegrees: 0 },
        { latitude: 53.03, longitude: 21.03, altitudeAglM: 80, gimbalPitchDegrees: -45, gimbalYawDegrees: 0 },
      ];
      flightPathViz.renderFlightPath(secondWaypoints);

      // Assert: new features are present
      const features = flightPathViz.flightPathSource.getFeatures();
      expect(features.length).toBe(5); // 4 points + 1 line
    });
  });

  describe('Form success shows toast', () => {
    it('should display a success toast when ToastService.show(success) is called', () => {
      const fixture = TestBed.createComponent(TestHostComponent);
      fixture.detectChanges();

      // Act: simulate what FlightPlanFormComponent does on success
      toastService.show('success', 'Plan lotu wygenerowany');
      fixture.detectChanges();

      // Assert: toast appears in the DOM
      const toastEl = fixture.nativeElement.querySelector('.toast--success');
      expect(toastEl).toBeTruthy();

      const messageEl = fixture.nativeElement.querySelector('.toast__message');
      expect(messageEl.textContent.trim()).toBe('Plan lotu wygenerowany');
    });
  });

  describe('Form error shows error toast', () => {
    it('should display an error toast when ToastService.show(error) is called', () => {
      const fixture = TestBed.createComponent(TestHostComponent);
      fixture.detectChanges();

      // Act: simulate what FlightPlanFormComponent does on error
      toastService.show('error', 'Wystąpił błąd podczas generowania planu lotu');
      fixture.detectChanges();

      // Assert: error toast appears in the DOM
      const toastEl = fixture.nativeElement.querySelector('.toast--error');
      expect(toastEl).toBeTruthy();

      expect(toastEl.getAttribute('role')).toBe('alert');
      expect(toastEl.getAttribute('aria-live')).toBe('assertive');

      const messageEl = fixture.nativeElement.querySelector('.toast__message');
      expect(messageEl.textContent.trim()).toBe('Wystąpił błąd podczas generowania planu lotu');
    });
  });

  describe('Responsive layout at desktop (≥1024px)', () => {
    it('should show side panel inline when expanded on desktop', () => {
      const fixture = TestBed.createComponent(SidePanelComponent);
      const component = fixture.componentInstance;

      // Simulate desktop viewport width (≥1024px)
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (component as any).viewportWidth.set(1280);
      panelState.expand();
      fixture.detectChanges();

      // On desktop, isTablet and isMobile should be false
      expect(component.isMobile()).toBeFalse();
      expect(component.isTablet()).toBeFalse();

      // Panel should be visible
      expect(component.isPanelVisible()).toBeTrue();

      // Side panel element should have expanded class
      const panelEl = fixture.nativeElement.querySelector('.side-panel');
      expect(panelEl).toBeTruthy();
      expect(panelEl.classList.contains('side-panel--expanded')).toBeTrue();
      expect(panelEl.classList.contains('side-panel--mobile')).toBeFalse();
      expect(panelEl.classList.contains('side-panel--tablet')).toBeFalse();
    });
  });

  describe('Responsive layout at tablet (768-1023px)', () => {
    it('should show FAB when panel is collapsed on tablet viewport', () => {
      const fixture = TestBed.createComponent(SidePanelComponent);
      const component = fixture.componentInstance;

      // Simulate tablet viewport by setting the private viewportWidth signal
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (component as any).viewportWidth.set(900);
      panelState.collapse();
      fixture.detectChanges();

      // Should be in tablet mode
      expect(component.isTablet()).toBeTrue();
      expect(component.isMobile()).toBeFalse();

      // FAB should be visible
      const fabEl = fixture.nativeElement.querySelector('.side-panel__fab');
      expect(fabEl).toBeTruthy();
      expect(fabEl.getAttribute('aria-label')).toBe('Toggle side panel');
    });

    it('should hide FAB and show panel when expanded on tablet', () => {
      const fixture = TestBed.createComponent(SidePanelComponent);
      const component = fixture.componentInstance;

      // Simulate tablet viewport with expanded panel
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (component as any).viewportWidth.set(900);
      panelState.expand();
      fixture.detectChanges();

      // FAB should not be visible (panel is expanded)
      const fabEl = fixture.nativeElement.querySelector('.side-panel__fab');
      expect(fabEl).toBeFalsy();

      // Panel should be visible with tablet class
      const panelEl = fixture.nativeElement.querySelector('.side-panel');
      expect(panelEl.classList.contains('side-panel--tablet')).toBeTrue();
      expect(panelEl.classList.contains('side-panel--visible')).toBeTrue();
    });
  });

  describe('Toast notifications are rendered in the container', () => {
    it('should render toasts from any caller via ToastService.show()', () => {
      const fixture = TestBed.createComponent(TestHostComponent);
      fixture.detectChanges();

      // Act: call show from service directly (simulating any component calling it)
      toastService.show('info', 'Test notification from service');
      fixture.detectChanges();

      // Assert: toast is visible in the DOM
      const toasts = fixture.nativeElement.querySelectorAll('.toast');
      expect(toasts.length).toBe(1);

      const toast = toasts[0];
      expect(toast.classList.contains('toast--info')).toBeTrue();
      expect(toast.getAttribute('role')).toBe('status');
      expect(toast.getAttribute('aria-live')).toBe('polite');
    });

    it('should render multiple toasts (up to 3 visible)', () => {
      const fixture = TestBed.createComponent(TestHostComponent);
      fixture.detectChanges();

      // Act: show 4 toasts
      toastService.show('success', 'First toast');
      toastService.show('error', 'Second toast');
      toastService.show('info', 'Third toast');
      toastService.show('success', 'Fourth toast (queued)');
      fixture.detectChanges();

      // Assert: only 3 visible
      const toasts = fixture.nativeElement.querySelectorAll('.toast');
      expect(toasts.length).toBe(3);
    });

    it('should dismiss toast via close button and promote queued toast', () => {
      const fixture = TestBed.createComponent(TestHostComponent);
      fixture.detectChanges();

      // Show 4 toasts (3 visible + 1 queued)
      toastService.show('success', 'First');
      toastService.show('error', 'Second');
      toastService.show('info', 'Third');
      toastService.show('success', 'Fourth (queued)');
      fixture.detectChanges();

      // Dismiss the first toast
      const closeBtn = fixture.nativeElement.querySelector('.toast__close');
      closeBtn.click();
      fixture.detectChanges();

      // Assert: still 3 visible (fourth promoted from queue)
      const toasts = fixture.nativeElement.querySelectorAll('.toast');
      expect(toasts.length).toBe(3);

      // The fourth toast should now be visible
      const messages = Array.from<Element>(fixture.nativeElement.querySelectorAll('.toast__message'))
        .map((el) => el.textContent!.trim());
      expect(messages).toContain('Fourth (queued)');
    });
  });
});
