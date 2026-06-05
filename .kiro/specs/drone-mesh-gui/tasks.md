# Implementation Plan: drone-mesh-gui

## Overview

Implement the complete GUI layer for DroneMesh3D — a dark-themed, professional drone flight planning interface built with Angular 21 (zoneless, signals, standalone components), OpenLayers 10.9, and SCSS design tokens. The implementation follows a bottom-up dependency order: design tokens → state services → shared components → feature components → integration wiring.

## Tasks

- [x] 1. Design system tokens and shared utilities
  - [x] 1.1 Create `_tokens.scss` design system file
    - Create `Web/src/styles/_tokens.scss` declaring all CSS custom properties on `:root`
    - Include colors (primary, primary-hover, secondary, surface, surface-elevated, background, text-primary, text-secondary, text-muted, border, success, warning, error, overlay), spacing (xs through 3xl at 4px increments), typography (font-family, sizes xs–2xl, weights, line-heights), border-radius (sm, md, lg, xl), box-shadow (sm, md, lg), transitions (fast, normal, slow), and breakpoints (tablet, desktop)
    - Use dark theme with surface colors in hsl(220, 15-25%, 8-18%) range
    - Import from `Web/src/styles.scss` globally
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 10.6_

  - [x] 1.2 Create utility functions: `sortByCreatedAtDesc`, `formatFlightTime`, `truncateMessage`, and `rangeValidator`
    - Create `Web/src/app/utils/sort-by-date.ts` — generic sort function for arrays by `createdAt` descending
    - Create `Web/src/app/utils/format-flight-time.ts` — formats totalSeconds to `"X min Y s"` string
    - Create `Web/src/app/utils/truncate-message.ts` — truncates strings > 150 chars with ellipsis
    - Create `Web/src/app/utils/range-validator.ts` — Angular validator factory for numeric min/max range validation
    - _Requirements: 3.1, 5.1, 5.2, 4.3, 4.4, 8.9_

  - [x] 1.3 Write property tests for utility functions (Properties 1, 4, 6, 11)
    - **Property 1: List items are sorted by creation date descending** — Validates: Requirements 3.1, 5.1
    - **Property 4: Numeric range validation correctly classifies values** — Validates: Requirements 4.3, 4.4
    - **Property 6: Flight time formatting is correct** — Validates: Requirements 5.2
    - **Property 11: Toast message truncation at 150 characters** — Validates: Requirements 8.9
    - Create `Web/src/app/utils/sort-by-date.spec.ts`, `range-validator.spec.ts`, `format-flight-time.spec.ts`, `truncate-message.spec.ts` using fast-check with minimum 100 iterations per property

- [x] 2. Core state services
  - [x] 2.1 Implement `PanelStateService`
    - Create `Web/src/app/services/panel-state.service.ts`
    - Manage `isExpanded` WritableSignal (default: true)
    - Provide `toggle()`, `expand()`, `collapse()` methods
    - Track collapsed section states (Area_List, Flight_Plan_Form, Flight_Plan_List) via signals
    - _Requirements: 2.2, 2.8_

  - [x] 2.2 Implement `SelectionStateService`
    - Create `Web/src/app/services/selection-state.service.ts`
    - Manage `selectedAreaId: WritableSignal<string | null>` and `selectedPlanId: WritableSignal<string | null>`
    - Provide `selectArea(id)` (resets selectedPlanId) and `selectPlan(id)` methods
    - Expose computed `selectedArea` and `selectedPlan` signals
    - _Requirements: 3.3, 5.3, 3.5_

  - [x] 2.3 Write unit tests for PanelStateService and SelectionStateService
    - Test toggle, expand, collapse state transitions
    - Test that selectArea resets selectedPlanId
    - Test computed signals derive correctly
    - _Requirements: 2.2, 2.8, 3.3, 5.3_

- [x] 3. Shared components and pipes
  - [x] 3.1 Implement `SkeletonComponent`
    - Create `Web/src/app/components/skeleton/skeleton.component.ts` (standalone, OnPush)
    - Accept inputs: `lines` (default 3), `height` (default '1rem')
    - Render animated placeholder rectangles with shimmer gradient (surface → surface-elevated)
    - Animation cycle: 1.5s repeating left-to-right
    - Respect `prefers-reduced-motion`: static background, no animation
    - _Requirements: 9.1, 9.2, 9.4_

  - [x] 3.2 Implement `EmptyStateComponent`
    - Create `Web/src/app/components/empty-state/empty-state.component.ts` (standalone, OnPush)
    - Accept inputs: `icon` ('map' | 'route' | 'pointer'), `heading` (required), `description` (required)
    - Render inline SVG illustration (64x64px, text-muted color), heading, and description
    - Center vertically/horizontally with lg spacing padding
    - Add `role="status"` and `aria-label` matching heading
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.7_

  - [x] 3.3 Implement `RelativeTimePipe` and `AreaCalculationService`
    - Create `Web/src/app/pipes/relative-time.pipe.ts` — transforms ISO date to relative time (within 30 days) or DD.MM.YYYY (older)
    - Create `Web/src/app/services/area-calculation.service.ts` — calculates polygon area in hectares from GeoJSON coordinates using spherical excess formula, rounded to 2 decimal places
    - _Requirements: 3.2_

  - [x] 3.4 Write property tests for RelativeTimePipe and AreaCalculationService (Properties 2, 3)
    - **Property 2: Polygon area calculation produces correct hectares** — Validates: Requirements 3.2
    - **Property 3: Relative time formatting follows locale rules** — Validates: Requirements 3.2
    - Create `Web/src/app/pipes/relative-time.pipe.spec.ts` and `Web/src/app/services/area-calculation.service.spec.ts` using fast-check

- [x] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Toast notification system
  - [x] 5.1 Implement `ToastService`
    - Create `Web/src/app/services/toast.service.ts`
    - Signal-based queue: `visibleToasts: Signal<ToastNotification[]>`, `pendingQueue: Signal<ToastNotification[]>`
    - `show(type, message)` method: adds toast, manages max 3 visible / max 10 pending FIFO
    - `dismiss(id)` method: removes toast, promotes next from queue
    - Auto-dismiss: 5s timeout for success/info types via setTimeout, no auto-dismiss for error
    - _Requirements: 8.4, 8.7, 8.8_

  - [x] 5.2 Implement `ToastContainerComponent`
    - Create `Web/src/app/components/toast-container/toast-container.component.ts` (standalone, OnPush)
    - Position: fixed bottom-right, stacked vertically with 8px gap
    - Render each visible toast with type-specific styling (left-border color, icon)
    - Slide-in animation (200ms) and fade-out (150ms)
    - Close button (X icon) on each toast
    - ARIA: role="status"/aria-live="polite" for success/info, role="alert"/aria-live="assertive" for error
    - Truncate messages > 150 chars with title attribute for full text
    - _Requirements: 8.1, 8.2, 8.3, 8.5, 8.6, 8.9_

  - [x] 5.3 Write property tests for ToastService (Properties 9, 10)
    - **Property 9: Toast type determines auto-dismiss behavior and ARIA attributes** — Validates: Requirements 8.4, 8.6
    - **Property 10: Toast queue maintains max 3 visible invariant** — Validates: Requirements 8.7
    - Create `Web/src/app/services/toast.service.spec.ts` using fast-check

- [x] 6. Side panel layout and area list
  - [x] 6.1 Implement `SidePanelComponent`
    - Create `Web/src/app/components/side-panel/side-panel.component.ts` (standalone, OnPush)
    - Read PanelStateService for expanded/collapsed state
    - Render toggle button with aria-label="Toggle side panel" and aria-expanded attribute
    - Expanded: 360px width; collapsed: 48px rail with icons only
    - Collapsible sections (aria-expanded on headers) for Area_List, Flight_Plan_Form, Flight_Plan_List
    - Mobile (<768px): full-width overlay with backdrop (0.5 opacity), close button, slide animation
    - Tablet (768-1023px): overlay triggered by FAB (48x48px, positioned 16px top/left)
    - CSS transition 300ms for expand/collapse; map resizes to fill remaining width
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 10.1, 10.2_

  - [x] 6.2 Implement `AreaListComponent`
    - Create `Web/src/app/components/area-list/area-list.component.ts` (standalone, OnPush)
    - Fetch areas from AreasApiService.listAreas() on init, sort descending by createdAt
    - Display: sequential number, relative time (RelativeTimePipe), area in hectares (AreaCalculationService)
    - Selection: highlight selected item, emit area ID to SelectionStateService
    - Loading state: 3 skeleton items; Error state: inline message + retry button; Empty state: EmptyStateComponent
    - Keyboard navigation: arrow keys cycle through items (wrapping), Enter/Space selects
    - ARIA: role="listbox" on container, role="option" on items, aria-selected on active, aria-activedescendant
    - Virtual scrolling for > 50 items
    - Focus indicator: 2px solid outline with :focus-visible
    - _Requirements: 3.1, 3.2, 3.3, 3.6, 3.7, 3.8, 3.9, 3.10, 11.3_

  - [x] 6.3 Write property test for keyboard list navigation (Property 5)
    - **Property 5: Keyboard list navigation cycles correctly** — Validates: Requirements 3.9, 5.8
    - Create `Web/src/app/directives/list-keyboard-nav.directive.spec.ts` using fast-check

  - [x] 6.4 Write unit tests for SidePanelComponent and AreaListComponent
    - Test expanded/collapsed rendering, toggle behavior, ARIA attributes
    - Test area list loading/loaded/error/empty states
    - Test keyboard navigation and selection
    - _Requirements: 2.1–2.8, 3.1–3.10_

- [x] 7. Flight plan form
  - [x] 7.1 Implement `FlightPlanFormComponent`
    - Create `Web/src/app/components/flight-plan-form/flight-plan-form.component.ts` (standalone, OnPush)
    - Reactive form with dynamic controls switching between Grid and Poi modes (segmented control)
    - Grid mode fields: altitudeM, sensorWidthMm, focalLengthMm, imageWidthPx, imageHeightPx, frontOverlapPercent, sideOverlapPercent, headingDegrees (optional)
    - Poi mode fields: centerLatitude, centerLongitude, radiusM, altitudeM, gimbalPitchDegrees, photoCount (optional), overlapPercent (optional), cameraHorizontalFovDegrees (optional), structureHeightM (optional)
    - Range validation on blur + valueChanges, inline error messages below invalid fields
    - Tooltip icons next to field labels (hover/focus explanatory text)
    - Disabled state when no area selected (visual + non-interactive)
    - Submit: validate → scroll/focus first invalid field OR call FlightPlansApiService.calculate()
    - Submitting state: disabled button with spinner + "Obliczam..."
    - Success: toast "Plan lotu wygenerowany", prepend plan to Flight_Plan_List
    - Error: toast with API error message
    - Required fields marked with asterisk, labels linked with for/id, aria-describedby for tooltips/errors
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10, 4.11, 4.12_

  - [x] 7.2 Write unit tests for FlightPlanFormComponent
    - Test mode switching (Grid/Poi) and field visibility
    - Test validation on blur and inline error display
    - Test disabled state when no area selected
    - Test form submission flow (success and error paths)
    - _Requirements: 4.1–4.12_

- [x] 8. Flight plan list and export dialog
  - [x] 8.1 Implement `FlightPlanListComponent`
    - Create `Web/src/app/components/flight-plan-list/flight-plan-list.component.ts` (standalone, OnPush)
    - Fetch plans from FlightPlansApiService.list({ areaId }) when selectedAreaId changes
    - Display: mode icon+label, relative time, total distance (m), flight time (formatFlightTime), photo count
    - Selection: highlight, trigger FlightPathVisualizationService, show "Eksportuj" button
    - Loading: 2 skeleton items; Error: inline message + retry; Empty: EmptyStateComponent
    - Keyboard navigation: arrow keys wrap, Enter/Space selects
    - ARIA: role="listbox", role="option", aria-selected, aria-activedescendant
    - Re-fetch on area change (discard current, show loading)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9_

  - [x] 8.2 Implement `ExportDialogComponent`
    - Create `Web/src/app/components/export-dialog/export-dialog.component.ts` (standalone, OnPush)
    - Modal overlay with backdrop; radio group (LitchiCsv, Kml, DjiWpml), default LitchiCsv
    - "Pobierz" button triggers FlightPlansApiService.exportMissionFile()
    - Loading: spinner + "Pobieranie...", button disabled
    - Success (HTTP 200): trigger file download from Blob + Content-Disposition filename, close dialog
    - Error (404/422/500/network): inline error below format options, re-enable button
    - Timeout (30s): abort request, inline error
    - Close: X button, backdrop click, Escape key
    - Focus trap: Tab/Shift+Tab cycle within dialog
    - ARIA: role="dialog", aria-modal="true", aria-labelledby
    - Return focus to trigger element on close
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 7.10, 7.11, 7.12_

  - [x] 8.3 Write property test for focus trap (Property 8)
    - **Property 8: Focus trap cycles within modal boundaries** — Validates: Requirements 7.10, 11.5
    - Create `Web/src/app/directives/focus-trap.directive.spec.ts` using fast-check

  - [x] 8.4 Write unit tests for FlightPlanListComponent and ExportDialogComponent
    - Test plan list loading/loaded/error/empty states
    - Test export dialog open/close, format selection, download flow, error handling
    - Test focus trap and keyboard dismiss (Escape)
    - _Requirements: 5.1–5.9, 7.1–7.12_

- [x] 9. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Flight path visualization
  - [x] 10.1 Implement `FlightPathVisualizationService`
    - Create `Web/src/app/services/flight-path-visualization.service.ts`
    - Create dedicated VectorSource + VectorLayer (separate from polygon drawing layer)
    - `renderFlightPath(waypoints: WaypointDto[])`: transform coords EPSG:4326 → map projection, create numbered point features (12px circle, primary color, 1-based label) and LineString feature (2px stroke, primary at 80% opacity)
    - `clearFlightPath()`: remove all features from flight path source
    - `fitToFlightPath()`: animate map view to fit extent with 15% padding within 500ms
    - React to SelectionStateService.selectedPlan changes via effect: render/clear/fit
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8_

  - [x] 10.2 Write property test for flight path visualization (Property 7)
    - **Property 7: Flight path visualization preserves waypoint count, order, and labels** — Validates: Requirements 6.2, 6.3
    - Create `Web/src/app/services/flight-path-visualization.service.spec.ts` using fast-check

- [x] 11. Integration and wiring
  - [x] 11.1 Update `AppComponent` to integrate SidePanelComponent, ToastContainerComponent, and layout
    - Modify `Web/src/app/app.component.ts` to import SidePanelComponent and ToastContainerComponent
    - Update template: `<app-side-panel />`, `<app-map>` with panel-expanded class binding, `<app-toast-container />`
    - Update `Web/src/app/app.component.scss` with flex layout (side panel + map fill remaining width)
    - _Requirements: 2.1, 2.3_

  - [x] 11.2 Wire MapComponent to fit selected area and register flight path layer
    - Modify `Web/src/app/components/map/map.component.ts` to:
      - Inject SelectionStateService, react to selectedArea changes → animate map to fit polygon bounds (10% padding, 500ms)
      - Inject FlightPathVisualizationService, add its VectorLayer to the map
    - Ensure map remains interactive (pan, zoom, draw) regardless of panel state
    - _Requirements: 3.4, 6.6, 6.8, 10.4_

  - [x] 11.3 Implement responsive layout styles and accessibility polish
    - Add global responsive styles for breakpoints (tablet overlay, mobile full-screen, swipe-to-close)
    - Ensure all interactive elements have :focus-visible indicators (2px solid, primary color, 2px offset)
    - Ensure tab order follows visual layout: panel toggle → area list → form → plan list → map
    - Set aria-hidden on main content when modal is open
    - Add aria-live="polite" regions for dynamic list updates
    - _Requirements: 10.1, 10.2, 10.3, 10.5, 10.7, 11.1, 11.2, 11.4, 11.6, 11.7, 11.8_

  - [x] 11.4 Write integration tests for full panel–map–toast interaction flow
    - Test area selection → map fit, plan selection → flight path render, export → file download
    - Test responsive layout at different viewport widths
    - Test toast notifications appear on form success/error
    - _Requirements: 2.3, 3.4, 5.3, 6.6, 8.8_

- [x] 12. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties (11 properties from the design)
- Unit tests validate specific examples and edge cases
- All components use standalone, OnPush change detection, and Angular signals (zoneless)
- The API layer (AreasApiService, FlightPlansApiService) is already auto-generated and available
- fast-check is already in devDependencies; Karma/Jasmine is the test runner

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3", "2.1", "2.2", "3.1", "3.2"] },
    { "id": 2, "tasks": ["2.3", "3.3"] },
    { "id": 3, "tasks": ["3.4", "5.1"] },
    { "id": 4, "tasks": ["5.2", "5.3"] },
    { "id": 5, "tasks": ["6.1", "6.2"] },
    { "id": 6, "tasks": ["6.3", "6.4", "7.1"] },
    { "id": 7, "tasks": ["7.2", "8.1"] },
    { "id": 8, "tasks": ["8.2", "8.3", "8.4"] },
    { "id": 9, "tasks": ["10.1"] },
    { "id": 10, "tasks": ["10.2", "11.1"] },
    { "id": 11, "tasks": ["11.2", "11.3"] },
    { "id": 12, "tasks": ["11.4"] }
  ]
}
```
