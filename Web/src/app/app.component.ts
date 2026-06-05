import { Component, computed, inject } from '@angular/core';
import { MapComponent } from './components/map/map.component';
import { SidePanelComponent } from './components/side-panel/side-panel.component';
import { ToastContainerComponent } from './components/toast-container/toast-container.component';
import { PanelStateService } from './services/panel-state.service';
import { SelectionStateService } from './services/selection-state.service';
import { LiveAnnouncerService } from './services/live-announcer.service';

@Component({
  selector: 'app-root',
  imports: [MapComponent, SidePanelComponent, ToastContainerComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  title = 'dronemesh3d-web';
  panelState = inject(PanelStateService);
  private readonly selectionState = inject(SelectionStateService);
  private readonly liveAnnouncer = inject(LiveAnnouncerService);

  /** Whether a modal overlay is currently open (export dialog or mobile side panel) */
  readonly isModalOpen = computed(() => {
    // Mobile overlay counts as modal
    const isMobileOverlay = typeof window !== 'undefined'
      && window.innerWidth < 768
      && this.panelState.isExpanded();
    return isMobileOverlay;
  });

  /** Live announcement text for aria-live region */
  readonly liveAnnouncement = computed(() => this.liveAnnouncer.announcement());
}
