# Requirements Document

## Introduction

Kompletny, profesjonalny interfejs graficzny (GUI) dla aplikacji DroneMesh3D — frontendowej aplikacji Angular 21 przeznaczonej dla operatorów dronów. GUI obejmuje system designu (design tokens, typografia, paleta kolorów), panel boczny z listą obszarów i planów lotów, formularz generowania trasy lotu, wizualizację trasy na mapie OpenLayers, dialog eksportu plików misji, system notyfikacji (toast), stany ładowania (skeleton screens) oraz wsparcie dla urządzeń tabletowych i dostępności (WCAG). Estetyka: ciemny, profesjonalny motyw dopasowany do branży dronowej (inspiracja: DJI FlightHub, Pix4D).

## Glossary

- **Design_System**: Zbiór tokenów (kolory, typografia, spacing, cienie, zaokrąglenia) definiujących spójny wygląd całej aplikacji
- **Side_Panel**: Wysuwany/zwijany panel boczny wyświetlany obok mapy, zawierający listy obszarów, planów i formularze
- **Area_List**: Komponent wyświetlający listę zapisanych obszarów geograficznych z możliwością selekcji
- **Flight_Plan_Form**: Formularz służący do konfiguracji parametrów generowania trasy lotu (tryb Grid/POI, parametry kamery, nakładki)
- **Flight_Plan_List**: Komponent wyświetlający historię wygenerowanych planów lotów dla wybranego obszaru
- **Flight_Path_Visualization**: Warstwa wektorowa na mapie OpenLayers prezentująca waypointy i linie trasy lotu
- **Export_Dialog**: Modalny dialog umożliwiający wybór formatu eksportu i pobranie pliku misji
- **Toast_Notification**: Tymczasowy komunikat wyświetlany w rogu ekranu informujący o sukcesie, błędzie lub ostrzeżeniu
- **Skeleton_Screen**: Animowany placeholder imitujący kształt docelowej treści podczas ładowania danych
- **Empty_State**: Dedykowany widok wyświetlany gdy lista nie zawiera elementów, z komunikatem i ewentualnym CTA
- **Map_Component**: Istniejący komponent Angular z mapą OpenLayers (pełny viewport)
- **Design_Token**: Zmienna CSS (custom property) definiująca wartość koloru, rozmiaru lub typografii w Design_System
- **Breakpoint**: Punkt graniczny szerokości viewportu determinujący zmianę layoutu (tablet ≥ 768px, desktop ≥ 1024px)
- **ARIA**: Accessible Rich Internet Applications — atrybuty HTML zapewniające dostępność dla technologii asystujących
- **Waypoint_Marker**: Ikona/punkt na mapie reprezentujący pojedynczy waypoint trasy lotu
- **Flight_Path_Line**: Linia łącząca kolejne waypointy na mapie, wizualizująca trasę lotu

## Requirements

### Requirement 1: System designu i tokeny

**User Story:** Jako deweloper frontendowy, chcę mieć spójny system designu oparty na CSS custom properties, żeby wszystkie komponenty GUI korzystały z jednolitej palety kolorów, typografii i spacingu.

#### Acceptance Criteria

1. THE Design_System SHALL define CSS custom properties for colors including: primary, primary-hover, secondary, surface, surface-elevated, background, text-primary, text-secondary, text-muted, border, success, warning, error, and overlay values, each prefixed with `--ds-color-` (e.g., `--ds-color-primary`)
2. THE Design_System SHALL define CSS custom properties for spacing using a scale of 4px increments from 4px (xs) to 48px (3xl), prefixed with `--ds-spacing-` (e.g., `--ds-spacing-xs: 4px`, `--ds-spacing-sm: 8px`, up to `--ds-spacing-3xl: 48px`)
3. THE Design_System SHALL define CSS custom properties for typography including: font-family (sans-serif stack), font sizes at six scale steps (xs: 0.75rem, sm: 0.875rem, md: 1rem, lg: 1.125rem, xl: 1.25rem, 2xl: 1.5rem), font weights (regular: 400, medium: 500, semibold: 600, bold: 700), and line heights (tight: 1.25, normal: 1.5, relaxed: 1.75)
4. THE Design_System SHALL define CSS custom properties for border-radius (sm, md, lg, xl), box-shadow (sm, md, lg), and transition durations (fast: 150ms, normal: 250ms, slow: 400ms)
5. THE Design_System SHALL use a dark color theme with surface colors in the range of hsl(220, 15-25%, 8-18%) and accent colors providing sufficient contrast ratio of at least 4.5:1 against surface backgrounds
6. THE Design_System SHALL be implemented as a single SCSS file (`_tokens.scss`) that declares all custom properties on the `:root` selector and is importable by all component stylesheets

### Requirement 2: Layout aplikacji z panelem bocznym

**User Story:** Jako operator drona, chcę mieć panel boczny obok mapy, w którym widzę listę obszarów i planów lotów, żebym mógł szybko nawigować między nimi bez zakrywania mapy.

#### Acceptance Criteria

1. WHILE the viewport width is 768px or greater, THE Side_Panel SHALL be displayed to the left of the Map_Component occupying a fixed width of 360px
2. THE Side_Panel SHALL provide a toggle button that collapses the panel to a narrow rail (48px wide) showing only icons, and expands it back to full width
3. WHEN the Side_Panel is collapsed or expanded, THE Map_Component SHALL resize to fill the remaining viewport width using a CSS transition of 300ms duration
4. THE Side_Panel SHALL contain vertically stacked sections: Area_List at the top, Flight_Plan_Form in the middle, and Flight_Plan_List at the bottom, each collapsible independently
5. WHILE the viewport width is less than 768px, THE Side_Panel SHALL render as a full-width overlay sliding in from the left with a backdrop of 0.5 opacity
6. WHEN the user taps the backdrop or presses the close button while the Side_Panel is displayed as a mobile overlay, THE Side_Panel SHALL slide out to the left and the backdrop SHALL be removed
7. THE Side_Panel toggle button SHALL have an aria-label of "Toggle side panel" and aria-expanded attribute reflecting the current panel state
8. THE Side_Panel SHALL initialize in the expanded state (360px) on first load and maintain its collapsed/expanded state across user interactions within the same session using a signal-based state

### Requirement 3: Lista obszarów

**User Story:** Jako operator drona, chcę widzieć listę wszystkich zapisanych obszarów, żebym mógł wybrać obszar do generowania planu lotu.

#### Acceptance Criteria

1. WHEN the Side_Panel is opened, THE Area_List SHALL fetch and display all areas from the AreasApiService.listAreas() endpoint, ordered by creation date descending (newest first)
2. THE Area_List SHALL display each area as a list item showing: a sequential number (starting from 1), the creation date formatted as relative time using locale-appropriate labels (e.g., "2 hours ago", "3 days ago") for dates within the last 30 days and as an absolute date (DD.MM.YYYY) for older dates, and the area size in hectares rounded to 2 decimal places calculated from the polygon geometry
3. WHEN the user clicks on an area list item, THE Area_List SHALL mark the item as selected with a visual highlight (distinct background color using Design_Token), deselect any previously selected item, and emit the selected area ID
4. WHEN an area is selected, THE Map_Component SHALL animate the map view to fit the selected area polygon bounds with 10% padding, completing the animation within 500 milliseconds
5. WHEN an area is selected, THE Flight_Plan_List SHALL load and display flight plans associated with the selected area
6. IF the AreasApiService returns an error, THEN THE Area_List SHALL display an inline error message with a retry button, and WHEN the retry button is clicked, THE Area_List SHALL re-fetch areas from the AreasApiService.listAreas() endpoint
7. WHILE areas are loading, THE Area_List SHALL display three Skeleton_Screen placeholder items matching the dimensions of real area items
8. IF the AreasApiService returns an empty array, THEN THE Area_List SHALL display an Empty_State with a message "Brak zapisanych obszarów" and an icon indicating an empty map
9. THE Area_List SHALL support keyboard navigation with arrow keys between items and Enter/Space to select an item, with a visible focus indicator rendered as a 2px solid outline using Design_Token focus color
10. IF more than 50 areas are returned by the AreasApiService, THEN THE Area_List SHALL render items using virtual scrolling to maintain a maximum initial render of 50 visible items

### Requirement 4: Formularz generowania planu lotu

**User Story:** Jako operator drona, chcę wypełnić formularz z parametrami lotu (tryb, wysokość, kamera, nakładki), żeby wygenerować optymalną trasę dla wybranego obszaru.

#### Acceptance Criteria

1. IF no area is selected in the Area_List, THEN THE Flight_Plan_Form SHALL be visually disabled with all inputs and the submit button non-interactive
2. THE Flight_Plan_Form SHALL provide a mode selector (segmented control) allowing the user to choose between Grid and Poi flight modes, with Grid selected by default
3. WHEN Grid mode is selected, THE Flight_Plan_Form SHALL display fields: altitudeM (range 20-400, default 100), sensorWidthMm (range 1-100, default 13.2), focalLengthMm (range 1-500, default 8.8), imageWidthPx (range 1-20000, default 4000), imageHeightPx (range 1-20000, default 3000), frontOverlapPercent (range 20-95, default 70), sideOverlapPercent (range 20-95, default 65), headingDegrees (range 0-359, optional)
4. WHEN Poi mode is selected, THE Flight_Plan_Form SHALL display fields: centerLatitude (range -90 to 90), centerLongitude (range -180 to 180), radiusM (range 5-500, default 50), altitudeM (range 20-400, default 80), gimbalPitchDegrees (range -90 to 0, default -45), photoCount (range 1-1000, optional), overlapPercent (range 20-95, optional), cameraHorizontalFovDegrees (range 1-180, optional), structureHeightM (range 1-1000, optional)
5. THE Flight_Plan_Form SHALL validate each numeric field when the field loses focus (blur) and on each value change thereafter, showing a single inline error message below the invalid field indicating the accepted range
6. THE Flight_Plan_Form SHALL display a tooltip icon next to each field label, showing an explanatory text on hover or focus describing the parameter purpose
7. IF the user clicks the "Generuj trasę" submit button while any visible field contains an invalid or empty required value, THEN THE Flight_Plan_Form SHALL not send the request and SHALL scroll to and focus the first invalid field
8. WHEN the user clicks the "Generuj trasę" submit button and all visible fields are valid, THE Flight_Plan_Form SHALL send a CalculateFlightPathRequest to FlightPlansApiService.calculate() with the selected area ID and form values
9. WHILE the flight plan calculation is in progress, THE Flight_Plan_Form SHALL disable the submit button and display a spinner inside it with text "Obliczam..."
10. IF the calculation request returns an error, THEN THE Flight_Plan_Form SHALL display the error message from the API response body in a Toast_Notification of type error
11. WHEN the calculation succeeds, THE Flight_Plan_Form SHALL display a Toast_Notification of type success with message "Plan lotu wygenerowany" and prepend the new plan to the Flight_Plan_List
12. THE Flight_Plan_Form SHALL mark required fields with a visual indicator (asterisk next to the label) and associate each field with a label element using matching id and for attributes

### Requirement 5: Lista planów lotów

**User Story:** Jako operator drona, chcę widzieć historię wygenerowanych planów lotów dla wybranego obszaru, żebym mógł porównywać trasy i wybrać optymalną do eksportu.

#### Acceptance Criteria

1. WHEN an area is selected, THE Flight_Plan_List SHALL fetch flight plans from FlightPlansApiService.list({ areaId }) and display them as a list ordered by creation date descending (newest first)
2. THE Flight_Plan_List SHALL display each plan showing: flight mode (Grid/POI icon + label), creation date as relative time (e.g. "2 min temu", "3 godz. temu", "5 dni temu"), total distance in meters rounded to the nearest integer, estimated flight time formatted as minutes and seconds (e.g. "4 min 32 s"), and photo count as an integer
3. WHEN the user clicks on a flight plan list item, THE Flight_Plan_List SHALL mark it as selected with a visually distinct highlight style and trigger the Flight_Path_Visualization on the map using the selected plan's waypoints
4. WHEN a flight plan is selected, THE Flight_Plan_List SHALL display an "Eksportuj" button next to the selected item opening the Export_Dialog
5. WHILE flight plans are loading, THE Flight_Plan_List SHALL display two Skeleton_Screen placeholder items
6. IF no flight plans exist for the selected area, THEN THE Flight_Plan_List SHALL display an Empty_State with message "Brak planów lotu — wygeneruj trasę powyżej" and an arrow icon pointing toward the Flight_Plan_Form
7. IF the flight plans request fails, THEN THE Flight_Plan_List SHALL display an inline error message indicating the failure and a retry button that re-invokes FlightPlansApiService.list({ areaId }) when clicked
8. THE Flight_Plan_List SHALL support keyboard navigation with arrow keys to move focus between list items (wrapping from last to first and vice versa) and Enter/Space to select the focused item
9. WHEN a different area is selected while the Flight_Plan_List is already displaying plans, THE Flight_Plan_List SHALL discard the current list, show the loading state, and fetch flight plans for the newly selected area

### Requirement 6: Wizualizacja trasy lotu na mapie

**User Story:** Jako operator drona, chcę widzieć trasę lotu (waypointy i linie) na mapie, żebym mógł ocenić jakość wygenerowanego planu przed eksportem.

#### Acceptance Criteria

1. WHEN a flight plan is selected in the Flight_Plan_List, THE Flight_Path_Visualization SHALL render all waypoints from the plan as circular markers (12px diameter) on the Map_Component using the coordinates transformed from EPSG:4326 to the map projection
2. THE Flight_Path_Visualization SHALL render lines connecting consecutive waypoints in order, forming the flight path polyline
3. THE Waypoint_Marker SHALL display a numbered label (1-based sequential index) centered within the marker and use the Design_System --ds-color-primary value as fill color
4. THE Flight_Path_Line SHALL be rendered with a solid stroke of 2px width using the Design_System --ds-color-primary value at 80% opacity
5. WHEN a different flight plan is selected, THE Flight_Path_Visualization SHALL clear all features from the previous visualization and render the new flight path
6. WHEN the flight path visualization is active, THE Map_Component SHALL animate the map view to fit the flight path extent with 15% padding within 500ms
7. WHEN no flight plan is selected (deselection or area change), THE Flight_Path_Visualization SHALL remove all waypoint markers and flight path lines from the map
8. THE Flight_Path_Visualization SHALL use a dedicated VectorLayer (separate from the polygon drawing VectorLayer) to avoid visual conflicts with area polygons

### Requirement 7: Dialog eksportu pliku misji

**User Story:** Jako operator drona, chcę wybrać format eksportu (LitchiCsv, Kml, DjiWpml) i pobrać plik misji, żeby załadować trasę do mojego drona.

#### Acceptance Criteria

1. WHEN the user clicks "Eksportuj" on a selected flight plan, THE Export_Dialog SHALL open as a modal overlay with a semi-transparent backdrop
2. THE Export_Dialog SHALL display a radio group with three format options: "Litchi CSV" (value LitchiCsv), "KML" (value Kml), "DJI WPML" (value DjiWpml) with LitchiCsv pre-selected as default
3. THE Export_Dialog SHALL display a "Pobierz" (Download) button that triggers the export when clicked
4. WHEN the user clicks "Pobierz", THE Export_Dialog SHALL call FlightPlansApiService.exportMissionFile() with the selected plan ID and chosen format
5. WHILE the export request is in progress, THE Export_Dialog SHALL display a loading spinner inside the "Pobierz" button with text "Pobieranie..." and disable the button to prevent duplicate submissions
6. WHEN the export response is received with HTTP 200, THE Export_Dialog SHALL trigger a browser file download using the Blob response body and the filename extracted from the Content-Disposition response header, then close the dialog
7. IF the export request returns HTTP 404 or HTTP 422, THEN THE Export_Dialog SHALL display an inline error message below the format options indicating the reason for failure (resource not found or validation error respectively), keep the dialog open, and re-enable the "Pobierz" button so the user can retry
8. IF the export request fails due to a network error or an HTTP 500 response, THEN THE Export_Dialog SHALL display an inline error message below the format options indicating that the server is unreachable or an unexpected error occurred, keep the dialog open, and re-enable the "Pobierz" button so the user can retry
9. THE Export_Dialog SHALL provide a close button (X icon) in the top-right corner and close when the user clicks the backdrop or presses Escape
10. THE Export_Dialog SHALL trap keyboard focus within the dialog while open and return focus to the triggering element on close
11. THE Export_Dialog SHALL have role="dialog", aria-modal="true", and aria-labelledby pointing to the dialog title element
12. IF the export request does not receive a response within 30 seconds, THEN THE Export_Dialog SHALL abort the request, display an inline error message below the format options indicating a timeout, and re-enable the "Pobierz" button so the user can retry

### Requirement 8: System notyfikacji (Toast)

**User Story:** Jako operator drona, chcę otrzymywać krótkie komunikaty o sukcesie lub błędzie operacji, żebym wiedział co się dzieje bez przerywania mojej pracy.

#### Acceptance Criteria

1. THE Toast_Notification SHALL display messages in the bottom-right corner of the viewport, stacked vertically with 8px gap between multiple toasts
2. THE Toast_Notification SHALL support three types: success (green accent), error (red accent), and info (blue accent), each with a distinct left-border color and icon
3. WHEN a toast is created, THE Toast_Notification SHALL animate in from the right side using a slide-in transition lasting 200ms, and WHEN a toast is dismissed or auto-dismissed, THE Toast_Notification SHALL animate out using a fade-out transition lasting 150ms before being removed from the DOM
4. THE Toast_Notification SHALL automatically dismiss after 5 seconds for success and info types, and remain visible until manually dismissed for error types
5. THE Toast_Notification SHALL provide a close button (X icon) allowing the user to dismiss the notification before the auto-dismiss timeout
6. THE Toast_Notification SHALL have role="status" and aria-live="polite" for success and info types, and role="alert" and aria-live="assertive" for error types
7. THE Toast_Notification SHALL display a maximum of 3 toasts simultaneously; additional toasts SHALL be placed in a FIFO queue (maximum 10 pending items) and the next queued toast SHALL appear when a visible toast is dismissed
8. THE Toast_Notification SHALL be implemented as a global Angular service injectable into any component via a ToastService.show(type, message) method
9. THE Toast_Notification SHALL truncate message text that exceeds 150 characters by displaying the first 150 characters followed by an ellipsis ("…"), and SHALL display the full text in a title attribute tooltip on hover

### Requirement 9: Stany ładowania i skeleton screens

**User Story:** Jako operator drona, chcę widzieć profesjonalne animacje ładowania zamiast pustego ekranu, żeby aplikacja sprawiała wrażenie responsywnej nawet gdy dane są pobierane.

#### Acceptance Criteria

1. THE Skeleton_Screen SHALL render animated placeholder shapes (rectangles with border-radius matching the Design_System border-radius-sm token) mimicking the layout of the target content with a shimmer gradient animation moving left-to-right in a repeating cycle of 1.5 seconds
2. THE Skeleton_Screen SHALL use a linear gradient animation transitioning between the Design_System surface color and surface-elevated color to produce the shimmer effect
3. WHILE any API request triggered by the Side_Panel is in progress, THE corresponding section (Area_List, Flight_Plan_List, or Flight_Plan_Form) SHALL display Skeleton_Screen placeholders matching the expected content structure in dimensions and element count
4. WHILE the user has prefers-reduced-motion enabled, THE Skeleton_Screen SHALL display a static placeholder with the Design_System surface-elevated background color and no animation
5. WHEN data loading completes successfully, THE Skeleton_Screen SHALL be replaced by the actual content within 150ms using a fade transition, with placeholder elements matching the height and width of rendered content elements within 2px tolerance to prevent layout shift
6. WHILE a Flight_Plan_Form submit request is pending, THE Flight_Plan_Form SHALL display an overlay with 0.6 opacity background color (using the Design_System surface color) and pointer-events set to none, preserving full visibility of the form content beneath
7. IF an API request fails while a Skeleton_Screen is displayed, THEN THE corresponding section SHALL replace the Skeleton_Screen with the section-specific error state (inline error message with retry button)

### Requirement 10: Responsywność i wsparcie dla tabletów

**User Story:** Jako operator drona używający tabletu w terenie, chcę aby interfejs był w pełni użyteczny na ekranach 768-1024px, żebym mógł planować loty bez dostępu do komputera.

#### Acceptance Criteria

1. WHILE the viewport width is between 768px and 1023px, THE Side_Panel SHALL render as a collapsible overlay triggered by a floating action button (minimum 48x48px tap target) positioned 16px from the top and 16px from the left of the viewport
2. WHILE the viewport width is less than 768px, THE Side_Panel SHALL render as a full-screen panel with a close button (minimum 48x48px tap target positioned in the top-right corner), overlaying the map entirely
3. WHILE the Side_Panel width is below 360px, THE Flight_Plan_Form SHALL adapt its field layout to a single-column arrangement
4. THE Map_Component SHALL remain interactive (pan, zoom, draw) regardless of the Side_Panel state on all viewport widths from 320px to the maximum supported desktop width
5. WHERE the device supports touch input, THE application SHALL support touch interactions for map gestures (pinch-to-zoom, two-finger pan) and Side_Panel swipe-to-close triggered by a horizontal swipe of at least 150px or 50% of the panel width in the closing direction
6. THE Design_System SHALL define Breakpoint custom properties for tablet (768px) and desktop (1024px) used consistently across all components
7. WHEN the device orientation changes between portrait and landscape, THE application SHALL re-evaluate the active Breakpoint and adapt the layout within 300ms without losing the current Side_Panel open/closed state or scroll position

### Requirement 11: Dostępność klawiatury i ARIA

**User Story:** Jako operator drona z ograniczeniami ruchowymi, chcę nawigować po aplikacji za pomocą klawiatury, żeby korzystać z niej bez użycia myszy.

#### Acceptance Criteria

1. THE application SHALL provide visible focus indicators (2px solid outline using Design_System primary color with 2px offset) on all interactive elements only during keyboard navigation (using :focus-visible), suppressing focus outlines on mouse or pointer interactions
2. THE Side_Panel sections SHALL implement aria-expanded attributes on collapsible section headers indicating their open/closed state
3. THE Area_List and Flight_Plan_List SHALL implement role="listbox" with role="option" on each item, aria-selected on the active item, and aria-activedescendant on the container
4. THE Flight_Plan_Form SHALL associate all input fields with labels using matching id and htmlFor attributes, and use aria-describedby to link fields to their tooltip and error message elements
5. WHILE the Export_Dialog is open, THE Export_Dialog SHALL trap keyboard focus so that Tab and Shift+Tab cycle only through focusable elements within the dialog
6. WHILE a modal (Export_Dialog or mobile Side_Panel overlay) is open, THE application SHALL set aria-hidden="true" on the main content container and prevent background content from receiving scroll and click events
7. THE application SHALL maintain a logical tab order following the visual layout: Side_Panel toggle, Area_List items, Flight_Plan_Form fields, Flight_Plan_List items, then map controls; collapsed Side_Panel sections SHALL remove their child elements from the tab order until expanded
8. WHEN the Area_List or Flight_Plan_List content updates dynamically (items added, removed, or selection changed), THE application SHALL announce the change to assistive technologies using an aria-live="polite" region describing the updated state

### Requirement 12: Puste stany (Empty States)

**User Story:** Jako nowy użytkownik aplikacji, chcę widzieć przyjazne komunikaty gdy nie ma jeszcze żadnych danych, żebym wiedział jak zacząć korzystać z aplikacji.

#### Acceptance Criteria

1. WHEN the Area_List contains no items, THE Empty_State SHALL display an SVG illustration (stylized empty map icon), a heading "Brak obszarów", and a description "Narysuj polygon na mapie, aby utworzyć pierwszy obszar"
2. WHEN the Flight_Plan_List contains no items for the selected area, THE Empty_State SHALL display an SVG illustration (route icon), a heading "Brak planów lotu", and a description "Skonfiguruj parametry powyżej i wygeneruj trasę"
3. WHEN no area is selected, THE Flight_Plan_Form and Flight_Plan_List section SHALL display an Empty_State with an SVG illustration (selection pointer icon), a heading "Wybierz obszar", and a description "Wybierz obszar z listy, aby rozpocząć planowanie"
4. THE Empty_State illustrations SHALL use colors from the Design_System text-muted token and be sized at 64x64px
5. THE Empty_State SHALL be centered vertically and horizontally within its parent container with padding using the Design_System lg spacing token
6. WHEN the data condition that triggered an Empty_State is no longer true (items are added or an area is selected), THE Empty_State SHALL be immediately replaced by the corresponding content without requiring a page reload or manual user action
7. THE Empty_State container SHALL have role="status" and an aria-label attribute matching the displayed heading text so that assistive technologies announce the empty state to users
