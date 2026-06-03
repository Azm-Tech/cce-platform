import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  AboutSettings,
  ApiPolicySection,
  CreateEventBody,
  CreateHomepageSectionBody,
  CreateNewsBody,
  CreatePageBody,
  Event,
  GlossaryTerm,
  GlossaryTermBody,
  HomepageSection,
  HomepageSettings,
  KnowledgePartner,
  KnowledgePartnerBody,
  News,
  PagedResult,
  Page,
  PoliciesSettings,
  PolicySection,
  PolicySectionBody,
  RescheduleEventBody,
  ReorderHomepageSectionsBody,
  UpdateAboutSettingsBody,
  UpdateEventBody,
  UpdateHomepageSectionBody,
  UpdateHomepageSettingsBody,
  UpdateNewsBody,
  UpdatePageBody,
} from './publishing.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

/** Single API service handling News + Events + Pages + Homepage Sections. */
@Injectable({ providedIn: 'root' })
export class PublishingApiService {
  private readonly http = inject(HttpClient);

  // ---- News ----
  async listNews(opts: { page?: number; pageSize?: number; search?: string; isPublished?: boolean } = {}): Promise<Result<PagedResult<News>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.isPublished !== undefined) params = params.set('isPublished', String(opts.isPublished));
    return this.run(() => firstValueFrom(this.http.get<PagedResult<News>>('/api/admin/news', { params })));
  }
  async createNews(body: CreateNewsBody): Promise<Result<News>> {
    return this.run(() => firstValueFrom(this.http.post<News>('/api/admin/news', body)));
  }
  async updateNews(id: string, body: UpdateNewsBody): Promise<Result<News>> {
    return this.run(() => firstValueFrom(this.http.put<News>(`/api/admin/news/${id}`, body)));
  }
  async deleteNews(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/news/${id}`)));
  }
  async publishNews(id: string): Promise<Result<News>> {
    return this.run(() => firstValueFrom(this.http.post<News>(`/api/admin/news/${id}/publish`, {})));
  }

  // ---- Events ----
  async listEvents(opts: { page?: number; pageSize?: number; search?: string } = {}): Promise<Result<PagedResult<Event>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    return this.run(() => firstValueFrom(this.http.get<PagedResult<Event>>('/api/admin/events', { params })));
  }
  async createEvent(body: CreateEventBody): Promise<Result<Event>> {
    return this.run(() => firstValueFrom(this.http.post<Event>('/api/admin/events', body)));
  }
  async updateEvent(id: string, body: UpdateEventBody): Promise<Result<Event>> {
    return this.run(() => firstValueFrom(this.http.put<Event>(`/api/admin/events/${id}`, body)));
  }
  async rescheduleEvent(id: string, body: RescheduleEventBody): Promise<Result<Event>> {
    return this.run(() =>
      firstValueFrom(this.http.post<Event>(`/api/admin/events/${id}/reschedule`, body)),
    );
  }
  async deleteEvent(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/events/${id}`)));
  }

  // ---- Pages ----
  async listPages(opts: { page?: number; pageSize?: number; search?: string } = {}): Promise<Result<PagedResult<Page>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    return this.run(() => firstValueFrom(this.http.get<PagedResult<Page>>('/api/admin/pages', { params })));
  }
  async createPage(body: CreatePageBody): Promise<Result<Page>> {
    return this.run(() => firstValueFrom(this.http.post<Page>('/api/admin/pages', body)));
  }
  async updatePage(id: string, body: UpdatePageBody): Promise<Result<Page>> {
    return this.run(() => firstValueFrom(this.http.put<Page>(`/api/admin/pages/${id}`, body)));
  }
  async deletePage(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/pages/${id}`)));
  }

  // ---- Homepage Sections ----
  async listHomepageSections(): Promise<Result<HomepageSection[]>> {
    return this.run(() => firstValueFrom(this.http.get<HomepageSection[]>('/api/admin/homepage-sections')));
  }
  async createHomepageSection(body: CreateHomepageSectionBody): Promise<Result<HomepageSection>> {
    return this.run(() =>
      firstValueFrom(this.http.post<HomepageSection>('/api/admin/homepage-sections', body)),
    );
  }
  async updateHomepageSection(id: string, body: UpdateHomepageSectionBody): Promise<Result<HomepageSection>> {
    return this.run(() =>
      firstValueFrom(this.http.put<HomepageSection>(`/api/admin/homepage-sections/${id}`, body)),
    );
  }
  async deleteHomepageSection(id: string): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(this.http.delete<void>(`/api/admin/homepage-sections/${id}`)),
    );
  }
  async reorderHomepageSections(body: ReorderHomepageSectionsBody): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(this.http.post<void>('/api/admin/homepage-sections/reorder', body)),
    );
  }

  // ---- Homepage Settings ----
  async getHomepageSettings(): Promise<Result<HomepageSettings>> {
    return this.run(async () => {
      const d = await firstValueFrom(
        this.http.get<{
          videoUrl?: string | null;
          objective?: { ar?: string | null; en?: string | null };
          objectiveAr?: string | null;
          objectiveEn?: string | null;
          cceConceptsAr?: string | null;
          cceConceptsEn?: string | null;
          participatingCountryIds?: string[];
          participatingCountries?: Array<{ id?: string; countryId?: string }>;
        }>('/api/admin/settings/homepage'),
      );
      return {
        videoUrl: d.videoUrl ?? null,
        objectiveAr: d.objective?.ar ?? d.objectiveAr ?? null,
        objectiveEn: d.objective?.en ?? d.objectiveEn ?? null,
        cceConceptsAr: d.cceConceptsAr ?? null,
        cceConceptsEn: d.cceConceptsEn ?? null,
        participatingCountryIds:
          d.participatingCountryIds
          ?? (d.participatingCountries ?? [])
            .map((c) => c.countryId ?? c.id)
            .filter((id): id is string => !!id),
      };
    });
  }
  async updateHomepageSettings(body: UpdateHomepageSettingsBody): Promise<Result<HomepageSettings>> {
    return this.run(() => firstValueFrom(this.http.put<HomepageSettings>('/api/admin/settings/homepage', body)));
  }

  // ---- About Settings ----
  async getAboutSettings(): Promise<Result<AboutSettings>> {
    return this.run(async () => {
      const d = await firstValueFrom(
        this.http.get<{
          description?: { ar?: string; en?: string };
          howToUseVideoUrl?: string | null;
          glossaryEntries?: Array<{ id: string; term?: { ar?: string; en?: string }; definition?: { ar?: string; en?: string } }>;
          knowledgePartners?: Array<{ id: string; name?: { ar?: string; en?: string }; description?: { ar?: string; en?: string }; logoUrl?: string | null; websiteUrl?: string | null }>;
        }>('/api/admin/settings/about'),
      );
      return {
        descriptionAr: d.description?.ar ?? '',
        descriptionEn: d.description?.en ?? '',
        howToUseVideoUrl: d.howToUseVideoUrl ?? null,
        glossaryTerms: (d.glossaryEntries ?? []).map((g) => ({
          id: g.id,
          termAr: g.term?.ar ?? '',
          termEn: g.term?.en ?? '',
          definitionAr: g.definition?.ar ?? '',
          definitionEn: g.definition?.en ?? '',
        })),
        knowledgePartners: (d.knowledgePartners ?? []).map((p) => ({
          id: p.id,
          nameAr: p.name?.ar ?? '',
          nameEn: p.name?.en ?? '',
          logoUrl: p.logoUrl ?? null,
          websiteUrl: p.websiteUrl ?? null,
          descriptionAr: p.description?.ar ?? null,
          descriptionEn: p.description?.en ?? null,
        })),
      };
    });
  }
  async updateAboutSettings(body: UpdateAboutSettingsBody): Promise<Result<AboutSettings>> {
    return this.run(() => firstValueFrom(this.http.put<AboutSettings>('/api/admin/settings/about', {
      descriptionAr: body.descriptionAr ?? '',
      descriptionEn: body.descriptionEn ?? '',
      howToUseVideoUrl: body.howToUseVideoUrl ?? null,
    })));
  }
  async createGlossaryTerm(body: GlossaryTermBody): Promise<Result<GlossaryTerm>> {
    return this.run(() => firstValueFrom(this.http.post<GlossaryTerm>('/api/admin/settings/about/glossary', {
      termAr: body.termAr,
      termEn: body.termEn,
      definitionAr: body.definitionAr,
      definitionEn: body.definitionEn,
    })));
  }
  async updateGlossaryTerm(id: string, body: GlossaryTermBody): Promise<Result<GlossaryTerm>> {
    return this.run(() => firstValueFrom(this.http.put<GlossaryTerm>(`/api/admin/settings/about/glossary/${id}`, {
      termAr: body.termAr,
      termEn: body.termEn,
      definitionAr: body.definitionAr,
      definitionEn: body.definitionEn,
    })));
  }
  async deleteGlossaryTerm(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/settings/about/glossary/${id}`)));
  }
  async createKnowledgePartner(body: KnowledgePartnerBody): Promise<Result<KnowledgePartner>> {
    return this.run(() => firstValueFrom(this.http.post<KnowledgePartner>('/api/admin/settings/about/knowledge-partners', {
      nameAr: body.nameAr,
      nameEn: body.nameEn,
      descriptionAr: body.descriptionAr ?? '',
      descriptionEn: body.descriptionEn ?? '',
      logoUrl: body.logoUrl ?? null,
      websiteUrl: body.websiteUrl ?? null,
    })));
  }
  async updateKnowledgePartner(id: string, body: KnowledgePartnerBody): Promise<Result<KnowledgePartner>> {
    return this.run(() => firstValueFrom(this.http.put<KnowledgePartner>(`/api/admin/settings/about/knowledge-partners/${id}`, {
      nameAr: body.nameAr,
      nameEn: body.nameEn,
      descriptionAr: body.descriptionAr ?? '',
      descriptionEn: body.descriptionEn ?? '',
      logoUrl: body.logoUrl ?? null,
      websiteUrl: body.websiteUrl ?? null,
    })));
  }
  async deleteKnowledgePartner(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/settings/about/knowledge-partners/${id}`)));
  }

  // ---- Policies Settings ----
  async getPoliciesSettings(): Promise<Result<PoliciesSettings>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<{ id: string; sections: ApiPolicySection[] }>('/api/admin/settings/policies'),
      );
      return {
        sections: (res.sections ?? []).map((s) => ({
          id: s.id,
          type: s.type,
          titleAr: s.title?.ar ?? '',
          titleEn: s.title?.en ?? '',
          contentAr: s.content?.ar ?? '',
          contentEn: s.content?.en ?? '',
          orderIndex: s.orderIndex,
        })),
      };
    });
  }
  async createPolicySection(body: PolicySectionBody): Promise<Result<PolicySection>> {
    return this.run(() => firstValueFrom(this.http.post<PolicySection>('/api/admin/settings/policies/sections', {
      type: body.type,
      titleAr: body.titleAr,
      titleEn: body.titleEn,
      contentAr: body.contentAr,
      contentEn: body.contentEn,
    })));
  }
  async updatePolicySection(id: string, body: PolicySectionBody): Promise<Result<PolicySection>> {
    return this.run(() => firstValueFrom(this.http.put<PolicySection>(`/api/admin/settings/policies/sections/${id}`, {
      type: body.type,
      titleAr: body.titleAr,
      titleEn: body.titleEn,
      contentAr: body.contentAr,
      contentEn: body.contentEn,
    })));
  }
  async deletePolicySection(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/settings/policies/sections/${id}`)));
  }
  async reorderPolicySection(id: string, orderIndex: number): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.put<void>(`/api/admin/settings/policies/sections/${id}/order`, { orderIndex })));
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      const error = toFeatureError(err as HttpErrorResponse);
      return { ok: false, error };
    }
  }
}
