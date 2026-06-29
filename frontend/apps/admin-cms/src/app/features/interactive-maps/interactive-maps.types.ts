export interface InteractiveMapDto {
  id: string;
  nameAr: string | null;
  nameEn: string | null;
  descriptionAr: string | null;
  descriptionEn: string | null;
  isActive: boolean;
}

export interface InteractiveMapNodeDto {
  id: string;
  interactiveMapId: string;
  nameAr: string | null;
  nameEn: string | null;
  iconKey: string | null;
  category: number | null;
  categoryNameAr: string | null;
  categoryNameEn: string | null;
  level: number;
  parentId: string | null;
  topicId: string;
  isActive: boolean;
  tags: InteractiveMapTagDto[] | null;
}

export interface InteractiveMapTagDto {
  id: string;
  nameAr: string | null;
  nameEn: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

/** Body for `PUT /api/admin/interactive-maps` — metadata only; `isActive` is
 *  no longer part of the update contract. */
export interface UpdateInteractiveMapRequest {
  nameAr: string | null;
  nameEn: string | null;
  descriptionAr: string | null;
  descriptionEn: string | null;
}

export interface CreateInteractiveMapNodeRequest {
  nameAr: string | null;
  nameEn: string | null;
  iconKey: string | null;
  category: number | null;
  categoryNameAr: string | null;
  categoryNameEn: string | null;
  level: number;
  parentId: string | null;
  topicId: string;
}

export interface UpdateInteractiveMapNodeRequest extends CreateInteractiveMapNodeRequest {
  isActive: boolean;
}
