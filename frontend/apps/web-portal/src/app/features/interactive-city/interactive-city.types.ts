/**
 * Mirrors backend CityTechnologyDto. v0.1.0 only consumes the
 * technologies-list endpoint; scenario shapes land in Sub-7.
 */
export interface CityTechnology {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  categoryAr: string;
  categoryEn: string;
  carbonImpactKgPerYear: number;
  costUsd: number;
  iconUrl: string | null;
}
