import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { FlightPlanListComponent } from './flight-plan-list.component';
import { FlightPlansApiService } from '../../api/services/flight-plans.service';
import { SelectionStateService } from '../../services/selection-state.service';
import { FlightPlanResponse } from '../../api/models/flight-plan-response';

describe('FlightPlanListComponent', () => {
  let component: FlightPlanListComponent;
  let fixture: ComponentFixture<FlightPlanListComponent>;
  let flightPlansApiSpy: jasmine.SpyObj<FlightPlansApiService>;
  let selectionState: SelectionStateService;

  const mockPlans: FlightPlanResponse[] = [
    {
      id: 'plan-1',
      areaId: 'area-1',
      mode: 'Grid',
      waypoints: [
        { latitude: 50.0, longitude: 20.0, altitudeAglM: 100, gimbalPitchDegrees: -90, gimbalYawDegrees: 0 },
        { latitude: 50.001, longitude: 20.001, altitudeAglM: 100, gimbalPitchDegrees: -90, gimbalYawDegrees: 0 },
      ],
      statistics: { totalDistanceM: 1234.5, estimatedFlightTimeS: 272, photoCount: 45, coveredAreaM2: 50000 },
      createdAt: '2024-01-16T14:00:00Z',
    },
    {
      id: 'plan-2',
      areaId: 'area-1',
      mode: 'Poi',
      waypoints: [
        { latitude: 50.0, longitude: 20.0, altitudeAglM: 80, gimbalPitchDegrees: -45, gimbalYawDegrees: 0 },
      ],
      statistics: { totalDistanceM: 567.8, estimatedFlightTimeS: 135, photoCount: 20, coveredAreaM2: 30000 },
      createdAt: '2024-01-15T10:00:00Z',
    },
  ];

  beforeEach(async () => {
    flightPlansApiSpy = jasmine.createSpyObj('FlightPlansApiService', ['list', 'exportMissionFile']);
    flightPlansApiSpy.list.and.returnValue(of(mockPlans));

    await TestBed.configureTestingModule({
      imports: [FlightPlanListComponent],
      providers: [
        { provide: FlightPlansApiService, useValue: flightPlansApiSpy },
      ],
    }).compileComponents();

    selectionState = TestBed.inject(SelectionStateService);
    fixture = TestBed.createComponent(FlightPlanListComponent);
    component = fixture.componentInstance;
  });

  describe('no area selected (empty state)', () => {
    it('should show "Wybierz obszar" empty state when no area is selected', () => {
      fixture.detectChanges();

      const emptyState = fixture.nativeElement.querySelector('app-empty-state');
      expect(emptyState).toBeTruthy();
      expect(emptyState.getAttribute('heading')).toBe('Wybierz obszar');
    });

    it('should not call FlightPlansApiService.list when no area selected', () => {
      fixture.detectChanges();
      expect(flightPlansApiSpy.list).not.toHaveBeenCalled();
    });
  });

  describe('loading state', () => {
    it('should show 2 skeleton items while loading', () => {
      const plansSubject = new Subject<FlightPlanResponse[]>();
      flightPlansApiSpy.list.and.returnValue(plansSubject.asObservable());

      selectionState.selectArea('area-1');
      fixture.detectChanges();

      const skeletons = fixture.nativeElement.querySelectorAll('app-skeleton');
      expect(skeletons.length).toBe(2);
    });

    it('should show loading aria-label with role="status" while loading', () => {
      const plansSubject = new Subject<FlightPlanResponse[]>();
      flightPlansApiSpy.list.and.returnValue(plansSubject.asObservable());

      selectionState.selectArea('area-1');
      fixture.detectChanges();

      const loadingContainer = fixture.nativeElement.querySelector('[role="status"]');
      expect(loadingContainer).toBeTruthy();
    });
  });

  describe('error state', () => {
    beforeEach(() => {
      flightPlansApiSpy.list.and.returnValue(throwError(() => new Error('Network error')));
      selectionState.selectArea('area-1');
      fixture.detectChanges();
    });

    it('should show error message on API error', () => {
      const errorMsg = fixture.nativeElement.querySelector('.flight-plan-list__error-message');
      expect(errorMsg).toBeTruthy();
      expect(errorMsg.textContent.trim()).toBe('Network error');
    });

    it('should show error with role="alert"', () => {
      const errorContainer = fixture.nativeElement.querySelector('[role="alert"]');
      expect(errorContainer).toBeTruthy();
    });

    it('should show retry button on error', () => {
      const retryBtn = fixture.nativeElement.querySelector('.flight-plan-list__retry-btn');
      expect(retryBtn).toBeTruthy();
      expect(retryBtn.textContent.trim()).toBe('Ponów');
    });

    it('should re-fetch plans when retry button is clicked', () => {
      flightPlansApiSpy.list.and.returnValue(of(mockPlans));

      const retryBtn = fixture.nativeElement.querySelector('.flight-plan-list__retry-btn');
      retryBtn.click();
      fixture.detectChanges();

      const items = fixture.nativeElement.querySelectorAll('.flight-plan-list__item');
      expect(items.length).toBe(2);
    });
  });

  describe('empty plans state', () => {
    it('should show "Brak planów lotu" empty state when no plans exist for selected area', () => {
      flightPlansApiSpy.list.and.returnValue(of([]));
      selectionState.selectArea('area-1');
      fixture.detectChanges();

      const emptyState = fixture.nativeElement.querySelector('app-empty-state');
      expect(emptyState).toBeTruthy();
      expect(emptyState.getAttribute('heading')).toBe('Brak planów lotu');
    });
  });

  describe('loaded state', () => {
    beforeEach(() => {
      selectionState.selectArea('area-1');
      fixture.detectChanges();
    });

    it('should render plan items with mode label', () => {
      const modes = fixture.nativeElement.querySelectorAll('.flight-plan-list__mode');
      expect(modes.length).toBe(2);
      // Plans sorted by createdAt desc: plan-1 first (2024-01-16), plan-2 second (2024-01-15)
      expect(modes[0].textContent.trim()).toContain('Grid');
      expect(modes[1].textContent.trim()).toContain('POI');
    });

    it('should render plan items with relative date', () => {
      const dates = fixture.nativeElement.querySelectorAll('.flight-plan-list__date');
      expect(dates.length).toBe(2);
      dates.forEach((dateEl: HTMLElement) => {
        expect(dateEl.textContent!.trim().length).toBeGreaterThan(0);
      });
    });

    it('should render plan items with total distance', () => {
      const stats = fixture.nativeElement.querySelectorAll('.flight-plan-list__item-stats');
      // plan-1 totalDistanceM=1234.5 → should be rounded to 1235 m
      expect(stats[0].textContent).toContain('1235 m');
      // plan-2 totalDistanceM=567.8 → should be rounded to 568 m
      expect(stats[1].textContent).toContain('568 m');
    });

    it('should render plan items with formatted flight time', () => {
      const stats = fixture.nativeElement.querySelectorAll('.flight-plan-list__item-stats');
      // plan-1: 272s → "4 min 32 s"
      expect(stats[0].textContent).toContain('4 min 32 s');
      // plan-2: 135s → "2 min 15 s"
      expect(stats[1].textContent).toContain('2 min 15 s');
    });

    it('should render plan items with photo count', () => {
      const stats = fixture.nativeElement.querySelectorAll('.flight-plan-list__item-stats');
      expect(stats[0].textContent).toContain('45');
      expect(stats[1].textContent).toContain('20');
    });

    it('should have role="listbox" on the container', () => {
      const container = fixture.nativeElement.querySelector('[role="listbox"]');
      expect(container).toBeTruthy();
    });

    it('should have role="option" on each item', () => {
      const options = fixture.nativeElement.querySelectorAll('[role="option"]');
      expect(options.length).toBe(2);
    });
  });

  describe('selection', () => {
    beforeEach(() => {
      selectionState.selectArea('area-1');
      fixture.detectChanges();
    });

    it('should highlight selected plan with --selected class', () => {
      const items = fixture.nativeElement.querySelectorAll('.flight-plan-list__item');
      items[0].click();
      fixture.detectChanges();

      expect(items[0].classList.contains('flight-plan-list__item--selected')).toBeTrue();
    });

    it('should show "Eksportuj" button when a plan is selected', () => {
      const items = fixture.nativeElement.querySelectorAll('.flight-plan-list__item');
      items[0].click();
      fixture.detectChanges();

      const exportBtn = fixture.nativeElement.querySelector('.flight-plan-list__export-btn');
      expect(exportBtn).toBeTruthy();
      expect(exportBtn.textContent.trim()).toBe('Eksportuj');
    });

    it('should update SelectionStateService on plan click', () => {
      const items = fixture.nativeElement.querySelectorAll('.flight-plan-list__item');
      items[0].click();
      fixture.detectChanges();

      // Plans sorted desc: first item = plan-1
      expect(selectionState.selectedPlanId()).toBe('plan-1');
    });

    it('should set aria-selected on the selected item', () => {
      const items = fixture.nativeElement.querySelectorAll('.flight-plan-list__item');
      items[0].click();
      fixture.detectChanges();

      expect(items[0].getAttribute('aria-selected')).toBe('true');
      expect(items[1].getAttribute('aria-selected')).toBe('false');
    });
  });

  describe('keyboard navigation', () => {
    beforeEach(() => {
      selectionState.selectArea('area-1');
      fixture.detectChanges();
    });

    function dispatchKeydown(key: string): void {
      const container = fixture.nativeElement.querySelector('[role="listbox"]');
      const event = new KeyboardEvent('keydown', { key, bubbles: true });
      container.dispatchEvent(event);
      fixture.detectChanges();
    }

    it('should move focusedIndex forward on ArrowDown (wrapping)', () => {
      dispatchKeydown('ArrowDown'); // -1 → 0
      expect(component.focusedIndex()).toBe(0);

      dispatchKeydown('ArrowDown'); // 0 → 1
      expect(component.focusedIndex()).toBe(1);

      dispatchKeydown('ArrowDown'); // 1 → 0 (wraps)
      expect(component.focusedIndex()).toBe(0);
    });

    it('should move focusedIndex backward on ArrowUp (wrapping)', () => {
      dispatchKeydown('ArrowDown'); // -1 → 0
      expect(component.focusedIndex()).toBe(0);

      dispatchKeydown('ArrowUp'); // 0 → 1 (wraps to end)
      expect(component.focusedIndex()).toBe(1);

      dispatchKeydown('ArrowUp'); // 1 → 0
      expect(component.focusedIndex()).toBe(0);
    });

    it('should select the focused item on Enter', () => {
      dispatchKeydown('ArrowDown'); // -1 → 0
      dispatchKeydown('Enter');

      expect(selectionState.selectedPlanId()).toBe('plan-1');
    });

    it('should select the focused item on Space', () => {
      dispatchKeydown('ArrowDown'); // -1 → 0
      dispatchKeydown('ArrowDown'); // 0 → 1
      dispatchKeydown(' ');

      expect(selectionState.selectedPlanId()).toBe('plan-2');
    });

    it('should update aria-activedescendant when focus changes', () => {
      dispatchKeydown('ArrowDown'); // -1 → 0
      fixture.detectChanges();

      const container = fixture.nativeElement.querySelector('[role="listbox"]');
      const activeDescendant = container.getAttribute('aria-activedescendant');
      expect(activeDescendant).toBe('plan-item-plan-1');
    });
  });

  describe('re-fetch on area change', () => {
    it('should re-fetch plans when selectedAreaId changes', () => {
      selectionState.selectArea('area-1');
      fixture.detectChanges();

      expect(flightPlansApiSpy.list).toHaveBeenCalledWith({ areaId: 'area-1' });

      flightPlansApiSpy.list.calls.reset();
      selectionState.selectArea('area-2');
      fixture.detectChanges();

      expect(flightPlansApiSpy.list).toHaveBeenCalledWith({ areaId: 'area-2' });
    });

    it('should show loading state when area changes', () => {
      selectionState.selectArea('area-1');
      fixture.detectChanges();

      const plansSubject = new Subject<FlightPlanResponse[]>();
      flightPlansApiSpy.list.and.returnValue(plansSubject.asObservable());

      selectionState.selectArea('area-2');
      fixture.detectChanges();

      const skeletons = fixture.nativeElement.querySelectorAll('app-skeleton');
      expect(skeletons.length).toBe(2);
    });
  });
});
