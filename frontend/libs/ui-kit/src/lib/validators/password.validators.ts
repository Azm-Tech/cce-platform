import { ValidatorFn, Validators } from '@angular/forms';

export const PASSWORD_MIN_LENGTH = 12;
export const PASSWORD_MAX_LENGTH = 20;
export const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/;

/**
 * Standard CCE password validators — shared across registration, admin user
 * creation, and any future password input. Does NOT include Validators.required
 * so callers can decide if the field is optional.
 */
export const PASSWORD_STRENGTH_VALIDATORS: ValidatorFn[] = [
  Validators.minLength(PASSWORD_MIN_LENGTH),
  Validators.maxLength(PASSWORD_MAX_LENGTH),
  Validators.pattern(PASSWORD_PATTERN),
];
