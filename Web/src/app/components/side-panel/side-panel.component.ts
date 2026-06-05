import {
  ChangeDetectionStrategy,
  Component,
  computed,
  HostListener,
  inject,
  signal,
} from '@angular/core';
import { PanelStateService } from '../../services/panel-state.service';
import { AreaListComponent } from '../area-list/area-list.component';
import { FlightPlanFormComponent } from '../flight-plan-form/flight-plan-form.component';
import { FlightPlanListComponent } from '../flight-plan-list/flight-plan-list.component';

@Component({
  selector: 'app-side-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AreaListComponent, FlightPlanFormComponent, FlightPlanListComponent],
  templateUrl: './side-panel.component.html',
  styleUrl: './side-panel.component.scss',
})
export class SidePanelComponent {
  readonly panelState = inject(PanelStateService);

  /** Track viewport width for responsive behavior */
  private readonly viewportWidth = signal(
    typeof window !== 'undefined' ? window.innerWidth : 1024
  );

  /** Whether the viewport is mobile (<768px) */
  readonly isMobile = computed(() => this.viewportWidth() < 768);

  /** Whether the viewport is tablet (768-1023px) */
  readonly isTablet = computed(() => {
    const w = this.viewportWidth();
    return w >= 768 && w < 1024;
  });

  /** Whether the panel should be visible (for mobile/tablet overlay modes) */
  readonly isPanelVisible = computed(() => {
    if (this.isMobile() || this.isTablet()) {
      return this.panelState.isExpanded();
    }
    return true; // Desktop always visible
  });

  /** Whether the mobile overlay backdrop should show */
  readonly isMobileOverlayVisible = computed(
    () => this.isMobile() && this.panelState.isExpanded()
  );

  @HostListener('window:resize')
  onResize(): void {
    this.viewportWidth.set(window.innerWidth);
  }

  /** Toggle the panel expanded/collapsed state */
  togglePanel(): void {
    this.panelState.toggle();
  }

  /** Close the mobile overlay */
  closeMobileOverlay(): void {
    this.panelState.collapse();
  }
}
