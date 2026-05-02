import { TestBed } from '@angular/core/testing';
import { AssistantApiService } from './assistant-api.service';

describe('AssistantApiService (Phase 00 stub)', () => {
  it('is provided in root', () => {
    TestBed.configureTestingModule({});
    const sut = TestBed.inject(AssistantApiService);
    expect(sut).toBeTruthy();
  });

  it('query throws until Phase 01 wires it', () => {
    TestBed.configureTestingModule({});
    const sut = TestBed.inject(AssistantApiService);
    expect(() => sut.query({ messages: [], locale: 'en' }, new AbortController().signal)).toThrow(
      /Phase 01/,
    );
  });
});
