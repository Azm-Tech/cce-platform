import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { AboutContent, PoliciesContent, PublicPage } from './page.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class PagesApiService {
  private readonly http = inject(HttpClient);

  async getBySlug(slug: string): Promise<Result<PublicPage>> {
    try {
      const res = await firstValueFrom(
        this.http.get<{ data: PublicPage }>(`/api/pages/${encodeURIComponent(slug)}`),
      );
      return { ok: true, value: res.data };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  async getAbout(): Promise<Result<AboutContent>> {
    try {
      const res = await firstValueFrom(
        this.http.get<{
          data?: {
            description?: { ar?: string; en?: string };
            howToUseVideoUrl?: string | null;
            glossary?: Array<{
              term?: { ar?: string; en?: string };
              definition?: { ar?: string; en?: string };
            }>;
            knowledgePartners?: Array<{
              name?: { ar?: string; en?: string };
              description?: { ar?: string; en?: string };
              logoUrl?: string | null;
              websiteUrl?: string | null;
            }>;
          };
        }>('/api/about'),
      );
      const d = res.data ?? {};
      const value: AboutContent = {
        descriptionAr: d.description?.ar ?? '',
        descriptionEn: d.description?.en ?? '',
        howToUseVideoUrl: d.howToUseVideoUrl ?? null,
        glossaryTerms: (d.glossary ?? []).map((g, i) => ({
          id: String(i),
          termAr: g.term?.ar ?? '',
          termEn: g.term?.en ?? '',
          definitionAr: g.definition?.ar ?? '',
          definitionEn: g.definition?.en ?? '',
        })),
        knowledgePartners: (d.knowledgePartners ?? []).map((p, i) => ({
          id: String(i),
          nameAr: p.name?.ar ?? '',
          nameEn: p.name?.en ?? '',
          logoUrl: p.logoUrl ?? null,
          websiteUrl: p.websiteUrl ?? null,
          descriptionAr: p.description?.ar ?? null,
          descriptionEn: p.description?.en ?? null,
        })),
      };
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  async getPolicies(): Promise<Result<PoliciesContent>> {
    try {
      const res = await firstValueFrom(
        this.http.get<{ data: { sections: Array<{ type: number; title: { ar: string; en: string }; content: { ar: string; en: string } }> } }>('/api/policies'),
      );
      const value: PoliciesContent = {
        sections: (res.data?.sections ?? []).map((s, i) => ({
          id: String(i),
          type: s.type,
          titleAr: s.title?.ar ?? '',
          titleEn: s.title?.en ?? '',
          contentAr: s.content?.ar ?? '',
          contentEn: s.content?.en ?? '',
          orderIndex: i,
        })),
      };
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
