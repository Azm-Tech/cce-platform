import { Pipe, PipeTransform } from '@angular/core';
import { KNOWN_ROLE_OPTIONS } from './identity.types';

@Pipe({ name: 'roleLabel', standalone: true, pure: true })
export class RoleLabelPipe implements PipeTransform {
  private static readonly map = new Map(
    KNOWN_ROLE_OPTIONS.map(o => [o.value, o.labelKey]),
  );

  transform(value: string): string {
    return RoleLabelPipe.map.get(value) ?? value;
  }
}
