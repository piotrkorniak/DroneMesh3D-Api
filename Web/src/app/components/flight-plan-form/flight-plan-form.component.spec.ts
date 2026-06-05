import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FlightPlanFormComponent } from './flight-plan-form.component';
import { SelectionStateService } from '../../services/selection-state.service';
import { FlightPlansApiService } from '../../api/services/flight-plans.service';
import { ToastService } from '../../services/toast.service';
import { of, throwError } from 'rxjs';
import { FlightPlanResponse } from '../../api/models/flight-plan-response';
import { FlightStatisticsDto } from '../../api/models/flight-statistics-dto';

describe('FlightPlanFormComponent', () => {
  let component: FlightPlanFormComponent;
  let fixture: ComponentFixture<FlightPlanFormComponent>;
  let selectionState: SelectionStateService;
  let flightPlansApi: jasmine.SpyObj<FlightPlansApiService>;
  let toastService: jasmine.SpyObj<ToastService>;

  const mockPlanResponse: FlightPlanResponse = {
    id: 'plan-1',
    areaId: 'area-1',
    mode: 'Grid',
    waypoints: [],
    statistics: { totalDistanceM: 1200, flightTimeSeconds: 300, photoCount: 50 } as unknown as FlightStatisticsDto,
    createdAt: new Date().toISOString(),
  };

  beforeEach(async () => {
    const flightPlansApiSpy = jasmine.createSpyObj('FlightPlansApiService', ['calculate']);
    const toastServiceSpy = jasmine.createSpyObj('ToastService', ['show']);

    await TestBed.configureTestingModule({
      imports: [FlightPlanFormComponent],
      providers: [
        SelectionStateService,
        { provide: FlightPlansApiService, useValue: flightPlansApiSpy },
        { provide: ToastService, useValue: toastServiceSpy },
      ],
    }).compileComponents();

    selectionState = TestBed.inject(SelectionStateService);
    flightPlansApi = TestBed.inject(FlightPlansApiService) as jasmine.SpyObj<FlightPlansApiService>;
    toastService = TestBed.inject(ToastService) as jasmine.SpyObj<ToastService>;

    fixture = TestBed.createComponent(FlightPlanFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // --- Test 1: Default mode is Grid with 8 fields visible ---
  it('should default to Grid mode with 8 fields visible', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    expect(component.mode()).toBe('Grid');
    const fields = fixture.nativeElement.querySelectorAll('.fpf__field');
    expect(fields.length).toBe(8);
  });

  // --- Test 2: Switching to Poi mode shows 9 fields ---
  it('should show 9 fields when switching to Poi mode', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    component.setMode('Poi');
    fixture.detectChanges();

    expect(component.mode()).toBe('Poi');
    const fields = fixture.nativeElement.querySelectorAll('.fpf__field');
    expect(fields.length).toBe(9);
  });

  // --- Test 3: Form is disabled (has fpf--disabled class) when no area is selected ---
  it('should have fpf--disabled class when no area is selected', () => {
    selectionState.selectedAreaId.set(null);
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('.fpf');
    expect(form.classList.contains('fpf--disabled')).toBeTrue();
  });

  it('should NOT have fpf--disabled class when an area is selected', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('.fpf');
    expect(form.classList.contains('fpf--disabled')).toBeFalse();
  });

  // --- Test 4: Range validation shows error on blur for out-of-range values ---
  it('should show range validation error on blur for out-of-range values', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    // Set altitudeM to out-of-range value (range is 20-400)
    const altitudeControl = component.gridForm.get('altitudeM')!;
    altitudeControl.setValue(500);
    component.markFieldTouched('altitudeM');
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('#fpf-error-altitudeM');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('20');
    expect(errorEl.textContent).toContain('400');
  });

  // --- Test 5: Required field shows "Pole wymagane" error when empty and touched ---
  it('should show "Pole wymagane" error when required field is empty and touched', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    // Clear a required field
    const altitudeControl = component.gridForm.get('altitudeM')!;
    altitudeControl.setValue(null);
    component.markFieldTouched('altitudeM');
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('#fpf-error-altitudeM');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent.trim()).toBe('Pole wymagane');
  });

  // --- Test 6: Tooltip text is visible on hover/focus (check for tooltip element presence) ---
  it('should have tooltip elements present for each field', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    const tooltipTexts = fixture.nativeElement.querySelectorAll('.fpf__tooltip-text');
    expect(tooltipTexts.length).toBe(8); // 8 fields in Grid mode

    // Verify tooltip has role="tooltip"
    const firstTooltip = tooltipTexts[0];
    expect(firstTooltip.getAttribute('role')).toBe('tooltip');
    expect(firstTooltip.textContent.trim().length).toBeGreaterThan(0);
  });

  // --- Test 7: Submit with invalid form does not call API ---
  it('should NOT call FlightPlansApiService.calculate when form is invalid', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    // Make a required field invalid
    component.gridForm.get('altitudeM')!.setValue(null);
    fixture.detectChanges();

    component.onSubmit();
    fixture.detectChanges();

    expect(flightPlansApi.calculate).not.toHaveBeenCalled();
  });

  // --- Test 8: Submit with valid form calls FlightPlansApiService.calculate with correct request ---
  it('should call FlightPlansApiService.calculate with correct request when form is valid', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    flightPlansApi.calculate.and.returnValue(of(mockPlanResponse));

    component.onSubmit();
    fixture.detectChanges();

    expect(flightPlansApi.calculate).toHaveBeenCalledTimes(1);
    const request = flightPlansApi.calculate.calls.mostRecent().args[0];
    expect(request.areaId).toBe('area-1');
    expect(request.mode).toBe('Grid');
    expect(request.grid).toBeTruthy();
    expect(request.grid!.altitudeM).toBe(100);
    expect(request.grid!.camera.sensorWidthMm).toBe(13.2);
    expect(request.grid!.camera.focalLengthMm).toBe(8.8);
    expect(request.grid!.camera.imageWidthPx).toBe(4000);
    expect(request.grid!.camera.imageHeightPx).toBe(3000);
    expect(request.grid!.frontOverlapPercent).toBe(70);
    expect(request.grid!.sideOverlapPercent).toBe(65);
    expect(request.grid!.headingDegrees).toBeNull();
    expect(request.poi).toBeNull();
  });

  // --- Test 9: During submission, button shows "Obliczam..." and is disabled ---
  it('should show "Obliczam..." and disable submit button during submission', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    // Use a subject-like observable that doesn't complete immediately
    flightPlansApi.calculate.and.returnValue(of(mockPlanResponse));

    // Set submitting manually to test UI state
    component['submitting'].set(true);
    fixture.detectChanges();

    const submitBtn = fixture.nativeElement.querySelector('.fpf__submit') as HTMLButtonElement;
    expect(submitBtn.disabled).toBeTrue();
    expect(submitBtn.textContent).toContain('Obliczam...');
  });

  // --- Test 10: On success, toast "Plan lotu wygenerowany" is shown ---
  it('should show success toast "Plan lotu wygenerowany" on successful submission', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    flightPlansApi.calculate.and.returnValue(of(mockPlanResponse));

    component.onSubmit();
    fixture.detectChanges();

    expect(toastService.show).toHaveBeenCalledWith('success', 'Plan lotu wygenerowany');
  });

  // --- Test 11: On error, error toast with API message is shown ---
  it('should show error toast with API message on failed submission', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    const errorMessage = 'Nieprawidłowe parametry lotu';
    flightPlansApi.calculate.and.returnValue(
      throwError(() => ({ error: { message: errorMessage } }))
    );

    component.onSubmit();
    fixture.detectChanges();

    expect(toastService.show).toHaveBeenCalledWith('error', errorMessage);
  });

  // --- Test 12: On success, new plan is prepended to SelectionStateService.plans ---
  it('should prepend new plan to SelectionStateService.plans on success', () => {
    selectionState.selectedAreaId.set('area-1');
    selectionState.plans.set([]);
    fixture.detectChanges();

    flightPlansApi.calculate.and.returnValue(of(mockPlanResponse));

    component.onSubmit();
    fixture.detectChanges();

    expect(selectionState.plans().length).toBe(1);
    expect(selectionState.plans()[0]).toEqual(mockPlanResponse);
  });

  it('should prepend new plan to existing plans', () => {
    selectionState.selectedAreaId.set('area-1');
    const existingPlan: FlightPlanResponse = { ...mockPlanResponse, id: 'plan-old' };
    selectionState.plans.set([existingPlan]);
    fixture.detectChanges();

    flightPlansApi.calculate.and.returnValue(of(mockPlanResponse));

    component.onSubmit();
    fixture.detectChanges();

    expect(selectionState.plans().length).toBe(2);
    expect(selectionState.plans()[0].id).toBe('plan-1');
    expect(selectionState.plans()[1].id).toBe('plan-old');
  });

  // --- Test 13: Required fields have asterisk visible ---
  it('should show asterisk for required fields', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    const requiredIndicators = fixture.nativeElement.querySelectorAll('.fpf__required');
    // In Grid mode: 7 required fields (altitudeM, sensorWidthMm, focalLengthMm, imageWidthPx, imageHeightPx, frontOverlapPercent, sideOverlapPercent)
    // headingDegrees is optional
    expect(requiredIndicators.length).toBe(7);

    const firstAsterisk = requiredIndicators[0];
    expect(firstAsterisk.textContent.trim()).toBe('*');
    expect(firstAsterisk.getAttribute('aria-hidden')).toBe('true');
  });

  // --- Additional edge case tests ---

  it('should submit Poi form with correct request shape', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    component.setMode('Poi');
    fixture.detectChanges();

    // Fill required POI fields
    component.poiForm.get('centerLatitude')!.setValue(52.23);
    component.poiForm.get('centerLongitude')!.setValue(21.01);
    fixture.detectChanges();

    flightPlansApi.calculate.and.returnValue(of(mockPlanResponse));

    component.onSubmit();
    fixture.detectChanges();

    expect(flightPlansApi.calculate).toHaveBeenCalledTimes(1);
    const request = flightPlansApi.calculate.calls.mostRecent().args[0];
    expect(request.mode).toBe('Poi');
    expect(request.poi).toBeTruthy();
    expect(request.poi!.centerLatitude).toBe(52.23);
    expect(request.poi!.centerLongitude).toBe(21.01);
    expect(request.poi!.radiusM).toBe(50);
    expect(request.poi!.altitudeM).toBe(80);
    expect(request.poi!.gimbalPitchDegrees).toBe(-45);
    expect(request.grid).toBeNull();
  });

  it('should reset submitting state on error', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    flightPlansApi.calculate.and.returnValue(
      throwError(() => ({ error: { message: 'Error' } }))
    );

    component.onSubmit();
    fixture.detectChanges();

    expect(component.submitting()).toBeFalse();
  });

  it('should reset touched fields when switching modes', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    // Touch a field in Grid mode
    component.gridForm.get('altitudeM')!.setValue(null);
    component.markFieldTouched('altitudeM');
    fixture.detectChanges();

    // Switch to Poi mode
    component.setMode('Poi');
    fixture.detectChanges();

    // The touched fields should be reset
    const errorEl = fixture.nativeElement.querySelector('.fpf__error');
    expect(errorEl).toBeFalsy();
  });

  it('should not submit when no area is selected', () => {
    selectionState.selectedAreaId.set(null);
    fixture.detectChanges();

    component.onSubmit();
    expect(flightPlansApi.calculate).not.toHaveBeenCalled();
  });

  it('should not submit when already submitting', () => {
    selectionState.selectedAreaId.set('area-1');
    fixture.detectChanges();

    component['submitting'].set(true);
    component.onSubmit();
    expect(flightPlansApi.calculate).not.toHaveBeenCalled();
  });
});
