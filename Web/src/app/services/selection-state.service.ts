import { computed, Injectable, signal } from '@angular/core';
import { AreaResponse } from '../api/models/area-response';
import { FlightPlanResponse } from '../api/models/flight-plan-response';

@Injectable({ providedIn: 'root' })
export class SelectionStateService {
  /** Currently selected area ID */
  readonly selectedAreaId = signal<string | null>(null);

  /** Currently selected flight plan ID */
  readonly selectedPlanId = signal<string | null>(null);

  /** Cached areas list — populated by AreaListComponent after fetch */
  readonly areas = signal<AreaResponse[]>([]);

  /** Cached flight plans list — populated by FlightPlanListComponent after fetch */
  readonly plans = signal<FlightPlanResponse[]>([]);

  /** Computed: full AreaResponse for the selected area */
  readonly selectedArea = computed<AreaResponse | null>(() => {
    const id = this.selectedAreaId();
    if (!id) return null;
    return this.areas().find(a => a.id === id) ?? null;
  });

  /** Computed: full FlightPlanResponse for the selected plan */
  readonly selectedPlan = computed<FlightPlanResponse | null>(() => {
    const id = this.selectedPlanId();
    if (!id) return null;
    return this.plans().find(p => p.id === id) ?? null;
  });

  /** Select an area — also resets the selected flight plan */
  selectArea(id: string | null): void {
    this.selectedAreaId.set(id);
    this.selectedPlanId.set(null);
  }

  /** Select a flight plan */
  selectPlan(id: string | null): void {
    this.selectedPlanId.set(id);
  }
}
