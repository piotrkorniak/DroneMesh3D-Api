import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { SelectionStateService } from '../../services/selection-state.service';
import { FlightPlansApiService } from '../../api/services/flight-plans.service';
import { ToastService } from '../../services/toast.service';
import { rangeValidator } from '../../utils/range-validator';
import { FlightMode } from '../../api/models/flight-mode';
import { CalculateFlightPathRequest } from '../../api/models/calculate-flight-path-request';

/** Field metadata for labels, tooltips, and validation ranges. */
interface FieldMeta {
  id: string;
  label: string;
  tooltip: string;
  min: number;
  max: number;
  required: boolean;
  defaultValue: number | null;
}

const GRID_FIELDS: FieldMeta[] = [
  { id: 'altitudeM', label: 'Wysokość (m)', tooltip: 'Wysokość lotu drona nad terenem w metrach', min: 20, max: 400, required: true, defaultValue: 100 },
  { id: 'sensorWidthMm', label: 'Szerokość sensora (mm)', tooltip: 'Fizyczna szerokość sensora kamery w milimetrach', min: 1, max: 100, required: true, defaultValue: 13.2 },
  { id: 'focalLengthMm', label: 'Ogniskowa (mm)', tooltip: 'Ogniskowa obiektywu kamery w milimetrach', min: 1, max: 500, required: true, defaultValue: 8.8 },
  { id: 'imageWidthPx', label: 'Szerokość zdjęcia (px)', tooltip: 'Rozdzielczość pozioma zdjęcia w pikselach', min: 1, max: 20000, required: true, defaultValue: 4000 },
  { id: 'imageHeightPx', label: 'Wysokość zdjęcia (px)', tooltip: 'Rozdzielczość pionowa zdjęcia w pikselach', min: 1, max: 20000, required: true, defaultValue: 3000 },
  { id: 'frontOverlapPercent', label: 'Nakładka frontalna (%)', tooltip: 'Procentowe pokrycie kolejnych zdjęć w kierunku lotu', min: 20, max: 95, required: true, defaultValue: 70 },
  { id: 'sideOverlapPercent', label: 'Nakładka boczna (%)', tooltip: 'Procentowe pokrycie między sąsiednimi pasami lotu', min: 20, max: 95, required: true, defaultValue: 65 },
  { id: 'headingDegrees', label: 'Kierunek (°)', tooltip: 'Kierunek przelotu w stopniach (0–359). Opcjonalny.', min: 0, max: 359, required: false, defaultValue: null },
];

const POI_FIELDS: FieldMeta[] = [
  { id: 'centerLatitude', label: 'Szerokość geogr. środka', tooltip: 'Szerokość geograficzna punktu centralnego (–90 do 90)', min: -90, max: 90, required: true, defaultValue: null },
  { id: 'centerLongitude', label: 'Długość geogr. środka', tooltip: 'Długość geograficzna punktu centralnego (–180 do 180)', min: -180, max: 180, required: true, defaultValue: null },
  { id: 'radiusM', label: 'Promień (m)', tooltip: 'Promień okręgu lotu wokół punktu w metrach', min: 5, max: 500, required: true, defaultValue: 50 },
  { id: 'altitudeM', label: 'Wysokość (m)', tooltip: 'Wysokość lotu drona w metrach', min: 20, max: 400, required: true, defaultValue: 80 },
  { id: 'gimbalPitchDegrees', label: 'Pochylenie gimbala (°)', tooltip: 'Kąt pochylenia kamery (–90 do 0, gdzie –90 = prosto w dół)', min: -90, max: 0, required: true, defaultValue: -45 },
  { id: 'photoCount', label: 'Liczba zdjęć', tooltip: 'Liczba zdjęć do wykonania na okręgu. Opcjonalny.', min: 1, max: 1000, required: false, defaultValue: null },
  { id: 'overlapPercent', label: 'Nakładka (%)', tooltip: 'Procentowe pokrycie zdjęć. Opcjonalny.', min: 20, max: 95, required: false, defaultValue: null },
  { id: 'cameraHorizontalFovDegrees', label: 'FOV kamery (°)', tooltip: 'Poziome pole widzenia kamery w stopniach. Opcjonalny.', min: 1, max: 180, required: false, defaultValue: null },
  { id: 'structureHeightM', label: 'Wysokość struktury (m)', tooltip: 'Wysokość fotografowanej struktury w metrach. Opcjonalny.', min: 1, max: 1000, required: false, defaultValue: null },
];

@Component({
  selector: 'app-flight-plan-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './flight-plan-form.component.html',
  styleUrl: './flight-plan-form.component.scss',
})
export class FlightPlanFormComponent {
  private readonly selectionState = inject(SelectionStateService);
  private readonly flightPlansApi = inject(FlightPlansApiService);
  private readonly toastService = inject(ToastService);

  /** Current flight mode */
  readonly mode = signal<FlightMode>('Grid');

  /** Whether a calculation request is in progress */
  readonly submitting = signal(false);

  /** Whether an area is selected (form enabled state) */
  readonly hasSelectedArea = computed(() => this.selectionState.selectedAreaId() !== null);

  /** Fields metadata for the current mode */
  readonly activeFields = computed(() => this.mode() === 'Grid' ? GRID_FIELDS : POI_FIELDS);

  /** Reactive form for Grid mode */
  readonly gridForm = this.buildGridForm();

  /** Reactive form for Poi mode */
  readonly poiForm = this.buildPoiForm();

  /** Currently active form group (based on mode) */
  readonly activeForm = computed(() => this.mode() === 'Grid' ? this.gridForm : this.poiForm);

  /** Track which fields have been touched (for showing errors on blur) */
  private readonly touchedFields = signal<Set<string>>(new Set());

  setMode(newMode: FlightMode): void {
    this.mode.set(newMode);
    this.touchedFields.set(new Set());
  }

  markFieldTouched(fieldId: string): void {
    this.touchedFields.update(set => {
      const newSet = new Set(set);
      newSet.add(fieldId);
      return newSet;
    });
    // Also mark the form control as touched for Angular's validation display
    const control = this.activeForm().get(fieldId);
    control?.markAsTouched();
    control?.updateValueAndValidity();
  }

  isFieldInvalid(fieldId: string): boolean {
    const control = this.activeForm().get(fieldId);
    if (!control) return false;
    return control.invalid && (control.touched || this.touchedFields().has(fieldId));
  }

  getFieldError(fieldId: string): string {
    const control = this.activeForm().get(fieldId);
    if (!control || !control.errors) return '';

    if (control.errors['required']) {
      return 'Pole wymagane';
    }
    if (control.errors['range']) {
      return control.errors['range'].message;
    }
    return 'Nieprawidłowa wartość';
  }

  getAriaDescribedBy(fieldId: string): string {
    const parts: string[] = [`fpf-tooltip-${fieldId}`];
    if (this.isFieldInvalid(fieldId)) {
      parts.push(`fpf-error-${fieldId}`);
    }
    return parts.join(' ');
  }

  onSubmit(): void {
    if (!this.hasSelectedArea() || this.submitting()) return;

    const form = this.activeForm();
    form.markAllAsTouched();
    // Mark all fields touched for our custom tracking
    const allFieldIds = this.activeFields().map(f => f.id);
    this.touchedFields.set(new Set(allFieldIds));

    if (form.invalid) {
      this.scrollToFirstInvalidField();
      return;
    }

    this.submitCalculation();
  }

  private scrollToFirstInvalidField(): void {
    const form = this.activeForm();
    const fields = this.activeFields();

    for (const field of fields) {
      const control = form.get(field.id);
      if (control?.invalid) {
        const element = document.getElementById('fpf-' + field.id);
        if (element) {
          element.scrollIntoView({ behavior: 'smooth', block: 'center' });
          element.focus();
        }
        break;
      }
    }
  }

  private submitCalculation(): void {
    this.submitting.set(true);

    const areaId = this.selectionState.selectedAreaId()!;
    const currentMode = this.mode();
    const request = this.buildRequest(areaId, currentMode);

    this.flightPlansApi.calculate(request).subscribe({
      next: (plan) => {
        this.submitting.set(false);
        this.toastService.show('success', 'Plan lotu wygenerowany');
        // Prepend the new plan to the SelectionStateService plans list
        this.selectionState.plans.update(plans => [plan, ...plans]);
      },
      error: (err) => {
        this.submitting.set(false);
        const message = err?.error?.message || err?.message || 'Wystąpił błąd podczas generowania planu lotu';
        this.toastService.show('error', message);
      },
    });
  }

  private buildRequest(areaId: string, currentMode: FlightMode): CalculateFlightPathRequest {
    if (currentMode === 'Grid') {
      const v = this.gridForm.value;
      return {
        areaId,
        mode: 'Grid',
        grid: {
          altitudeM: Number(v.altitudeM),
          camera: {
            sensorWidthMm: Number(v.sensorWidthMm),
            focalLengthMm: Number(v.focalLengthMm),
            imageWidthPx: Number(v.imageWidthPx),
            imageHeightPx: Number(v.imageHeightPx),
          },
          frontOverlapPercent: Number(v.frontOverlapPercent),
          sideOverlapPercent: Number(v.sideOverlapPercent),
          headingDegrees: v.headingDegrees != null && v.headingDegrees !== '' ? Number(v.headingDegrees) : null,
        },
        poi: null,
      };
    } else {
      const v = this.poiForm.value;
      return {
        areaId,
        mode: 'Poi',
        grid: null,
        poi: {
          centerLatitude: Number(v.centerLatitude),
          centerLongitude: Number(v.centerLongitude),
          radiusM: Number(v.radiusM),
          altitudeM: Number(v.altitudeM),
          gimbalPitchDegrees: Number(v.gimbalPitchDegrees),
          photoCount: v.photoCount != null && v.photoCount !== '' ? Number(v.photoCount) : null,
          overlapPercent: v.overlapPercent != null && v.overlapPercent !== '' ? Number(v.overlapPercent) : null,
          cameraHorizontalFovDegrees: v.cameraHorizontalFovDegrees != null && v.cameraHorizontalFovDegrees !== '' ? Number(v.cameraHorizontalFovDegrees) : null,
          structureHeightM: v.structureHeightM != null && v.structureHeightM !== '' ? Number(v.structureHeightM) : null,
        },
      };
    }
  }

  private buildGridForm(): FormGroup {
    return new FormGroup({
      altitudeM: new FormControl(100, [Validators.required, rangeValidator(20, 400)]),
      sensorWidthMm: new FormControl(13.2, [Validators.required, rangeValidator(1, 100)]),
      focalLengthMm: new FormControl(8.8, [Validators.required, rangeValidator(1, 500)]),
      imageWidthPx: new FormControl(4000, [Validators.required, rangeValidator(1, 20000)]),
      imageHeightPx: new FormControl(3000, [Validators.required, rangeValidator(1, 20000)]),
      frontOverlapPercent: new FormControl(70, [Validators.required, rangeValidator(20, 95)]),
      sideOverlapPercent: new FormControl(65, [Validators.required, rangeValidator(20, 95)]),
      headingDegrees: new FormControl(null, [rangeValidator(0, 359)]),
    });
  }

  private buildPoiForm(): FormGroup {
    return new FormGroup({
      centerLatitude: new FormControl(null, [Validators.required, rangeValidator(-90, 90)]),
      centerLongitude: new FormControl(null, [Validators.required, rangeValidator(-180, 180)]),
      radiusM: new FormControl(50, [Validators.required, rangeValidator(5, 500)]),
      altitudeM: new FormControl(80, [Validators.required, rangeValidator(20, 400)]),
      gimbalPitchDegrees: new FormControl(-45, [Validators.required, rangeValidator(-90, 0)]),
      photoCount: new FormControl(null, [rangeValidator(1, 1000)]),
      overlapPercent: new FormControl(null, [rangeValidator(20, 95)]),
      cameraHorizontalFovDegrees: new FormControl(null, [rangeValidator(1, 180)]),
      structureHeightM: new FormControl(null, [rangeValidator(1, 1000)]),
    });
  }
}
