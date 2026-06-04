// models/validation.ts
export interface ValidationResult {
  isValid: boolean;
  errors: ValidationError[];
}

export interface ValidationError {
  rule: ValidationRule;
  message: string;
}

export enum ValidationRule {
  MinVertices = 'MIN_VERTICES',
  Closure = 'CLOSURE',
  SelfIntersection = 'SELF_INTERSECTION',
  AreaTooLarge = 'AREA_TOO_LARGE',
  AreaTooSmall = 'AREA_TOO_SMALL',
}
