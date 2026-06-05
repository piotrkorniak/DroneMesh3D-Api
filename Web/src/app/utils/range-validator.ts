import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Angular validator factory that creates a ValidatorFn for numeric min/max range validation.
 * Returns null (valid) when min <= value <= max.
 * Returns a validation error object when value is out of range.
 *
 * @param min - Minimum allowed value (inclusive)
 * @param max - Maximum allowed value (inclusive)
 * @returns ValidatorFn that validates numeric range
 */
export function rangeValidator(min: number, max: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;

    if (value === null || value === undefined || value === '') {
      return null; // Don't validate empty values (use Validators.required for that)
    }

    const numValue = Number(value);

    if (isNaN(numValue)) {
      return { range: { min, max, actual: value, message: `Wartość musi być liczbą z zakresu ${min}–${max}` } };
    }

    if (numValue < min || numValue > max) {
      return { range: { min, max, actual: numValue, message: `Wartość musi być w zakresie ${min}–${max}` } };
    }

    return null;
  };
}
