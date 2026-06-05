import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import Map from 'ol/Map';
import View from 'ol/View';
import TileLayer from 'ol/layer/Tile';
import VectorLayer from 'ol/layer/Vector';
import VectorSource from 'ol/source/Vector';
import OSM from 'ol/source/OSM';
import Draw from 'ol/interaction/Draw';
import Modify from 'ol/interaction/Modify';
import { fromLonLat, toLonLat } from 'ol/proj';
import { boundingExtent } from 'ol/extent';
import Feature from 'ol/Feature';
import Polygon from 'ol/geom/Polygon';
import Style from 'ol/style/Style';
import Stroke from 'ol/style/Stroke';
import Fill from 'ol/style/Fill';
import { finalize } from 'rxjs';

import { PolygonValidatorService } from '../../services/polygon-validator.service';
import { AreaService } from '../../services/area.service';
import { SelectionStateService } from '../../services/selection-state.service';
import { FlightPathVisualizationService } from '../../services/flight-path-visualization.service';
import { CreateAreaRequest } from '../../api/models/create-area-request';
import { ValidationResult } from '../../models/validation';
import { MapToolbarComponent } from '../map-toolbar/map-toolbar.component';
import { MapSearchComponent, LocationSelectedEvent } from '../map-search/map-search.component';

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrl: './map.component.scss',
  standalone: true,
  imports: [MapToolbarComponent, MapSearchComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapComponent implements OnInit, OnDestroy {
  private readonly polygonValidator = inject(PolygonValidatorService);
  private readonly areaService = inject(AreaService);
  private readonly selectionState = inject(SelectionStateService);
  private readonly flightPathViz = inject(FlightPathVisualizationService);

  private map!: Map;
  readonly vectorSource = new VectorSource();
  readonly vectorLayer = new VectorLayer({ source: this.vectorSource });
  private drawInteraction: Draw | null = null;
  private modifyInteraction: Modify | null = null;

  // Validation visual feedback styles
  private readonly validStyle = new Style({
    stroke: new Stroke({ color: 'rgba(33, 150, 243, 1)', width: 3 }),
    fill: new Fill({ color: 'rgba(33, 150, 243, 0.15)' }),
  });

  private readonly invalidStyle = new Style({
    stroke: new Stroke({ color: 'rgba(244, 67, 54, 1)', width: 3 }),
    fill: new Fill({ color: 'rgba(244, 67, 54, 0.15)' }),
  });

  // Signals for reactive state
  readonly validationResult = signal<ValidationResult | null>(null);
  readonly isSubmitting = signal(false);
  readonly submissionError = signal<string | null>(null);
  readonly hasPolygon = signal(false);
  readonly isDrawing = signal(false);

  // Computed signals
  readonly isValid = computed(() => this.validationResult()?.isValid ?? false);
  readonly validationErrors = computed(
    () => this.validationResult()?.errors.map(e => e.message) ?? []
  );

  // Reactive style update based on validation result
  private readonly validationStyleEffect = effect(() => {
    const result = this.validationResult();
    if (result === null) {
      this.vectorLayer.setStyle(this.validStyle);
    } else if (result.isValid) {
      this.vectorLayer.setStyle(this.validStyle);
    } else {
      this.vectorLayer.setStyle(this.invalidStyle);
    }
  });

  // React to selectedArea changes: draw polygon on map and animate to fit bounds
  private readonly selectedAreaEffect = effect(() => {
    const area = this.selectionState.selectedArea();

    // Clear any existing polygon from the vector source
    this.vectorSource.clear();
    this.hasPolygon.set(false);
    this.validationResult.set(null);
    this.removeModifyInteraction();

    if (!area || !area.geometry || !area.geometry.coordinates) return;

    const coordinates = area.geometry.coordinates[0]; // outer ring
    if (!coordinates || coordinates.length === 0) return;

    // Transform coordinates from EPSG:4326 to map projection (EPSG:3857)
    const projectedCoords = coordinates.map(coord => fromLonLat(coord as [number, number]));

    // Create and add the polygon feature to the map
    const polygon = new Polygon([projectedCoords]);
    const feature = new Feature(polygon);
    this.vectorSource.addFeature(feature);
    this.hasPolygon.set(true);
    this.vectorLayer.setStyle(this.validStyle);

    // Animate map view to fit the extent with 10% padding, 500ms duration
    const extent = boundingExtent(projectedCoords);
    const view = this.map?.getView();
    if (view) {
      const size = this.map.getSize();
      const paddingValue = size ? Math.min(size[0], size[1]) * 0.1 : 50;
      view.fit(extent, {
        padding: [paddingValue, paddingValue, paddingValue, paddingValue],
        duration: 500,
      });
    }
  });

  ngOnInit(): void {
    this.initializeMap();
  }

  ngOnDestroy(): void {
    this.map?.setTarget(undefined);
  }

  private initializeMap(): void {
    this.map = new Map({
      target: 'map-container',
      layers: [
        new TileLayer({
          source: new OSM(),
        }),
        this.vectorLayer,
        this.flightPathViz.flightPathLayer,
      ],
      view: new View({
        projection: 'EPSG:3857',
        center: fromLonLat([20.0, 52.0]), // Center on Poland
        zoom: 7,
      }),
    });

    // Provide the map view reference to FlightPathVisualizationService
    this.flightPathViz.setMapView(this.map.getView());
  }

  startDrawing(): void {
    // Clear any existing features and interactions
    this.vectorSource.clear();
    this.removeDrawInteraction();

    // Create a new Draw interaction with type 'Polygon'
    this.drawInteraction = new Draw({
      source: this.vectorSource,
      type: 'Polygon',
    });

    // Listen for drawend event
    this.drawInteraction.on('drawend', event => {
      const geometry = event.feature.getGeometry();
      if (!geometry) return;

      // Extract coordinates from the drawn polygon (in EPSG:3857)
      const coords3857 = (geometry as import('ol/geom/Polygon').default).getCoordinates()[0];

      // Transform from EPSG:3857 to EPSG:4326 (WGS 84)
      const coords4326 = coords3857.map(coord => toLonLat(coord));

      // Run validation
      const result = this.polygonValidator.validate(coords4326);
      this.validationResult.set(result);
      this.hasPolygon.set(true);
      this.isDrawing.set(false);

      // Remove the draw interaction after drawing is complete
      this.removeDrawInteraction();

      // Add Modify interaction to allow vertex dragging
      this.addModifyInteraction();
    });

    this.map.addInteraction(this.drawInteraction);
    this.isDrawing.set(true);
  }

  clearPolygon(): void {
    this.vectorSource.clear();
    this.removeDrawInteraction();
    this.removeModifyInteraction();
    this.hasPolygon.set(false);
    this.validationResult.set(null);
    this.isDrawing.set(false);
    this.submissionError.set(null);
  }

  private removeDrawInteraction(): void {
    if (this.drawInteraction) {
      this.map.removeInteraction(this.drawInteraction);
      this.drawInteraction = null;
    }
  }

  private addModifyInteraction(): void {
    this.removeModifyInteraction();

    this.modifyInteraction = new Modify({
      source: this.vectorSource,
    });

    this.modifyInteraction.on('modifyend', event => {
      const features = event.features.getArray();
      if (features.length === 0) return;

      const geometry = features[0].getGeometry();
      if (!geometry || !(geometry instanceof Polygon)) return;

      // Extract updated coordinates (in EPSG:3857)
      const coords3857 = geometry.getCoordinates()[0];

      // Transform from EPSG:3857 to EPSG:4326 (WGS 84)
      const coords4326 = coords3857.map(coord => toLonLat(coord));

      // Re-validate the modified polygon
      const result = this.polygonValidator.validate(coords4326);
      this.validationResult.set(result);
    });

    this.map.addInteraction(this.modifyInteraction);
  }

  private removeModifyInteraction(): void {
    if (this.modifyInteraction) {
      this.map.removeInteraction(this.modifyInteraction);
      this.modifyInteraction = null;
    }
  }

  submitArea(): void {
    // Get the current polygon feature from vectorSource
    const features = this.vectorSource.getFeatures();
    if (features.length === 0) return;

    const feature = features[0];
    const geometry = feature.getGeometry();
    if (!geometry || !(geometry instanceof Polygon)) return;

    // Extract the outer ring coordinates (in EPSG:3857)
    const coords3857 = geometry.getCoordinates()[0];

    // Transform each coordinate from EPSG:3857 to EPSG:4326 (WGS 84)
    const coords4326 = coords3857.map(coord => toLonLat(coord));

    // Construct a CreateAreaRequest with GeoJSON Polygon format
    const request: CreateAreaRequest = {
      type: 'Polygon',
      coordinates: [coords4326],
    };

    // Set submitting state
    this.isSubmitting.set(true);
    this.submissionError.set(null);

    // Call AreaService and manage signal state
    this.areaService
      .createArea(request)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: response => {
          // Prepend the new area to the list so it appears immediately
          this.selectionState.areas.update(areas => [response, ...areas]);
          // Select the new area (highlights it on the list and zooms map)
          this.selectionState.selectArea(response.id);
          // Clear the drawn polygon (it will be re-rendered by selectedAreaEffect)
          this.vectorSource.clear();
          this.removeModifyInteraction();
          this.hasPolygon.set(false);
          this.validationResult.set(null);
        },
        error: err => {
          const message =
            err?.error?.message ?? err?.message ?? 'An unexpected error occurred.';
          this.submissionError.set(message);
        },
      });
  }

  getMap(): Map {
    return this.map;
  }

  flyToLocation(event: LocationSelectedEvent): void {
    const view = this.map?.getView();
    if (!view) return;

    view.animate({
      center: fromLonLat([event.lon, event.lat]),
      zoom: 16,
      duration: 500,
    });
  }
}
