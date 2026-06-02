import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { HomepageSection, HomepageSectionType, HomepageSettings } from './home.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class HomeApiService {
  private readonly http = inject(HttpClient);

  async listSections(): Promise<Result<HomepageSection[]>> {
    try {
      const res = await firstValueFrom(
        this.http.get<HomepageSection[] | { data?: HomepageSection[] }>('/api/homepage-sections'),
      );
      const value = Array.isArray(res) ? res : (res.data ?? []);
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  async getSettings(): Promise<Result<HomepageSettings>> {
    try {
      const res = await firstValueFrom(
        this.http.get<{
          data?: {
            videoUrl?: string | null;
            objective?: { ar?: string | null; en?: string | null };
            objectiveAr?: string | null;
            objectiveEn?: string | null;
            cceConceptsAr?: string | null;
            cceConceptsEn?: string | null;
            participatingCountries?: Array<{
              id: string;
              nameAr?: string;
              nameEn?: string;
              flagUrl?: string | null;
            }>;
            sections?: Array<{
              id: string;
              sectionType: HomepageSectionType;
              orderIndex: number;
              contentAr?: string;
              contentEn?: string;
              isActive?: boolean;
            }>;
          };
        }>('/api/homepage'),
      );
      const d = res.data ?? {};
      const value: HomepageSettings = {
        videoUrl: d.videoUrl ?? null,
        objectiveAr: d.objective?.ar ?? d.objectiveAr ?? null,
        objectiveEn: d.objective?.en ?? d.objectiveEn ?? null,
        cceConceptsAr: d.cceConceptsAr ?? null,
        cceConceptsEn: d.cceConceptsEn ?? null,
        participatingCountries: (d.participatingCountries ?? []).map((c) => ({
          id: c.id,
          nameAr: c.nameAr ?? '',
          nameEn: c.nameEn ?? '',
          flagUrl: c.flagUrl ?? null,
        })),
        sections: (d.sections ?? []).map((s) => ({
          id: s.id,
          sectionType: s.sectionType,
          orderIndex: s.orderIndex,
          contentAr: s.contentAr ?? '',
          contentEn: s.contentEn ?? '',
          isActive: s.isActive ?? true,
        })),
      };
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
