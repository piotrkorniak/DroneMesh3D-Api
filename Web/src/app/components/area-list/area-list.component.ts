import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { AreaResponse } from '../../api/models/area-response';
import { AreasApiService } from '../../api/services/areas.service';
import { SelectionStateService } from '../../services/selection-state.service';
import { AreaCalculationService } from '../../services/area-calculation.service';
import { RelativeTimePipe } from '../../pipes/relative-time.pipe';
import { SkeletonComponent } from '../skeleton/skeleton.component';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { sortByCreatedAtDesc } from '../../utils/sort-by-date';

/**
 * AreaListComponent displays a list of saved areas sorted by creation date descending.
 * Supports keyboard navigation, ARIA listbox pattern, loading/error/empty states,
 * and virtual scrolling for large lists (>50 items).
 */
@Component({
  selector: 'app-area-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RelativeTimePipe, SkeletonComponent, EmptyStateComponent],
  templateUrl: './area-list.component.html',
  styleUrl: './area-list.component.scss',
})
export class AreaListComponent implements OnInit {
  private readonly areasApi = inject(AreasApiService);
  private readonly areaCalcService = inject(AreaCalculationService);
  readonly selectionState = inject(SelectionStateService);

  /** Areas displayed in the list — derived from SelectionStateService (single source of truth) */
  readonly areas = computed(() => this.selectionState.areas());
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly focusedIndex = signal(-1);

  readonly skeletonItems = [0, 1, 2];

  /** Item height in pixels for virtual scrolling */
  private readonly ITEM_HEIGHT = 56;
  /** Number of buffer items rendered above/below viewport */
  private readonly BUFFER_COUNT = 5;

  readonly scrollTop = signal(0);

  readonly useVirtualScroll = computed(() => this.areas().length > 50);

  readonly totalHeight = computed(() => this.areas().length * this.ITEM_HEIGHT);

  readonly visibleItems = computed(() => {
    const allAreas = this.areas();
    if (!this.useVirtualScroll()) return [];

    const top = this.scrollTop();
    const containerHeight = 400; // matches max-height in CSS
    const startIdx = Math.max(0, Math.floor(top / this.ITEM_HEIGHT) - this.BUFFER_COUNT);
    const endIdx = Math.min(
      allAreas.length,
      Math.ceil((top + containerHeight) / this.ITEM_HEIGHT) + this.BUFFER_COUNT
    );

    return allAreas.slice(startIdx, endIdx).map((area, i) => ({
      area,
      index: startIdx + i,
    }));
  });

  readonly offsetY = computed(() => {
    if (!this.useVirtualScroll()) return 0;
    const top = this.scrollTop();
    const startIdx = Math.max(0, Math.floor(top / this.ITEM_HEIGHT) - this.BUFFER_COUNT);
    return startIdx * this.ITEM_HEIGHT;
  });

  readonly activeDescendantId = computed(() => {
    const idx = this.focusedIndex();
    const areasList = this.areas();
    if (idx >= 0 && idx < areasList.length) {
      return 'area-item-' + areasList[idx].id;
    }
    return null;
  });

  /** Cache for hectares computation to avoid recalculating on every change detection */
  private readonly hectaresCache = new Map<string, number>();

  ngOnInit(): void {
    this.loadAreas();
  }

  loadAreas(): void {
    this.loading.set(true);
    this.error.set(null);

    this.areasApi.listAreas().subscribe({
      next: (response) => {
        const sorted = sortByCreatedAtDesc(response);
        this.selectionState.areas.set(sorted);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.message || 'Nie udało się pobrać listy obszarów');
        this.loading.set(false);
      },
    });
  }

  selectArea(id: string, index: number): void {
    this.selectionState.selectArea(id);
    this.focusedIndex.set(index);
  }

  getHectares(area: AreaResponse): number {
    const cached = this.hectaresCache.get(area.id);
    if (cached !== undefined) {
      return cached;
    }
    const coordinates = area.geometry?.coordinates?.[0] as [number, number][] | undefined;
    const hectares = coordinates ? this.areaCalcService.calculateHectares(coordinates) : 0;
    this.hectaresCache.set(area.id, hectares);
    return hectares;
  }

  onScroll(event: Event): void {
    const target = event.target as HTMLElement;
    this.scrollTop.set(target.scrollTop);
  }

  onKeydown(event: KeyboardEvent): void {
    const areasList = this.areas();
    const n = areasList.length;
    if (n === 0) return;

    const currentIndex = this.focusedIndex();

    switch (event.key) {
      case 'ArrowDown': {
        event.preventDefault();
        const nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % n;
        this.focusedIndex.set(nextIndex);
        this.scrollToItem(nextIndex);
        break;
      }
      case 'ArrowUp': {
        event.preventDefault();
        const prevIndex = currentIndex <= 0 ? n - 1 : (currentIndex - 1 + n) % n;
        this.focusedIndex.set(prevIndex);
        this.scrollToItem(prevIndex);
        break;
      }
      case 'Enter':
      case ' ': {
        event.preventDefault();
        const idx = this.focusedIndex();
        if (idx >= 0 && idx < n) {
          this.selectArea(areasList[idx].id, idx);
        }
        break;
      }
    }
  }

  private scrollToItem(index: number): void {
    const areasList = this.areas();
    if (index >= 0 && index < areasList.length) {
      const elementId = 'area-item-' + areasList[index].id;
      // Use requestAnimationFrame to ensure the DOM is updated before scrolling
      requestAnimationFrame(() => {
        const el = document.getElementById(elementId);
        el?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      });
    }
  }
}
