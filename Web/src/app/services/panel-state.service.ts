import { Injectable, signal } from '@angular/core';

export type SidePanelSection = 'Area_List' | 'Flight_Plan_Form' | 'Flight_Plan_List';

@Injectable({ providedIn: 'root' })
export class PanelStateService {
  /** Whether the side panel is expanded (true) or collapsed to rail (false). */
  readonly isExpanded = signal(true);

  /** Collapsed state for each independently collapsible section. */
  readonly sectionCollapsed = {
    Area_List: signal(false),
    Flight_Plan_Form: signal(false),
    Flight_Plan_List: signal(false),
  } as const;

  /** Toggle the side panel between expanded and collapsed states. */
  toggle(): void {
    this.isExpanded.update((v) => !v);
  }

  /** Expand the side panel to full width. */
  expand(): void {
    this.isExpanded.set(true);
  }

  /** Collapse the side panel to the narrow rail. */
  collapse(): void {
    this.isExpanded.set(false);
  }

  /** Toggle a specific section's collapsed state. */
  toggleSection(section: SidePanelSection): void {
    this.sectionCollapsed[section].update((v) => !v);
  }

  /** Expand (uncollapse) a specific section. */
  expandSection(section: SidePanelSection): void {
    this.sectionCollapsed[section].set(false);
  }

  /** Collapse a specific section. */
  collapseSection(section: SidePanelSection): void {
    this.sectionCollapsed[section].set(true);
  }

  /** Check if a specific section is collapsed. */
  isSectionCollapsed(section: SidePanelSection): boolean {
    return this.sectionCollapsed[section]();
  }
}
