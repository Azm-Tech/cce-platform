import { HttpErrorResponse } from '@angular/common/http';
import { toFieldErrors, toFeatureError } from './error-formatter';

function err(status: number, body: unknown = null): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: body });
}

describe('toFieldErrors', () => {
  it('returns empty for non-400', () => {
    expect(toFieldErrors(err(500))).toEqual({});
  });

  it('returns empty when body has no errors', () => {
    expect(toFieldErrors(err(400, {}))).toEqual({});
  });

  it('lowercases first letter of field names', () => {
    const body = { errors: { 'Name': ['required'], 'Address.Line1': ['too long'] } };
    expect(toFieldErrors(err(400, body))).toEqual({
      name: ['required'],
      'address.Line1': ['too long'],
    });
  });
});

describe('toFeatureError', () => {
  it('maps 0 → network', () => {
    expect(toFeatureError(err(0))).toEqual({ kind: 'network' });
  });

  it('maps 400 → validation with fieldErrors', () => {
    const body = { errors: { Name: ['required'] } };
    const result = toFeatureError(err(400, body));
    expect(result.kind).toBe('validation');
    expect((result as { fieldErrors: Record<string, string[]> }).fieldErrors).toEqual({ name: ['required'] });
  });

  it('maps 404 → not-found', () => {
    expect(toFeatureError(err(404))).toEqual({ kind: 'not-found' });
  });

  it('maps 403 → forbidden', () => {
    expect(toFeatureError(err(403))).toEqual({ kind: 'forbidden' });
  });

  it('maps 409 with /duplicate type → duplicate', () => {
    expect(toFeatureError(err(409, { type: 'https://x/problems/duplicate' }))).toEqual({ kind: 'duplicate' });
  });

  it('maps 409 default → concurrency', () => {
    expect(toFeatureError(err(409))).toEqual({ kind: 'concurrency' });
    expect(toFeatureError(err(409, { type: 'https://x/problems/concurrency' }))).toEqual({ kind: 'concurrency' });
  });

  it('maps 5xx → server', () => {
    expect(toFeatureError(err(500))).toEqual({ kind: 'server' });
    expect(toFeatureError(err(503))).toEqual({ kind: 'server' });
  });

  it('maps unknown → unknown with status', () => {
    expect(toFeatureError(err(418))).toEqual({ kind: 'unknown', status: 418 });
  });
});
