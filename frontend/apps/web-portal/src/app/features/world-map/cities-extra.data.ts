/**
 * Secondary cities dataset — minimal metadata, used to populate the
 * world map at higher zoom levels. Entries lack rich initiatives /
 * summaries (those live in cities.data.ts for the curated 60); this
 * file provides geographic coverage so users can find their region's
 * capitals + major metros at a glance.
 *
 * Carbon tier here is a rough heuristic based on country grid mix
 * + transport profile — not a verified per-city measurement.
 */

import type { CarbonTier } from './cities.data';

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

/* eslint-disable max-len */
export const CITIES_EXTRA: readonly CityMin[] = [
  // Africa
  { id: 'algiers',     name: 'Algiers',     country: 'Algeria',     countryCode: 'DZ', lat: 36.7538, lon: 3.0588,   population: 2900000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'tunis',       name: 'Tunis',       country: 'Tunisia',     countryCode: 'TN', lat: 36.8065, lon: 10.1815,  population: 1100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'tripoli',     name: 'Tripoli',     country: 'Libya',       countryCode: 'LY', lat: 32.8872, lon: 13.1913,  population: 1200000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'khartoum',    name: 'Khartoum',    country: 'Sudan',       countryCode: 'SD', lat: 15.5007, lon: 32.5599,  population: 5800000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'dakar',       name: 'Dakar',       country: 'Senegal',     countryCode: 'SN', lat: 14.7167, lon: -17.4677, population: 3000000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'abidjan',     name: 'Abidjan',     country: 'Côte d’Ivoire', countryCode: 'CI', lat: 5.3600, lon: -4.0083,  population: 5500000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'accra',       name: 'Accra',       country: 'Ghana',       countryCode: 'GH', lat: 5.6037,  lon: -0.1870,  population: 2500000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'kampala',     name: 'Kampala',     country: 'Uganda',      countryCode: 'UG', lat: 0.3476,  lon: 32.5825,  population: 1700000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'kigali',      name: 'Kigali',      country: 'Rwanda',      countryCode: 'RW', lat: -1.9706, lon: 30.1044,  population: 1200000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'dar-es-salaam', name: 'Dar es Salaam', country: 'Tanzania', countryCode: 'TZ', lat: -6.7924, lon: 39.2083, population: 7000000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'kinshasa',    name: 'Kinshasa',    country: 'DR Congo',    countryCode: 'CD', lat: -4.4419, lon: 15.2663,  population: 17000000, carbonTier: 'medium', kind: 'standard' },
  { id: 'luanda',      name: 'Luanda',      country: 'Angola',      countryCode: 'AO', lat: -8.8390, lon: 13.2894,  population: 8300000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'harare',      name: 'Harare',      country: 'Zimbabwe',    countryCode: 'ZW', lat: -17.8252,lon: 31.0335,  population: 2150000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'johannesburg',name: 'Johannesburg',country: 'South Africa',countryCode: 'ZA', lat: -26.2041,lon: 28.0473,  population: 6200000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'durban',      name: 'Durban',      country: 'South Africa',countryCode: 'ZA', lat: -29.8587,lon: 31.0218,  population: 3700000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'maputo',      name: 'Maputo',      country: 'Mozambique',  countryCode: 'MZ', lat: -25.9692,lon: 32.5732,  population: 1100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'antananarivo',name: 'Antananarivo',country: 'Madagascar',  countryCode: 'MG', lat: -18.8792,lon: 47.5079,  population: 3300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'asmara',      name: 'Asmara',      country: 'Eritrea',     countryCode: 'ER', lat: 15.3229, lon: 38.9251,  population: 800000,   carbonTier: 'medium', kind: 'standard' },
  { id: 'mogadishu',   name: 'Mogadishu',   country: 'Somalia',     countryCode: 'SO', lat: 2.0469,  lon: 45.3182,  population: 2500000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'djibouti',    name: 'Djibouti',    country: 'Djibouti',    countryCode: 'DJ', lat: 11.8251, lon: 42.5903,  population: 600000,   carbonTier: 'medium', kind: 'standard' },
  { id: 'rabat',       name: 'Rabat',       country: 'Morocco',     countryCode: 'MA', lat: 34.0209, lon: -6.8416,  population: 1900000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'marrakesh',   name: 'Marrakesh',   country: 'Morocco',     countryCode: 'MA', lat: 31.6295, lon: -7.9811,  population: 1300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'alexandria',  name: 'Alexandria',  country: 'Egypt',       countryCode: 'EG', lat: 31.2001, lon: 29.9187,  population: 5200000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'kano',        name: 'Kano',        country: 'Nigeria',     countryCode: 'NG', lat: 12.0022, lon: 8.5920,   population: 4100000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'abuja',       name: 'Abuja',       country: 'Nigeria',     countryCode: 'NG', lat: 9.0765,  lon: 7.3986,   population: 3500000,  carbonTier: 'high',   kind: 'standard' },

  // Middle East
  { id: 'baghdad',     name: 'Baghdad',     country: 'Iraq',        countryCode: 'IQ', lat: 33.3152, lon: 44.3661,  population: 7200000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'damascus',    name: 'Damascus',    country: 'Syria',       countryCode: 'SY', lat: 33.5138, lon: 36.2765,  population: 2100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'beirut',      name: 'Beirut',      country: 'Lebanon',     countryCode: 'LB', lat: 33.8938, lon: 35.5018,  population: 2400000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'sanaa',       name: 'Sanaa',       country: 'Yemen',       countryCode: 'YE', lat: 15.3694, lon: 44.1910,  population: 3300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'mecca',       name: 'Mecca',       country: 'Saudi Arabia',countryCode: 'SA', lat: 21.3891, lon: 39.8579,  population: 2000000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'medina',      name: 'Medina',      country: 'Saudi Arabia',countryCode: 'SA', lat: 24.5247, lon: 39.5692,  population: 1500000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'dammam',      name: 'Dammam',      country: 'Saudi Arabia',countryCode: 'SA', lat: 26.4207, lon: 50.0888,  population: 1500000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'abu-dhabi',   name: 'Abu Dhabi',   country: 'United Arab Emirates', countryCode: 'AE', lat: 24.4539, lon: 54.3773, population: 1500000, carbonTier: 'medium', kind: 'standard' },
  { id: 'sharjah',     name: 'Sharjah',     country: 'United Arab Emirates', countryCode: 'AE', lat: 25.3463, lon: 55.4209, population: 1700000, carbonTier: 'medium', kind: 'standard' },
  { id: 'jerusalem',   name: 'Jerusalem',   country: 'Israel',      countryCode: 'IL', lat: 31.7683, lon: 35.2137,  population: 950000,   carbonTier: 'medium', kind: 'standard' },
  { id: 'gaza',        name: 'Gaza',        country: 'Palestine',   countryCode: 'PS', lat: 31.5017, lon: 34.4668,  population: 600000,   carbonTier: 'medium', kind: 'standard' },
  { id: 'isfahan',     name: 'Isfahan',     country: 'Iran',        countryCode: 'IR', lat: 32.6539, lon: 51.6660,  population: 2200000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'mashhad',     name: 'Mashhad',     country: 'Iran',        countryCode: 'IR', lat: 36.2974, lon: 59.6062,  population: 3300000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'kabul',       name: 'Kabul',       country: 'Afghanistan', countryCode: 'AF', lat: 34.5553, lon: 69.2075,  population: 4400000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'lahore',      name: 'Lahore',      country: 'Pakistan',    countryCode: 'PK', lat: 31.5497, lon: 74.3436,  population: 13100000, carbonTier: 'high',   kind: 'standard' },
  { id: 'islamabad',   name: 'Islamabad',   country: 'Pakistan',    countryCode: 'PK', lat: 33.6844, lon: 73.0479,  population: 1100000,  carbonTier: 'medium', kind: 'standard' },

  // Europe
  { id: 'dublin',      name: 'Dublin',      country: 'Ireland',     countryCode: 'IE', lat: 53.3498, lon: -6.2603,  population: 1400000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'edinburgh',   name: 'Edinburgh',   country: 'United Kingdom', countryCode: 'GB', lat: 55.9533, lon: -3.1883, population: 540000, carbonTier: 'low', kind: 'standard' },
  { id: 'manchester',  name: 'Manchester',  country: 'United Kingdom', countryCode: 'GB', lat: 53.4808, lon: -2.2426, population: 2700000, carbonTier: 'low', kind: 'standard' },
  { id: 'lyon',        name: 'Lyon',        country: 'France',      countryCode: 'FR', lat: 45.7640, lon: 4.8357,   population: 1700000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'marseille',   name: 'Marseille',   country: 'France',      countryCode: 'FR', lat: 43.2965, lon: 5.3698,   population: 1600000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'munich',      name: 'Munich',      country: 'Germany',     countryCode: 'DE', lat: 48.1351, lon: 11.5820,  population: 1500000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'hamburg',     name: 'Hamburg',     country: 'Germany',     countryCode: 'DE', lat: 53.5511, lon: 9.9937,   population: 1900000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'frankfurt',   name: 'Frankfurt',   country: 'Germany',     countryCode: 'DE', lat: 50.1109, lon: 8.6821,   population: 760000,   carbonTier: 'low',    kind: 'standard' },
  { id: 'vienna',      name: 'Vienna',      country: 'Austria',     countryCode: 'AT', lat: 48.2082, lon: 16.3738,  population: 1900000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'zurich',      name: 'Zurich',      country: 'Switzerland', countryCode: 'CH', lat: 47.3769, lon: 8.5417,   population: 1400000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'geneva',      name: 'Geneva',      country: 'Switzerland', countryCode: 'CH', lat: 46.2044, lon: 6.1432,   population: 600000,   carbonTier: 'low',    kind: 'standard' },
  { id: 'milan',       name: 'Milan',       country: 'Italy',       countryCode: 'IT', lat: 45.4642, lon: 9.1900,   population: 3300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'naples',      name: 'Naples',      country: 'Italy',       countryCode: 'IT', lat: 40.8518, lon: 14.2681,  population: 3100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'barcelona',   name: 'Barcelona',   country: 'Spain',       countryCode: 'ES', lat: 41.3851, lon: 2.1734,   population: 5600000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'lisbon',      name: 'Lisbon',      country: 'Portugal',    countryCode: 'PT', lat: 38.7223, lon: -9.1393,  population: 2900000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'porto',       name: 'Porto',       country: 'Portugal',    countryCode: 'PT', lat: 41.1579, lon: -8.6291,  population: 1700000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'brussels',    name: 'Brussels',    country: 'Belgium',     countryCode: 'BE', lat: 50.8503, lon: 4.3517,   population: 2100000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'rotterdam',   name: 'Rotterdam',   country: 'Netherlands', countryCode: 'NL', lat: 51.9244, lon: 4.4777,   population: 1200000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'warsaw',      name: 'Warsaw',      country: 'Poland',      countryCode: 'PL', lat: 52.2297, lon: 21.0122,  population: 3100000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'krakow',      name: 'Kraków',      country: 'Poland',      countryCode: 'PL', lat: 50.0647, lon: 19.9450,  population: 800000,   carbonTier: 'high',   kind: 'standard' },
  { id: 'prague',      name: 'Prague',      country: 'Czechia',     countryCode: 'CZ', lat: 50.0755, lon: 14.4378,  population: 1300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'budapest',    name: 'Budapest',    country: 'Hungary',     countryCode: 'HU', lat: 47.4979, lon: 19.0402,  population: 1750000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'bucharest',   name: 'Bucharest',   country: 'Romania',     countryCode: 'RO', lat: 44.4268, lon: 26.1025,  population: 2100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'sofia',       name: 'Sofia',       country: 'Bulgaria',    countryCode: 'BG', lat: 42.6977, lon: 23.3219,  population: 1400000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'athens',      name: 'Athens',      country: 'Greece',      countryCode: 'GR', lat: 37.9838, lon: 23.7275,  population: 3200000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'belgrade',    name: 'Belgrade',    country: 'Serbia',      countryCode: 'RS', lat: 44.7866, lon: 20.4489,  population: 1700000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'zagreb',      name: 'Zagreb',      country: 'Croatia',     countryCode: 'HR', lat: 45.8150, lon: 15.9819,  population: 800000,   carbonTier: 'medium', kind: 'standard' },
  { id: 'ljubljana',   name: 'Ljubljana',   country: 'Slovenia',    countryCode: 'SI', lat: 46.0569, lon: 14.5058,  population: 290000,   carbonTier: 'low',    kind: 'standard' },
  { id: 'helsinki',    name: 'Helsinki',    country: 'Finland',     countryCode: 'FI', lat: 60.1699, lon: 24.9384,  population: 1300000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'reykjavik',   name: 'Reykjavik',   country: 'Iceland',     countryCode: 'IS', lat: 64.1466, lon: -21.9426, population: 230000,   carbonTier: 'low',    kind: 'standard' },
  { id: 'tallinn',     name: 'Tallinn',     country: 'Estonia',     countryCode: 'EE', lat: 59.4370, lon: 24.7536,  population: 440000,   carbonTier: 'low',    kind: 'standard' },
  { id: 'riga',        name: 'Riga',        country: 'Latvia',      countryCode: 'LV', lat: 56.9496, lon: 24.1052,  population: 620000,   carbonTier: 'low',    kind: 'standard' },
  { id: 'vilnius',     name: 'Vilnius',     country: 'Lithuania',   countryCode: 'LT', lat: 54.6872, lon: 25.2797,  population: 580000,   carbonTier: 'low',    kind: 'standard' },
  { id: 'kyiv',        name: 'Kyiv',        country: 'Ukraine',     countryCode: 'UA', lat: 50.4501, lon: 30.5234,  population: 3000000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'minsk',       name: 'Minsk',       country: 'Belarus',     countryCode: 'BY', lat: 53.9006, lon: 27.5590,  population: 2000000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'st-petersburg', name: 'Saint Petersburg', country: 'Russia', countryCode: 'RU', lat: 59.9343, lon: 30.3351, population: 5400000, carbonTier: 'high', kind: 'standard' },

  // Asia
  { id: 'almaty',      name: 'Almaty',      country: 'Kazakhstan',  countryCode: 'KZ', lat: 43.2220, lon: 76.8512,  population: 2000000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'astana',      name: 'Astana',      country: 'Kazakhstan',  countryCode: 'KZ', lat: 51.1605, lon: 71.4704,  population: 1200000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'tashkent',    name: 'Tashkent',    country: 'Uzbekistan',  countryCode: 'UZ', lat: 41.2995, lon: 69.2401,  population: 2900000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'tbilisi',     name: 'Tbilisi',     country: 'Georgia',     countryCode: 'GE', lat: 41.7151, lon: 44.8271,  population: 1200000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'yerevan',     name: 'Yerevan',     country: 'Armenia',     countryCode: 'AM', lat: 40.1792, lon: 44.4991,  population: 1100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'baku',        name: 'Baku',        country: 'Azerbaijan',  countryCode: 'AZ', lat: 40.4093, lon: 49.8671,  population: 2300000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'ankara',      name: 'Ankara',      country: 'Türkiye',     countryCode: 'TR', lat: 39.9334, lon: 32.8597,  population: 5700000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'kolkata',     name: 'Kolkata',     country: 'India',       countryCode: 'IN', lat: 22.5726, lon: 88.3639,  population: 14900000, carbonTier: 'high',   kind: 'standard' },
  { id: 'chennai',     name: 'Chennai',     country: 'India',       countryCode: 'IN', lat: 13.0827, lon: 80.2707,  population: 11000000, carbonTier: 'high',   kind: 'standard' },
  { id: 'bengaluru',   name: 'Bengaluru',   country: 'India',       countryCode: 'IN', lat: 12.9716, lon: 77.5946,  population: 13200000, carbonTier: 'high',   kind: 'standard' },
  { id: 'hyderabad',   name: 'Hyderabad',   country: 'India',       countryCode: 'IN', lat: 17.3850, lon: 78.4867,  population: 10500000, carbonTier: 'high',   kind: 'standard' },
  { id: 'ahmedabad',   name: 'Ahmedabad',   country: 'India',       countryCode: 'IN', lat: 23.0225, lon: 72.5714,  population: 8400000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'colombo',     name: 'Colombo',     country: 'Sri Lanka',   countryCode: 'LK', lat: 6.9271,  lon: 79.8612,  population: 5700000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'kathmandu',   name: 'Kathmandu',   country: 'Nepal',       countryCode: 'NP', lat: 27.7172, lon: 85.3240,  population: 1500000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'dhaka',       name: 'Dhaka',       country: 'Bangladesh',  countryCode: 'BD', lat: 23.8103, lon: 90.4125,  population: 22000000, carbonTier: 'high',   kind: 'standard' },
  { id: 'yangon',      name: 'Yangon',      country: 'Myanmar',     countryCode: 'MM', lat: 16.8409, lon: 96.1735,  population: 5400000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'phnom-penh',  name: 'Phnom Penh',  country: 'Cambodia',    countryCode: 'KH', lat: 11.5564, lon: 104.9282, population: 2100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'vientiane',   name: 'Vientiane',   country: 'Laos',        countryCode: 'LA', lat: 17.9757, lon: 102.6331, population: 950000,   carbonTier: 'low',    kind: 'standard' },
  { id: 'hanoi',       name: 'Hanoi',       country: 'Vietnam',     countryCode: 'VN', lat: 21.0285, lon: 105.8542, population: 8400000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'taipei',      name: 'Taipei',      country: 'Taiwan',      countryCode: 'TW', lat: 25.0330, lon: 121.5654, population: 7000000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'osaka',       name: 'Osaka',       country: 'Japan',       countryCode: 'JP', lat: 34.6937, lon: 135.5023, population: 19000000, carbonTier: 'medium', kind: 'standard' },
  { id: 'kyoto',       name: 'Kyoto',       country: 'Japan',       countryCode: 'JP', lat: 35.0116, lon: 135.7681, population: 1500000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'sapporo',     name: 'Sapporo',     country: 'Japan',       countryCode: 'JP', lat: 43.0618, lon: 141.3545, population: 2700000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'busan',       name: 'Busan',       country: 'South Korea', countryCode: 'KR', lat: 35.1796, lon: 129.0756, population: 3400000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'pyongyang',   name: 'Pyongyang',   country: 'North Korea', countryCode: 'KP', lat: 39.0392, lon: 125.7625, population: 3100000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'ulaanbaatar', name: 'Ulaanbaatar', country: 'Mongolia',    countryCode: 'MN', lat: 47.8864, lon: 106.9057, population: 1500000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'guangzhou',   name: 'Guangzhou',   country: 'China',       countryCode: 'CN', lat: 23.1291, lon: 113.2644, population: 18000000, carbonTier: 'high',   kind: 'standard' },
  { id: 'chengdu',     name: 'Chengdu',     country: 'China',       countryCode: 'CN', lat: 30.5728, lon: 104.0668, population: 16000000, carbonTier: 'high',   kind: 'standard' },
  { id: 'wuhan',       name: 'Wuhan',       country: 'China',       countryCode: 'CN', lat: 30.5928, lon: 114.3055, population: 11000000, carbonTier: 'high',   kind: 'standard' },
  { id: 'xian',        name: 'Xi’an',  country: 'China',       countryCode: 'CN', lat: 34.3416, lon: 108.9398, population: 13000000, carbonTier: 'high',   kind: 'standard' },
  { id: 'macau',       name: 'Macau',       country: 'Macau SAR',   countryCode: 'MO', lat: 22.1987, lon: 113.5439, population: 700000,   carbonTier: 'medium', kind: 'standard' },
  { id: 'surabaya',    name: 'Surabaya',    country: 'Indonesia',   countryCode: 'ID', lat: -7.2575, lon: 112.7521, population: 9100000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'bandung',     name: 'Bandung',     country: 'Indonesia',   countryCode: 'ID', lat: -6.9175, lon: 107.6191, population: 8400000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'cebu',        name: 'Cebu',        country: 'Philippines', countryCode: 'PH', lat: 10.3157, lon: 123.8854, population: 3000000,  carbonTier: 'medium', kind: 'standard' },

  // Americas
  { id: 'havana',      name: 'Havana',      country: 'Cuba',        countryCode: 'CU', lat: 23.1136, lon: -82.3666, population: 2100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'kingston',    name: 'Kingston',    country: 'Jamaica',     countryCode: 'JM', lat: 17.9714, lon: -76.7936, population: 1300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'santo-domingo', name: 'Santo Domingo', country: 'Dominican Republic', countryCode: 'DO', lat: 18.4861, lon: -69.9312, population: 3500000, carbonTier: 'medium', kind: 'standard' },
  { id: 'san-juan',    name: 'San Juan',    country: 'Puerto Rico', countryCode: 'PR', lat: 18.4655, lon: -66.1057, population: 2100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'guatemala-city', name: 'Guatemala City', country: 'Guatemala', countryCode: 'GT', lat: 14.6349, lon: -90.5069, population: 3000000, carbonTier: 'medium', kind: 'standard' },
  { id: 'san-salvador',name: 'San Salvador',country: 'El Salvador', countryCode: 'SV', lat: 13.6929, lon: -89.2182, population: 2300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'tegucigalpa', name: 'Tegucigalpa', country: 'Honduras',    countryCode: 'HN', lat: 14.0723, lon: -87.1921, population: 1300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'managua',     name: 'Managua',     country: 'Nicaragua',   countryCode: 'NI', lat: 12.1149, lon: -86.2362, population: 1100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'san-jose-cr', name: 'San José',    country: 'Costa Rica',  countryCode: 'CR', lat: 9.9281,  lon: -84.0907, population: 2200000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'panama-city', name: 'Panama City', country: 'Panama',      countryCode: 'PA', lat: 8.9824,  lon: -79.5199, population: 1900000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'caracas',     name: 'Caracas',     country: 'Venezuela',   countryCode: 'VE', lat: 10.4806, lon: -66.9036, population: 5200000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'medellin',    name: 'Medellín',    country: 'Colombia',    countryCode: 'CO', lat: 6.2476,  lon: -75.5658, population: 4100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'quito',       name: 'Quito',       country: 'Ecuador',     countryCode: 'EC', lat: -0.1807, lon: -78.4678, population: 2800000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'guayaquil',   name: 'Guayaquil',   country: 'Ecuador',     countryCode: 'EC', lat: -2.1709, lon: -79.9224, population: 3200000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'la-paz',      name: 'La Paz',      country: 'Bolivia',     countryCode: 'BO', lat: -16.4897,lon: -68.1193, population: 2300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'asuncion',    name: 'Asunción',    country: 'Paraguay',    countryCode: 'PY', lat: -25.2637,lon: -57.5759, population: 3500000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'montevideo',  name: 'Montevideo',  country: 'Uruguay',     countryCode: 'UY', lat: -34.9011,lon: -56.1645, population: 1750000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'salvador-br', name: 'Salvador',    country: 'Brazil',      countryCode: 'BR', lat: -12.9777,lon: -38.5016, population: 3900000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'brasilia',    name: 'Brasília',    country: 'Brazil',      countryCode: 'BR', lat: -15.7975,lon: -47.8919, population: 4800000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'belo-horizonte', name: 'Belo Horizonte', country: 'Brazil', countryCode: 'BR', lat: -19.9167, lon: -43.9345, population: 6100000, carbonTier: 'medium', kind: 'standard' },
  { id: 'curitiba',    name: 'Curitiba',    country: 'Brazil',      countryCode: 'BR', lat: -25.4284,lon: -49.2733, population: 3700000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'cordoba',     name: 'Córdoba',     country: 'Argentina',   countryCode: 'AR', lat: -31.4201,lon: -64.1888, population: 1600000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'houston',     name: 'Houston',     country: 'United States',countryCode: 'US', lat: 29.7604,lon: -95.3698, population: 7200000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'phoenix',     name: 'Phoenix',     country: 'United States',countryCode: 'US', lat: 33.4484,lon: -112.0740,population: 5100000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'philadelphia',name: 'Philadelphia',country: 'United States',countryCode: 'US', lat: 39.9526,lon: -75.1652, population: 6200000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'dallas',      name: 'Dallas',      country: 'United States',countryCode: 'US', lat: 32.7767,lon: -96.7970, population: 7600000,  carbonTier: 'high',   kind: 'standard' },
  { id: 'miami',       name: 'Miami',       country: 'United States',countryCode: 'US', lat: 25.7617,lon: -80.1918, population: 6300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'boston',      name: 'Boston',      country: 'United States',countryCode: 'US', lat: 42.3601,lon: -71.0589, population: 4900000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'seattle',     name: 'Seattle',     country: 'United States',countryCode: 'US', lat: 47.6062,lon: -122.3321,population: 4000000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'denver',      name: 'Denver',      country: 'United States',countryCode: 'US', lat: 39.7392,lon: -104.9903,population: 3000000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'atlanta',     name: 'Atlanta',     country: 'United States',countryCode: 'US', lat: 33.7490,lon: -84.3880, population: 6100000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'washington',  name: 'Washington',  country: 'United States',countryCode: 'US', lat: 38.9072,lon: -77.0369, population: 6300000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'montreal',    name: 'Montreal',    country: 'Canada',      countryCode: 'CA', lat: 45.5017,lon: -73.5673, population: 4300000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'calgary',     name: 'Calgary',     country: 'Canada',      countryCode: 'CA', lat: 51.0447,lon: -114.0719,population: 1400000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'edmonton',    name: 'Edmonton',    country: 'Canada',      countryCode: 'CA', lat: 53.5461,lon: -113.4938,population: 1500000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'guadalajara', name: 'Guadalajara', country: 'Mexico',      countryCode: 'MX', lat: 20.6597,lon: -103.3496,population: 5000000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'monterrey',   name: 'Monterrey',   country: 'Mexico',      countryCode: 'MX', lat: 25.6866,lon: -100.3161,population: 4900000,  carbonTier: 'high',   kind: 'standard' },

  // Oceania
  { id: 'brisbane',    name: 'Brisbane',    country: 'Australia',   countryCode: 'AU', lat: -27.4698, lon: 153.0251, population: 2500000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'perth',       name: 'Perth',       country: 'Australia',   countryCode: 'AU', lat: -31.9523, lon: 115.8613, population: 2200000,  carbonTier: 'medium', kind: 'standard' },
  { id: 'adelaide',    name: 'Adelaide',    country: 'Australia',   countryCode: 'AU', lat: -34.9285, lon: 138.6007, population: 1400000,  carbonTier: 'low',    kind: 'standard' },
  { id: 'canberra',    name: 'Canberra',    country: 'Australia',   countryCode: 'AU', lat: -35.2809, lon: 149.1300, population: 460000,   carbonTier: 'low',    kind: 'standard' },
  { id: 'christchurch',name: 'Christchurch',country: 'New Zealand', countryCode: 'NZ', lat: -43.5321, lon: 172.6362, population: 380000,   carbonTier: 'low',    kind: 'standard' },
  { id: 'suva',        name: 'Suva',        country: 'Fiji',        countryCode: 'FJ', lat: -18.1248, lon: 178.4501, population: 200000,   carbonTier: 'medium', kind: 'standard' },
  { id: 'port-moresby',name: 'Port Moresby',country: 'Papua New Guinea', countryCode: 'PG', lat: -9.4438, lon: 147.1803, population: 380000, carbonTier: 'medium', kind: 'standard' },
];
