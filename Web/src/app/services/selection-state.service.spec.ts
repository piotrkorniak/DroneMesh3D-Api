import { TestBed } from '@angular/core/testing';
import { SelectionStateService } from './selection-state.service';
import { AreaResponse } from '../api/models/area-response';
import { FlightPlanResponse } from '../api/models/flight-plan-response';

describe('SelectionStateService', () => {
  let service: SelectionStateService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SelectionStateService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('default values', () => {
    it('should have selectedAreaId default to null', () => {
      expect(service.selectedAreaId()).toBeNull();
    });

    it('should have selectedPlanId default to null', () => {
      expect(service.selectedPlanId()).toBeNull();
    });

    it('should have selectedArea computed as null', () => {
      expect(service.selectedArea()).toBeNull();
    });

    it('should have selectedPlan computed as null', () => {
      expect(service.selectedPlan()).toBeNull();
    });
  });

  describe('selectArea', () => {
    it('should set selectedAreaId', () => {
      service.selectArea('area-1');
      expect(service.selectedAreaId()).toBe('area-1');
    });

    it('should reset selectedPlanId to null when selecting an area', () => {
      service.selectPlan('plan-1');
      expect(service.selectedPlanId()).toBe('plan-1');

      service.selectArea('area-2');
      expect(service.selectedPlanId()).toBeNull();
    });

    it('should allow setting selectedAreaId to null', () => {
      service.selectArea('area-1');
      service.selectArea(null);
      expect(service.selectedAreaId()).toBeNull();
    });

    it('should reset selectedPlanId even when selecting null area', () => {
      service.selectPlan('plan-1');
      service.selectArea(null);
      expect(service.selectedPlanId()).toBeNull();
    });

    it('should clear previously selected plan when selecting a new area', () => {
      service.selectArea('area-1');
      service.selectPlan('plan-1');
      expect(service.selectedPlanId()).toBe('plan-1');

      service.selectArea('area-2');
      expect(service.selectedPlanId()).toBeNull();
      expect(service.selectedAreaId()).toBe('area-2');
    });
  });

  describe('selectPlan', () => {
    it('should set selectedPlanId', () => {
      service.selectPlan('plan-1');
      expect(service.selectedPlanId()).toBe('plan-1');
    });

    it('should allow setting selectedPlanId to null', () => {
      service.selectPlan('plan-1');
      service.selectPlan(null);
      expect(service.selectedPlanId()).toBeNull();
    });

    it('should not affect selectedAreaId', () => {
      service.selectArea('area-1');
      service.selectPlan('plan-1');
      expect(service.selectedAreaId()).toBe('area-1');
    });
  });

  describe('selectedArea computed signal', () => {
    const mockAreas: AreaResponse[] = [
      { id: 'area-1', createdAt: '2024-01-01T00:00:00Z', geometry: { type: 'Polygon', coordinates: [[[0, 0], [1, 0], [1, 1], [0, 0]]] } },
      { id: 'area-2', createdAt: '2024-01-02T00:00:00Z', geometry: { type: 'Polygon', coordinates: [[[2, 2], [3, 2], [3, 3], [2, 2]]] } },
    ];

    it('should return the correct AreaResponse when areas cache is populated and area is selected', () => {
      service.areas.set(mockAreas);
      service.selectArea('area-1');
      expect(service.selectedArea()).toEqual(mockAreas[0]);
    });

    it('should return the second area when its id is selected', () => {
      service.areas.set(mockAreas);
      service.selectArea('area-2');
      expect(service.selectedArea()).toEqual(mockAreas[1]);
    });

    it('should return null when no area is selected', () => {
      service.areas.set(mockAreas);
      expect(service.selectedArea()).toBeNull();
    });

    it('should return null when selected area id does not match any cached area', () => {
      service.areas.set(mockAreas);
      service.selectArea('non-existent');
      expect(service.selectedArea()).toBeNull();
    });

    it('should return null when areas cache is empty', () => {
      service.selectArea('area-1');
      expect(service.selectedArea()).toBeNull();
    });

    it('should update when areas cache changes', () => {
      service.selectArea('area-1');
      expect(service.selectedArea()).toBeNull();

      service.areas.set(mockAreas);
      expect(service.selectedArea()).toEqual(mockAreas[0]);
    });
  });

  describe('selectedPlan computed signal', () => {
    const mockPlans: FlightPlanResponse[] = [
      { id: 'plan-1', areaId: 'area-1', mode: 'Grid', waypoints: [], statistics: { totalDistanceM: 100, estimatedTimeS: 60, photoCount: 10 } as unknown as FlightPlanResponse['statistics'], createdAt: '2024-01-01T12:00:00Z' },
      { id: 'plan-2', areaId: 'area-1', mode: 'Poi', waypoints: [], statistics: { totalDistanceM: 200, estimatedTimeS: 120, photoCount: 20 } as unknown as FlightPlanResponse['statistics'], createdAt: '2024-01-02T12:00:00Z' },
    ];

    it('should return the correct FlightPlanResponse when plans cache is populated and plan is selected', () => {
      service.plans.set(mockPlans);
      service.selectPlan('plan-1');
      expect(service.selectedPlan()).toEqual(mockPlans[0]);
    });

    it('should return the second plan when its id is selected', () => {
      service.plans.set(mockPlans);
      service.selectPlan('plan-2');
      expect(service.selectedPlan()).toEqual(mockPlans[1]);
    });

    it('should return null when no plan is selected', () => {
      service.plans.set(mockPlans);
      expect(service.selectedPlan()).toBeNull();
    });

    it('should return null when selected plan id does not match any cached plan', () => {
      service.plans.set(mockPlans);
      service.selectPlan('non-existent');
      expect(service.selectedPlan()).toBeNull();
    });

    it('should return null when plans cache is empty', () => {
      service.selectPlan('plan-1');
      expect(service.selectedPlan()).toBeNull();
    });

    it('should update when plans cache changes', () => {
      service.selectPlan('plan-1');
      expect(service.selectedPlan()).toBeNull();

      service.plans.set(mockPlans);
      expect(service.selectedPlan()).toEqual(mockPlans[0]);
    });
  });
});
