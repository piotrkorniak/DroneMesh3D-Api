import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FlightPlansApiService } from '../../api/services/flight-plans.service';
import { SelectionStateService } from '../../services/selection-state.service';
import { RelativeTimePipe } from '../../pipes/relative-time.pipe';
import { SkeletonComponent } from '../skeleton/skeleton.component';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { ExportDialogComponent } from '../export-dialog/export-dialog.component';
import { sortByCreatedAtDesc } from '../../utils/sort-by-date';
import { formatFlightTime } from '../../utils/format-flight-time';
import { Subscription } from 'rxjs';

/**
 * FlightPlanListComponent displays the history of generated flight plans for the selected area.
 * Supports keyboard navigation, ARIA listbox pattern, loading/error/empty states.
 * Re-fetches when selectedAreaId changes, discarding previous data and showing loading.
 */
@Component({
  selector: 'app-flight-plan-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RelativeTimePipe, SkeletonComponent, EmptyStateComponent, ExportDialogComponent],
  templateUrl: './flight-plan-list.component.html',
  styleUrl: './flight-plan-list.component.scss',
})
export class FlightPlanListComponent {
  private readonly flightPlansApi = inject(FlightPlansApiService);
  readonly selectionState = inject(SelectionStateService);

  /** Plans displayed in the list — derived from SelectionStateService (single source of truth) */
  readonly plans = computed(() => this.selectionState.plans());
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly focusedIndex = signal(-1);
  readonly exportTriggeredPlanId = signal<string | null>(null);

  readonly skeletonItems = [0, 1];

  /** Expose Math for template usage */
  readonly Math = Math;

  private currentSubscription: Subscription | null = null;

  readonly activeDescendantId = computed(() => {
    const idx = this.focusedIndex();
    const plansList = this.plans();
    if (idx >= 0 && idx < plansList.length) {
      return 'plan-item-' + plansList[idx].id;
    }
    return null;
  });

  constructor() {
    // React to selectedAreaId changes
    effect(() => {
      const areaId = this.selectionState.selectedAreaId();
      if (areaId) {
        this.loadPlans();
      } else {
        // No area selected — clear plans
        this.selectionState.plans.set([]);
        this.loading.set(false);
        this.error.set(null);
        this.focusedIndex.set(-1);
      }
    });
  }

  loadPlans(): void {
    const areaId = this.selectionState.selectedAreaId();
    if (!areaId) return;

    // Discard current data and show loading
    this.selectionState.plans.set([]);
    this.loading.set(true);
    this.error.set(null);
    this.focusedIndex.set(-1);

    // Cancel previous pending request
    if (this.currentSubscription) {
      this.currentSubscription.unsubscribe();
      this.currentSubscription = null;
    }

    this.currentSubscription = this.flightPlansApi.list({ areaId }).subscribe({
      next: (response) => {
        const sorted = sortByCreatedAtDesc(response);
        this.selectionState.plans.set(sorted);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.message || 'Nie udało się pobrać planów lotu');
        this.loading.set(false);
      },
    });
  }

  selectPlan(id: string, index: number): void {
    this.selectionState.selectPlan(id);
    this.focusedIndex.set(index);
  }

  getFlightTime(totalSeconds: number): string {
    return formatFlightTime(totalSeconds);
  }

  onExportClick(event: Event, planId: string): void {
    event.stopPropagation();
    this.exportTriggeredPlanId.set(planId);
  }

  closeExportDialog(): void {
    this.exportTriggeredPlanId.set(null);
  }

  onKeydown(event: KeyboardEvent): void {
    const plansList = this.plans();
    const n = plansList.length;
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
          this.selectPlan(plansList[idx].id, idx);
        }
        break;
      }
    }
  }

  private scrollToItem(index: number): void {
    const plansList = this.plans();
    if (index >= 0 && index < plansList.length) {
      const elementId = 'plan-item-' + plansList[index].id;
      requestAnimationFrame(() => {
        const el = document.getElementById(elementId);
        el?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      });
    }
  }
}
