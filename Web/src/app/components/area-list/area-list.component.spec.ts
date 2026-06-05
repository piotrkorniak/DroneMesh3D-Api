import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { AreaListComponent } from './area-list.component';
import { AreasApiService } from '../../api/services/areas.service';
import { SelectionStateService } from '../../services/selection-state.service';
import { AreaResponse } from '../../api/models/area-response';

describe('AreaListComponent', () => {
  let component: AreaListComponent;
  let fixture: ComponentFixture<AreaListComponent>;
  let areasApiSpy: jasmine.SpyObj<AreasApiService>;
  let selectionState: SelectionStateService;

  const mockAreas: AreaResponse[] = [
    {
      id: 'area-1',
      createdAt: '2024-01-15T10:00:00Z',
      geometry: {
        type: 'Polygon',
        coordinates: [[[20.0, 50.0], [20.1, 50.0], [20.1, 50.1], [20.0, 50.1], [20.0, 50.0]]],
      },
    },
    {
      id: 'area-2',
      createdAt: '2024-01-16T12:00:00Z',
      geometry: {
        type: 'Polygon',
        coordinates: [[[21.0, 51.0], [21.2, 51.0], [21.2, 51.2], [21.0, 51.2], [21.0, 51.0]]],
      },
    },
    {
      id: 'area-3',
      createdAt: '2024-01-14T08:00:00Z',
      geometry: {
        type: 'Polygon',
        coordinates: [[[19.0, 49.0], [19.05, 49.0], [19.05, 49.05], [19.0, 49.05], [19.0, 49.0]]],
      },
    },
  ];

  beforeEach(async () => {
    areasApiSpy = jasmine.createSpyObj('AreasApiService', ['listAreas']);
    areasApiSpy.listAreas.and.returnValue(of(mockAreas));

    await TestBed.configureTestingModule({
      imports: [AreaListComponent],
      providers: [
        { provide: AreasApiService, useValue: areasApiSpy },
      ],
    }).compileComponents();

    selectionState = TestBed.inject(SelectionStateService);
    fixture = TestBed.createComponent(AreaListComponent);
    component = fixture.componentInstance;
  });

  describe('loading state', () => {
    it('should show 3 skeleton items while loading', () => {
      // Use a Subject that never emits to keep the component in loading state
      const areasSubject = new Subject<AreaResponse[]>();
      areasApiSpy.listAreas.and.returnValue(areasSubject.asObservable());

      // Re-create fixture so ngOnInit uses the non-emitting observable
      fixture = TestBed.createComponent(AreaListComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      const skeletons = fixture.nativeElement.querySelectorAll('app-skeleton');
      expect(skeletons.length).toBe(3);
    });
  });

  describe('loaded state', () => {
    beforeEach(() => {
      fixture.detectChanges(); // triggers ngOnInit → loadAreas
    });

    it('should render area items when data is loaded', () => {
      const items = fixture.nativeElement.querySelectorAll('.area-list__item');
      expect(items.length).toBe(3);
    });

    it('should display sequential number on each area item', () => {
      const numbers = fixture.nativeElement.querySelectorAll('.area-list__number');
      expect(numbers[0].textContent.trim()).toBe('1.');
      expect(numbers[1].textContent.trim()).toBe('2.');
      expect(numbers[2].textContent.trim()).toBe('3.');
    });

    it('should display date using relativeTime pipe', () => {
      const dates = fixture.nativeElement.querySelectorAll('.area-list__date');
      expect(dates.length).toBe(3);
      // Each date element should have non-empty text content
      dates.forEach((dateEl: HTMLElement) => {
        expect(dateEl.textContent!.trim().length).toBeGreaterThan(0);
      });
    });

    it('should display hectares for each area', () => {
      const hectares = fixture.nativeElement.querySelectorAll('.area-list__hectares');
      expect(hectares.length).toBe(3);
      hectares.forEach((el: HTMLElement) => {
        expect(el.textContent!.trim()).toMatch(/[\d.]+ ha/);
      });
    });

    it('should have role="listbox" on the container', () => {
      const container = fixture.nativeElement.querySelector('[role="listbox"]');
      expect(container).toBeTruthy();
    });

    it('should have role="option" on each item', () => {
      const options = fixture.nativeElement.querySelectorAll('[role="option"]');
      expect(options.length).toBe(3);
    });
  });

  describe('selection', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should highlight selected item with area-list__item--selected class', () => {
      const items = fixture.nativeElement.querySelectorAll('.area-list__item');
      items[0].click();
      fixture.detectChanges();

      expect(items[0].classList.contains('area-list__item--selected')).toBeTrue();
    });

    it('should update SelectionStateService when an item is clicked', () => {
      const items = fixture.nativeElement.querySelectorAll('.area-list__item');
      items[0].click();
      fixture.detectChanges();

      // The areas are sorted by createdAt desc, so the first item is area-2 (newest)
      expect(selectionState.selectedAreaId()).toBe('area-2');
    });

    it('should set aria-selected on the selected item', () => {
      const items = fixture.nativeElement.querySelectorAll('.area-list__item');
      items[0].click();
      fixture.detectChanges();

      expect(items[0].getAttribute('aria-selected')).toBe('true');
      expect(items[1].getAttribute('aria-selected')).toBe('false');
    });
  });

  describe('error state', () => {
    beforeEach(() => {
      areasApiSpy.listAreas.and.returnValue(throwError(() => new Error('Network error')));
      fixture.detectChanges();
    });

    it('should show error message on API error', () => {
      const errorMsg = fixture.nativeElement.querySelector('.area-list__error-message');
      expect(errorMsg).toBeTruthy();
      expect(errorMsg.textContent.trim()).toBe('Network error');
    });

    it('should show retry button on error', () => {
      const retryBtn = fixture.nativeElement.querySelector('.area-list__retry-btn');
      expect(retryBtn).toBeTruthy();
    });

    it('should re-fetch areas when retry button is clicked', () => {
      areasApiSpy.listAreas.and.returnValue(of(mockAreas));

      const retryBtn = fixture.nativeElement.querySelector('.area-list__retry-btn');
      retryBtn.click();
      fixture.detectChanges();

      expect(areasApiSpy.listAreas).toHaveBeenCalledTimes(2); // initial + retry
      const items = fixture.nativeElement.querySelectorAll('.area-list__item');
      expect(items.length).toBe(3);
    });
  });

  describe('empty state', () => {
    beforeEach(() => {
      areasApiSpy.listAreas.and.returnValue(of([]));
      fixture.detectChanges();
    });

    it('should show EmptyStateComponent when no areas returned', () => {
      const emptyState = fixture.nativeElement.querySelector('app-empty-state');
      expect(emptyState).toBeTruthy();
    });
  });

  describe('keyboard navigation', () => {
    beforeEach(() => {
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

      dispatchKeydown('ArrowDown'); // 1 → 2
      expect(component.focusedIndex()).toBe(2);

      dispatchKeydown('ArrowDown'); // 2 → 0 (wraps)
      expect(component.focusedIndex()).toBe(0);
    });

    it('should move focusedIndex backward on ArrowUp (wrapping)', () => {
      dispatchKeydown('ArrowDown'); // -1 → 0
      expect(component.focusedIndex()).toBe(0);

      dispatchKeydown('ArrowUp'); // 0 → 2 (wraps to end)
      expect(component.focusedIndex()).toBe(2);

      dispatchKeydown('ArrowUp'); // 2 → 1
      expect(component.focusedIndex()).toBe(1);
    });

    it('should select the focused item on Enter', () => {
      dispatchKeydown('ArrowDown'); // -1 → 0
      dispatchKeydown('Enter');

      // First area after sorting is area-2 (newest createdAt)
      expect(selectionState.selectedAreaId()).toBe('area-2');
    });

    it('should select the focused item on Space', () => {
      dispatchKeydown('ArrowDown'); // -1 → 0
      dispatchKeydown('ArrowDown'); // 0 → 1
      dispatchKeydown(' ');

      // Second item after sorting is area-1
      expect(selectionState.selectedAreaId()).toBe('area-1');
    });

    it('should update aria-activedescendant when focus changes', () => {
      dispatchKeydown('ArrowDown'); // -1 → 0
      fixture.detectChanges();

      const container = fixture.nativeElement.querySelector('[role="listbox"]');
      const activeDescendant = container.getAttribute('aria-activedescendant');
      // First sorted area is area-2
      expect(activeDescendant).toBe('area-item-area-2');
    });
  });
});
