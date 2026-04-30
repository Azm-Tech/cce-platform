import type { PagedResult } from '../identity/identity.types';

export interface ResourceCategory {
  id: string;
  nameAr: string;
  nameEn: string;
  slug: string;
  parentId: string | null;
  orderIndex: number;
  isActive: boolean;
}

export interface CreateResourceCategoryBody {
  nameAr: string;
  nameEn: string;
  slug: string;
  parentId?: string | null;
  orderIndex: number;
}

export interface UpdateResourceCategoryBody {
  nameAr: string;
  nameEn: string;
  orderIndex: number;
  isActive: boolean;
}

export interface Topic {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  slug: string;
  parentId: string | null;
  iconUrl: string | null;
  orderIndex: number;
  isActive: boolean;
}

export interface CreateTopicBody {
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  slug: string;
  parentId?: string | null;
  iconUrl?: string | null;
  orderIndex: number;
}

export interface UpdateTopicBody {
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  orderIndex: number;
  isActive: boolean;
}

export type { PagedResult };
