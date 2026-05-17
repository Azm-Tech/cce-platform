export type CarbonTier = 'low' | 'medium' | 'high';

export interface City {
  readonly id: string;
  readonly name: string;
  readonly country: string;
  readonly countryCode: string;
  readonly lat: number;
  readonly lon: number;
  readonly population: number;
  readonly carbonTier: CarbonTier;
  readonly summary: string;
  readonly initiatives: readonly string[];
}

export interface CityMin {
  readonly id: string;
  readonly name: string;
  readonly country: string;
  readonly countryCode: string;
  readonly lat: number;
  readonly lon: number;
  readonly population: number;
  readonly carbonTier: CarbonTier;
  readonly kind: 'standard';
}

export type FeaturedCity = City & { readonly kind: 'featured' };
export type AnyCity = FeaturedCity | CityMin;
