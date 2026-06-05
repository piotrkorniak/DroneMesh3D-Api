import { TestBed } from '@angular/core/testing';
import { PanelStateService, SidePanelSection } from './panel-state.service';

describe('PanelStateService', () => {
  let service: PanelStateService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PanelStateService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('isExpanded', () => {
    it('should default to true', () => {
      expect(service.isExpanded()).toBe(true);
    });

    it('should collapse when collapse() is called', () => {
      service.collapse();
      expect(service.isExpanded()).toBe(false);
    });

    it('should expand when expand() is called', () => {
      service.collapse();
      service.expand();
      expect(service.isExpanded()).toBe(true);
    });

    it('should toggle from expanded to collapsed', () => {
      service.toggle();
      expect(service.isExpanded()).toBe(false);
    });

    it('should toggle from collapsed to expanded', () => {
      service.collapse();
      service.toggle();
      expect(service.isExpanded()).toBe(true);
    });
  });

  describe('section collapsed states', () => {
    const sections: SidePanelSection[] = ['Area_List', 'Flight_Plan_Form', 'Flight_Plan_List'];

    sections.forEach((section) => {
      describe(section, () => {
        it('should default to not collapsed', () => {
          expect(service.sectionCollapsed[section]()).toBe(false);
          expect(service.isSectionCollapsed(section)).toBe(false);
        });

        it('should collapse when collapseSection() is called', () => {
          service.collapseSection(section);
          expect(service.sectionCollapsed[section]()).toBe(true);
          expect(service.isSectionCollapsed(section)).toBe(true);
        });

        it('should expand when expandSection() is called', () => {
          service.collapseSection(section);
          service.expandSection(section);
          expect(service.sectionCollapsed[section]()).toBe(false);
        });

        it('should toggle section state', () => {
          service.toggleSection(section);
          expect(service.sectionCollapsed[section]()).toBe(true);
          service.toggleSection(section);
          expect(service.sectionCollapsed[section]()).toBe(false);
        });
      });
    });

    it('should track sections independently', () => {
      service.collapseSection('Area_List');
      expect(service.isSectionCollapsed('Area_List')).toBe(true);
      expect(service.isSectionCollapsed('Flight_Plan_Form')).toBe(false);
      expect(service.isSectionCollapsed('Flight_Plan_List')).toBe(false);
    });
  });
});
