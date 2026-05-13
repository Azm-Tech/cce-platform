/**
 * Mock data for the Knowledge Center page (used as a demo fallback when
 * the backend `/api/resources` endpoint is unavailable or empty). Real
 * API responses always win — this only fills the page when the page
 * would otherwise be blank.
 *
 * 25 hand-curated entries spanning all 5 resource types so the type-color
 * card design (Pdf=red, Video=purple, Image=cyan, Link=amber, Document=indigo)
 * shows clearly on first paint.
 */

import type { Resource, ResourceCategory, ResourceListItem } from './knowledge.types';

/**
 * Demo video URLs for the 5 mock Video resources (r6–r10). These are
 * public Creative Commons sample MP4s — verified to be served with
 * proper CORS + 200 OK over HTTPS. When a real backend ships actual
 * video assets, the resource record will carry its own URL and this
 * map becomes irrelevant.
 *
 * Sources:
 *   - test-videos.co.uk: short, lightweight 720p Big Buck Bunny clips
 *     used widely as cross-browser test fixtures. 1–10 MB each.
 *   - blender.org: official Blender open-movie distributions.
 *   - learningcontainer.com: short MP4 fixtures.
 *
 * The `poster` is left empty by default; the <video> element shows
 * the first frame automatically when the user hits play.
 */
interface MockVideoEntry {
  url: string;
  poster: string;            // empty string = no poster (first frame is used)
  durationLabel: string;
  /** 'mp4' renders an HTML5 <video>; 'youtube' renders an <iframe>. */
  provider: 'mp4' | 'youtube';
}

const MOCK_VIDEO_URLS: Record<string, MockVideoEntry> = {
  r6: {
    provider: 'mp4',
    url: 'https://test-videos.co.uk/vids/bigbuckbunny/mp4/h264/720/Big_Buck_Bunny_720_10s_5MB.mp4',
    poster: '',
    durationLabel: '28 min',
  },
  r7: {
    // COP28 Highlights — Pathways to 1.5°C
    // YouTube embed of https://www.youtube.com/watch?v=97vCzpH5Dcc
    // user-provided COP28 reference video.
    provider: 'youtube',
    url: 'https://www.youtube.com/embed/97vCzpH5Dcc?rel=0&modestbranding=1&playsinline=1',
    poster: '',
    durationLabel: '42 min',
  },
  r8: {
    provider: 'mp4',
    url: 'https://test-videos.co.uk/vids/jellyfish/mp4/h264/720/Jellyfish_720_10s_2MB.mp4',
    poster: '',
    durationLabel: '14 min',
  },
  r9: {
    provider: 'mp4',
    url: 'https://download.blender.org/peach/bigbuckbunny_movies/BigBuckBunny_320x180.mp4',
    poster: '',
    durationLabel: '6 min',
  },
  r10: {
    provider: 'mp4',
    url: 'https://test-videos.co.uk/vids/elephantsdream/mp4/h264/720/Elephants_Dream_720_10s_5MB.mp4',
    poster: '',
    durationLabel: '9 min',
  },
};

/**
 * Generic fallback for any Video resource not in MOCK_VIDEO_URLS.
 * Returns the Big Buck Bunny sample so the player always plays.
 */
const FALLBACK_VIDEO: MockVideoEntry = MOCK_VIDEO_URLS['r6'];

/** Look up a demo video URL + poster + duration label for a resource id. */
export function getMockVideo(resourceId: string): MockVideoEntry {
  return MOCK_VIDEO_URLS[resourceId] ?? FALLBACK_VIDEO;
}

/**
 * Per-resource long-form descriptions (English + Arabic). Keyed by
 * resource id. The detail page composes a full {@link Resource} by
 * spreading the matching {@link ResourceListItem} and adding these
 * description fields + a few constants.
 */
const MOCK_DESCRIPTIONS: Record<string, { en: string; ar: string }> = {
  r1: {
    en: `The Saudi Vision 2030 Carbon Reduction Roadmap sets out the Kingdom's pathway to a 278 MtCO₂e annual emission reduction by 2030, anchored by the Saudi Green Initiative (SGI). The document covers four pillars: (1) shifting electricity generation to a 50% renewable mix, (2) deploying carbon capture, utilisation, and storage at scale across the upstream and petrochemical value chains, (3) expanding hydrogen production with NEOM Helios and Aramco/SABIC blue hydrogen pilots, and (4) afforestation and ecosystem restoration with a 10 billion tree pledge.\n\nThis brief is intended for senior policy advisors, sector regulators (MoENRA, MEWA, MoIMR), and large industrial off-takers preparing decarbonisation plans aligned with national targets. It includes interim milestones for 2025 and 2027, the governance map across SGI, the Public Investment Fund, and Aramco, and a methodology note on Scope 1+2 vs. consumption-basis accounting under the SGI framework.`,
    ar: `تحدد خارطة طريق خفض الكربون لرؤية المملكة 2030 المسار للوصول إلى خفض سنوي للانبعاثات بمقدار 278 مليون طن مكافئ ثاني أكسيد الكربون بحلول عام 2030، مرتكزةً على المبادرة السعودية الخضراء. تتناول الوثيقة أربعة محاور: (1) تحويل توليد الكهرباء إلى مزيج متجدد بنسبة 50%، (2) النشر الواسع لتقنيات احتجاز الكربون واستخدامه وتخزينه، (3) التوسع في إنتاج الهيدروجين، (4) التشجير واستعادة النظم البيئية بتعهد 10 مليارات شجرة.`,
  },
  r2: {
    en: `The IPCC's Sixth Assessment Report on Mitigation distils six decades of climate science into actionable, near-term sectoral mitigation strategies. This brief summarises the report's three highest-leverage findings for emerging-market policymakers: (1) the marginal abatement cost curve still favours rapid solar+storage deployment for grid decarbonisation through 2035, (2) industrial process heat above 400°C is the largest unsolved decarbonisation problem and demands targeted R&D investment, and (3) end-use electrification in buildings and ground transport remains the single most predictable lever, with payback periods under 8 years in most jurisdictions when paired with current incentive frameworks.\n\nIt also reviews the equity dimensions, including the IPCC's loss-and-damage framing and what that implies for MENA-region adaptation finance.`,
    ar: `يلخص هذا الموجز نتائج تقرير IPCC السادس حول التخفيف ويقدم ثلاث نتائج عالية الأثر للجهات صانعة القرار في الأسواق الناشئة، بما في ذلك انتشار الطاقة الشمسية والتخزين، والحرارة الصناعية، وكهربة الاستخدامات النهائية.`,
  },
  r3: {
    en: `Circular Economy in MENA — Policy Brief. The MENA region faces a unique combination of resource intensity, growing waste streams, and limited landfill capacity. This brief profiles seven national-scale circular economy initiatives across the region (UAE, Saudi Arabia, Egypt, Morocco, Jordan, Oman, Bahrain) and identifies cross-cutting policy levers: extended producer responsibility schemes, deposit-return systems for beverage containers, mandatory recycled content in construction materials, and industrial symbiosis cluster grants.\n\nKey takeaway: jurisdictions that pair EPR with measurable, time-bound recycled-content mandates achieve 2–4× the diversion rate of those relying on voluntary schemes alone.`,
    ar: `موجز سياسة الاقتصاد الدائري في منطقة الشرق الأوسط وشمال أفريقيا. يحلل سبع مبادرات وطنية ويحدد روافع السياسة المشتركة بما في ذلك مسؤولية المنتج الممتدة وأنظمة الإيداع والاسترداد ومحتوى المعاد تدويره الإلزامي.`,
  },
  r4: {
    en: `Green Hydrogen Production — Technical Feasibility Study. This study evaluates green-hydrogen production economics in three Saudi Arabian sites: NEOM (proximity to Red Sea export, mixed solar+wind), Yanbu (existing petrochemical infrastructure, solar-only), and Jubail (industrial cluster integration). Levelised cost of hydrogen (LCOH) is projected at $1.95–$2.40/kg by 2030 under the central scenario, dropping to $1.50–$1.80/kg by 2035 with electrolyser unit costs assumed to fall 4% per year.\n\nIncludes a sensitivity analysis on electrolyser CAPEX, capacity factor, and grid-electricity backup pricing, plus a comparison against blue hydrogen LCOH at the same sites.`,
    ar: `دراسة جدوى فنية لإنتاج الهيدروجين الأخضر تقيّم اقتصاديات الإنتاج في ثلاثة مواقع: نيوم وينبع والجبيل، مع توقعات بتكلفة موحدة تبلغ $1.95-$2.40/كغ بحلول 2030.`,
  },
  r5: {
    en: `NEOM Sustainability Master Plan. The master plan articulates NEOM's commitment to operate as the first 100%-renewable, regenerative megacity. Core targets: zero-net-emissions in operations by commissioning, a 95% biodiversity net gain, water reuse rates above 90%, and a circular materials economy reaching 70% diversion at maturity.\n\nThe document covers governance (NEOM Sustainability Authority), financing (sustainability-linked sukuk issuance plans), operational standards (NEOM Building Standard supersedes IECC 2021), and reporting (annual third-party-audited sustainability disclosure aligned with ISSB IFRS S1/S2).`,
    ar: `الخطة الرئيسية لاستدامة نيوم — أهداف صافي انبعاثات صفرية في العمليات، صافي ربح للتنوع البيولوجي 95%، وإعادة استخدام المياه بنسبة تتجاوز 90%، ضمن اقتصاد مواد دائري.`,
  },
  r6: {
    en: `Solar Megaprojects in the Arabian Peninsula — Documentary. A 28-minute documentary covering five flagship solar projects: Sakaka (300 MW, the Kingdom's first utility-scale plant), Sudair (1.5 GW), Al Shuaibah (2.6 GW), Mohammed bin Rashid Al Maktoum Solar Park (5 GW eventual capacity), and the Al Dhafra plant in Abu Dhabi (2 GW).\n\nFootage includes drone surveys of installation, interviews with project leads on grid integration, dust-mitigation operations, and the role of bifacial modules + tracker systems in lifting yield by 18–22% in MENA conditions vs. fixed-tilt baselines.`,
    ar: `فيلم وثائقي مدته 28 دقيقة يغطي خمسة مشاريع رائدة للطاقة الشمسية في شبه الجزيرة العربية، بما في ذلك سكاكا وسدير والشعيبة ومجمع محمد بن راشد للطاقة الشمسية ومحطة الظفرة.`,
  },
  r7: {
    en: `COP28 Highlights — Pathways to 1.5°C. Recorded panel highlights from the 28th Conference of the Parties hosted in the UAE. Sessions covered the first Global Stocktake (GST) outcome, the operationalisation of the Loss & Damage Fund, the Tripling Renewables / Doubling Efficiency pledge signed by 130+ countries, and the language transition from "fossil-fuel phase-down" to "transitioning away from fossil fuels in energy systems".\n\nFeaturing remarks from COP28 President Dr. Sultan Al Jaber, IRENA Director-General Francesco La Camera, and selected pavilion-track interviews from the Saudi, Brazilian, and EU delegations.`,
    ar: `مقتطفات من مؤتمر COP28 المُقام في الإمارات تغطي نتائج التقييم العالمي الأول، وتفعيل صندوق الخسائر والأضرار، وتعهد مضاعفة الطاقة المتجددة وكفاءة الطاقة.`,
  },
  r8: {
    en: `Industrial Symbiosis at Yanbu Industrial City. A 14-minute case study on Yanbu's industrial symbiosis network, where the byproduct streams of one facility (e.g., heat from a refinery, CO₂ from an ammonia plant, or brine from a desalination unit) are routed as feedstocks for adjacent operations. The film documents three mature symbiosis links and a planned fourth: heat-recovery to a desalination unit, CO₂ enhancement of a urea plant, brine-to-bromine extraction, and a forthcoming chlorine-to-PVC integration scheduled for 2026.\n\nKey learning: governance — Yanbu's Royal Commission acts as the broker, mapping streams and matching counterparties — is a more decisive success factor than infrastructure cost.`,
    ar: `دراسة حالة لشبكة التكامل الصناعي في مدينة ينبع الصناعية، حيث تُستخدم منتجات ثانوية لمنشأة كمواد أولية لأخرى مجاورة.`,
  },
  r9: {
    en: `Wind Farm Construction — Time-Lapse Documentary. A 6-minute time-lapse of the Dumat Al Jandal wind project, the Kingdom's first commercial-scale wind farm (400 MW). The footage compresses 18 months of construction into a continuous narrative: foundation pile-driving, hub assembly, blade transport across desert terrain, single-blade lifts via the rare crane fleet capable of working in MENA wind regimes, and grid-side substation commissioning.\n\nIncludes voice-over commentary on supply-chain decisions (turbine selection, port logistics through Jubail) and on adapting offshore-grade wind technology for inland desert operating conditions.`,
    ar: `فيلم وثائقي مكثف مدته 6 دقائق لمشروع دومة الجندل، أول مزرعة رياح تجارية في المملكة بقدرة 400 ميغاواط.`,
  },
  r10: {
    en: `How Carbon Markets Work — Animated Explainer. A 9-minute animated explainer covering the mechanics of compliance carbon markets (EU ETS, RGGI, China national ETS, the Kingdom's Voluntary Carbon Market via Saudi Tadawul) and voluntary markets (Verra, Gold Standard, the new Integrity Council for Voluntary Carbon Markets framework).\n\nWalks through credit issuance, retirement, additionality testing, and the difference between offsets and reductions. Designed as an onboarding piece for new sustainability team members and an accessible primer for executive audiences.`,
    ar: `شرح متحرك مدته 9 دقائق يغطي آليات أسواق الكربون الإلزامية والطوعية، بما في ذلك إصدار الأرصدة والتقاعد واختبار الإضافة.`,
  },
  r11: {
    en: `Global Carbon Emissions Heatmap 2024. Annual heatmap dataset visualising territorial CO₂ emissions for 195 countries, normalised by population and by GDP, with side-by-side comparison panels for production-based vs. consumption-based accounting.\n\nThe accompanying methodology note details the data-source pipeline (Global Carbon Project, IEA, EDGAR) and the imputation approach used for the 12 countries with reporting gaps. Suitable for inclusion in policy briefings, board decks, and academic teaching.`,
    ar: `مجموعة بيانات سنوية للخريطة الحرارية تعرض انبعاثات ثاني أكسيد الكربون الإقليمية لـ195 دولة، مع مقارنة بين المحاسبة على أساس الإنتاج والاستهلاك.`,
  },
  r12: {
    en: `Sakaka Solar Plant — Aerial Photography. A curated set of 24 high-resolution aerial photographs of the Sakaka 300 MW PV plant in Al Jouf province, captured in golden-hour light over the December 2024 spring inspection cycle. The set includes wide context shots showing the panel array against the Nafud landscape, mid-altitude row detail, and close-up tracker pivot points.\n\nLicence: free for editorial and educational use with attribution to the Saudi Green Initiative photo library.`,
    ar: `مجموعة من 24 صورة جوية عالية الدقة لمحطة سكاكا للطاقة الشمسية بقدرة 300 ميغاواط في منطقة الجوف.`,
  },
  r13: {
    en: `Renewable Energy Capacity Infographic — GCC. Single-page infographic showing 2020 vs. 2024 vs. projected 2030 renewable capacity for each GCC member state, with breakdown by technology (solar PV, CSP, wind, waste-to-energy). Includes a comparative panel against EU27 and a "build pipeline" panel showing announced-but-not-yet-operational projects.`,
    ar: `إنفوغرافيك من صفحة واحدة يعرض قدرة الطاقة المتجددة لكل دولة من دول مجلس التعاون الخليجي للأعوام 2020 و2024 و2030 المتوقعة.`,
  },
  r14: {
    en: `Recycling Process Flow Diagram. Comprehensive flow diagram showing the journey of municipal solid waste through a modern materials recovery facility (MRF), from collection through optical sorting, ferrous/non-ferrous separation, polymer disambiguation, and downstream cleansing.\n\nIncludes annotated tonnage assumptions for a typical 100,000 tonnes/year facility and recovery-rate benchmarks by stream.`,
    ar: `مخطط تدفق شامل يوضح رحلة النفايات الصلبة البلدية عبر مرفق استرداد مواد حديث، بما في ذلك الفرز الضوئي وفصل المعادن.`,
  },
  r15: {
    en: `IRENA Renewable Capacity Statistics — Live Dashboard. Direct link to IRENA's quarterly-updated dashboard tracking installed renewable capacity across 195 countries. Filterable by technology, region, and time period; includes net additions, capacity factor estimates, and capital cost trend lines.`,
    ar: `رابط مباشر إلى لوحة بيانات إيرينا المحدّثة فصلياً لتتبع قدرات الطاقة المتجددة المنشأة عبر 195 دولة.`,
  },
  r16: {
    en: `Saudi Green Initiative — Official Portal. The official portal of the Saudi Green Initiative (SGI) and the regional Middle East Green Initiative (MGI). Use this resource for authoritative pledges, current programme dashboards, partner directory, and the official tree-planting tracker.`,
    ar: `البوابة الرسمية للمبادرة السعودية الخضراء ومبادرة الشرق الأوسط الأخضر، تتضمن التعهدات وبرامج التتبع ودليل الشركاء.`,
  },
  r17: {
    en: `World Bank Climate Action Tracker. Reference site for the World Bank's climate-aligned country diagnostics, programme pipeline, and adaptation finance metrics.`,
    ar: `موقع مرجعي لتشخيصات البنك الدولي للدول المتوافقة مع المناخ ومسارات البرامج ومقاييس تمويل التكيف.`,
  },
  r18: {
    en: `Green Climate Fund — Project Database. Live database of all Green Climate Fund-approved projects, searchable by country, sector, and outcome category. Useful for benchmarking expected GCF co-financing terms when preparing concept notes.`,
    ar: `قاعدة بيانات مباشرة لجميع مشاريع صندوق المناخ الأخضر المعتمدة، قابلة للبحث حسب الدولة والقطاع وفئة النتائج.`,
  },
  r19: {
    en: `Net-Zero by 2060 — Technical White Paper. The Kingdom's white paper articulating the technical pathway to economy-wide net-zero greenhouse-gas emissions by 2060. The paper specifies the sequencing of grid decarbonisation, industrial CCUS deployment, transport electrification, and carbon-sink afforestation across three phases (2024–2030 foundation; 2030–2045 transformation; 2045–2060 residual abatement).\n\nIncludes detailed technology-readiness curves, marginal abatement cost projections, and a calibrated sectoral emissions budget. Authored by the Saudi Green Initiative technical office in collaboration with KAUST and KFUPM research teams.`,
    ar: `ورقة بيضاء فنية تحدد المسار التقني لتحقيق الحياد الكربوني على مستوى الاقتصاد بحلول عام 2060، بما في ذلك تسلسل إزالة الكربون من الشبكة ونشر CCUS وكهربة النقل.`,
  },
  r20: {
    en: `ESG Reporting Standards — Practitioner Guide. A 64-page practitioner's guide for sustainability and finance teams preparing ESG disclosures under the IFRS Sustainability Disclosure Standards (S1, S2), the EU CSRD/ESRS, and the SEC Climate-Related Disclosure rules. Covers materiality assessment methodology, scope-3 calculation approaches, climate-scenario analysis under the TCFD framework, and the assurance-readiness checklist.`,
    ar: `دليل مهني من 64 صفحة لفِرق الاستدامة والتمويل التي تُعدّ إفصاحات ESG وفق معايير IFRS وESRS وقواعد SEC.`,
  },
  r21: {
    en: `Smart Cities Indicators Framework. The reference framework for measuring smart-city outcomes across six domains: governance, economy, mobility, environment, people, and living. Aligned with ISO 37120 (city indicators) and ITU-T Y.4901 (key performance indicators for smart sustainable cities).\n\nUsed by the Royal Commission for Riyadh City and the NEOM Sustainability Authority as the canonical KPI taxonomy.`,
    ar: `الإطار المرجعي لقياس نتائج المدن الذكية عبر ستة محاور: الحوكمة، الاقتصاد، التنقل، البيئة، الناس، والمعيشة.`,
  },
  r22: {
    en: `Industrial Decarbonization — Sector Roadmap. Sector-by-sector decarbonisation roadmap covering Saudi Arabia's three highest-emission industrial verticals: petrochemicals, cement, and steel. For each vertical the roadmap details current emissions intensity, available abatement levers (process electrification, hydrogen substitution, CCUS, low-carbon feedstock), capital intensity of each lever, and a sequenced 2030 / 2040 / 2050 deployment plan.\n\nIncludes case studies of leading global moves (Hybrit, H2 Green Steel, Salzgitter SALCOS) and how those translate to Saudi sites.`,
    ar: `خارطة طريق قطاعية لإزالة الكربون تغطي القطاعات الثلاثة الأعلى انبعاثاً: البتروكيماويات والإسمنت والفولاذ.`,
  },
  r23: {
    en: `Carbon Pricing Mechanisms — Comparative Analysis. A comparative analysis of carbon-pricing mechanisms across 18 jurisdictions (cap-and-trade systems, hybrid systems, and pure carbon taxes). The paper evaluates each system on five dimensions: coverage (% of national emissions), price stability, revenue use, leakage controls, and political durability.\n\nThe Kingdom's Voluntary Carbon Market (operated via Tadawul) is profiled in detail, with particular attention to article 6 of the Paris Agreement and how voluntary credits interface with national NDC accounting.`,
    ar: `تحليل مقارن لآليات تسعير الكربون عبر 18 ولاية قضائية، يقيم كل نظام على خمسة أبعاد بما في ذلك التغطية واستقرار الأسعار.`,
  },
  r24: {
    en: `Hydrogen Strategy 2025 — Implementation Plan. The Kingdom's revised hydrogen strategy implementation plan with a target of 4 million tonnes per annum (Mtpa) of clean hydrogen exports by 2030. The plan covers green-hydrogen production hubs (NEOM Helios — 600 ktpa first phase), blue-hydrogen production from existing gas fields with CCS, dedicated export-ready ammonia and methanol facilities, and the regulatory framework for safety, certification, and trade.\n\nKey policy innovation: the Clean Hydrogen Certification Scheme (CHCS), which provides life-cycle emission certificates compatible with EU CBAM, the UK CBAM, and Japanese / Korean import requirements.`,
    ar: `خطة تنفيذ استراتيجية الهيدروجين المنقحة في المملكة، تستهدف 4 ملايين طن سنوياً من صادرات الهيدروجين النظيف بحلول 2030.`,
  },
  r25: {
    en: `Riyadh Urban Sustainability Index — Methodology. The methodology document for the Riyadh Urban Sustainability Index (RUSI), a composite indicator tracking the city's sustainability performance across 14 sub-indices. Maintained by the Royal Commission for Riyadh City and used to assess progress toward the Riyadh Master Plan 2030 sustainability targets.\n\nThe doc explains the indicator selection rationale, weighting methodology, data sources, and the annual recalibration process.`,
    ar: `وثيقة منهجية مؤشر استدامة الرياض الحضري (RUSI)، وهو مؤشر مركّب يتتبع أداء استدامة المدينة عبر 14 مؤشراً فرعياً.`,
  },
};

/**
 * Build a full {@link Resource} object for a given list-item id by
 * spreading the matching list item and adding the matching long-form
 * description. Returns `null` if the id isn't in the mock dataset.
 */
export function getMockResource(id: string): Resource | null {
  const item = MOCK_RESOURCES.find((r) => r.id === id);
  if (!item) return null;
  const desc = MOCK_DESCRIPTIONS[id] ?? { en: '', ar: '' };
  return {
    ...item,
    descriptionEn: desc.en,
    descriptionAr: desc.ar,
    uploadedById: 'demo-author',
    assetFileId: `asset-${id}`,
    isCenterManaged: true,
  };
}

export const MOCK_CATEGORIES: ResourceCategory[] = [
  { id: 'c1', nameEn: 'Climate Policy', nameAr: 'سياسة المناخ', slug: 'climate-policy', parentId: null, orderIndex: 1 },
  { id: 'c1-1', nameEn: 'Carbon Markets', nameAr: 'أسواق الكربون', slug: 'carbon-markets', parentId: 'c1', orderIndex: 1 },
  { id: 'c1-2', nameEn: 'Net-Zero Targets', nameAr: 'أهداف الحياد الكربوني', slug: 'net-zero', parentId: 'c1', orderIndex: 2 },
  { id: 'c2', nameEn: 'Renewable Energy', nameAr: 'الطاقة المتجددة', slug: 'renewable-energy', parentId: null, orderIndex: 2 },
  { id: 'c2-1', nameEn: 'Solar', nameAr: 'الطاقة الشمسية', slug: 'solar', parentId: 'c2', orderIndex: 1 },
  { id: 'c2-2', nameEn: 'Wind', nameAr: 'طاقة الرياح', slug: 'wind', parentId: 'c2', orderIndex: 2 },
  { id: 'c3', nameEn: 'Circular Economy', nameAr: 'الاقتصاد الدائري', slug: 'circular-economy', parentId: null, orderIndex: 3 },
  { id: 'c3-1', nameEn: 'Recycling', nameAr: 'إعادة التدوير', slug: 'recycling', parentId: 'c3', orderIndex: 1 },
  { id: 'c3-2', nameEn: 'Industrial Symbiosis', nameAr: 'التكامل الصناعي', slug: 'industrial-symbiosis', parentId: 'c3', orderIndex: 2 },
  { id: 'c4', nameEn: 'Sustainable Cities', nameAr: 'المدن المستدامة', slug: 'sustainable-cities', parentId: null, orderIndex: 4 },
  { id: 'c5', nameEn: 'Green Finance', nameAr: 'التمويل الأخضر', slug: 'green-finance', parentId: null, orderIndex: 5 },
];

export const MOCK_RESOURCES: ResourceListItem[] = [
  // ── Pdf (red) ──
  { id: 'r1', titleEn: 'Saudi Vision 2030 — Carbon Reduction Roadmap', titleAr: 'رؤية المملكة 2030 — خارطة طريق خفض الكربون', resourceType: 'Pdf', categoryId: 'c1', countryId: 'sa', publishedOn: '2024-09-15', viewCount: 12482 },
  { id: 'r2', titleEn: 'IPCC Sixth Assessment Report — Mitigation Strategies', titleAr: 'تقرير IPCC السادس — استراتيجيات التخفيف', resourceType: 'Pdf', categoryId: 'c1-2', countryId: null, publishedOn: '2024-04-08', viewCount: 8763 },
  { id: 'r3', titleEn: 'Circular Economy Policy Brief — MENA Region', titleAr: 'موجز سياسة الاقتصاد الدائري — منطقة الشرق الأوسط', resourceType: 'Pdf', categoryId: 'c3', countryId: null, publishedOn: '2025-02-12', viewCount: 5621 },
  { id: 'r4', titleEn: 'Green Hydrogen Production — Technical Feasibility Study', titleAr: 'إنتاج الهيدروجين الأخضر — دراسة جدوى فنية', resourceType: 'Pdf', categoryId: 'c2', countryId: 'sa', publishedOn: '2024-11-30', viewCount: 9402 },
  { id: 'r5', titleEn: 'NEOM Sustainability Master Plan', titleAr: 'الخطة الرئيسية للاستدامة في نيوم', resourceType: 'Pdf', categoryId: 'c4', countryId: 'sa', publishedOn: '2025-01-22', viewCount: 15734 },

  // ── Video (purple) ──
  { id: 'r6', titleEn: 'Solar Megaprojects in the Arabian Peninsula', titleAr: 'المشاريع الشمسية الكبرى في شبه الجزيرة العربية', resourceType: 'Video', categoryId: 'c2-1', countryId: 'sa', publishedOn: '2024-08-04', viewCount: 24891 },
  { id: 'r7', titleEn: 'COP28 Highlights — Pathways to 1.5°C', titleAr: 'أبرز ما جاء في COP28 — مسارات نحو 1.5 درجة مئوية', resourceType: 'Video', categoryId: 'c1', countryId: null, publishedOn: '2024-12-13', viewCount: 31204 },
  { id: 'r8', titleEn: 'Industrial Symbiosis at Yanbu Industrial City', titleAr: 'التكامل الصناعي في مدينة ينبع الصناعية', resourceType: 'Video', categoryId: 'c3-2', countryId: 'sa', publishedOn: '2024-10-19', viewCount: 8147 },
  { id: 'r9', titleEn: 'Wind Farm Construction — Time-Lapse Documentary', titleAr: 'بناء مزرعة رياح — فيلم وثائقي مكثف', resourceType: 'Video', categoryId: 'c2-2', countryId: 'sa', publishedOn: '2024-06-21', viewCount: 18539 },
  { id: 'r10', titleEn: 'How Carbon Markets Work — Animated Explainer', titleAr: 'كيف تعمل أسواق الكربون — شرح متحرك', resourceType: 'Video', categoryId: 'c1-1', countryId: null, publishedOn: '2025-03-05', viewCount: 11023 },

  // ── Image (cyan) ──
  { id: 'r11', titleEn: 'Global Carbon Emissions Heatmap 2024', titleAr: 'خريطة حرارية لانبعاثات الكربون 2024', resourceType: 'Image', categoryId: 'c1', countryId: null, publishedOn: '2024-07-11', viewCount: 6712 },
  { id: 'r12', titleEn: 'Sakaka Solar Plant — Aerial Photography', titleAr: 'محطة سكاكا الشمسية — صور جوية', resourceType: 'Image', categoryId: 'c2-1', countryId: 'sa', publishedOn: '2024-05-28', viewCount: 9286 },
  { id: 'r13', titleEn: 'Renewable Energy Capacity Infographic — GCC', titleAr: 'إنفوغرافيك لطاقة المصادر المتجددة — دول الخليج', resourceType: 'Image', categoryId: 'c2', countryId: null, publishedOn: '2025-01-09', viewCount: 4502 },
  { id: 'r14', titleEn: 'Recycling Process Flow Diagram', titleAr: 'مخطط تدفق عملية إعادة التدوير', resourceType: 'Image', categoryId: 'c3-1', countryId: null, publishedOn: '2024-09-02', viewCount: 3814 },

  // ── Link (amber) ──
  { id: 'r15', titleEn: 'IRENA Renewable Capacity Statistics — Live Dashboard', titleAr: 'إحصائيات الطاقة المتجددة من إيرينا — لوحة بيانات حية', resourceType: 'Link', categoryId: 'c2', countryId: null, publishedOn: '2025-04-01', viewCount: 7451 },
  { id: 'r16', titleEn: 'Saudi Green Initiative — Official Portal', titleAr: 'المبادرة السعودية الخضراء — البوابة الرسمية', resourceType: 'Link', categoryId: 'c1', countryId: 'sa', publishedOn: '2024-03-15', viewCount: 19438 },
  { id: 'r17', titleEn: 'World Bank Climate Action Tracker', titleAr: 'متعقب العمل المناخي للبنك الدولي', resourceType: 'Link', categoryId: 'c1', countryId: null, publishedOn: '2024-11-08', viewCount: 5604 },
  { id: 'r18', titleEn: 'Green Climate Fund — Project Database', titleAr: 'صندوق المناخ الأخضر — قاعدة بيانات المشاريع', resourceType: 'Link', categoryId: 'c5', countryId: null, publishedOn: '2024-12-01', viewCount: 6879 },

  // ── Document (indigo) ──
  { id: 'r19', titleEn: 'Net-Zero by 2060 — Technical White Paper', titleAr: 'الحياد الكربوني بحلول 2060 — ورقة تقنية', resourceType: 'Document', categoryId: 'c1-2', countryId: 'sa', publishedOn: '2024-10-04', viewCount: 13927 },
  { id: 'r20', titleEn: 'ESG Reporting Standards — Practitioner Guide', titleAr: 'معايير تقارير ESG — دليل الممارس', resourceType: 'Document', categoryId: 'c5', countryId: null, publishedOn: '2025-02-18', viewCount: 7245 },
  { id: 'r21', titleEn: 'Smart Cities Indicators Framework', titleAr: 'إطار مؤشرات المدن الذكية', resourceType: 'Document', categoryId: 'c4', countryId: null, publishedOn: '2024-06-30', viewCount: 4108 },
  { id: 'r22', titleEn: 'Industrial Decarbonization — Sector Roadmap', titleAr: 'إزالة الكربون الصناعي — خارطة طريق القطاع', resourceType: 'Document', categoryId: 'c3', countryId: 'sa', publishedOn: '2024-08-22', viewCount: 8963 },
  { id: 'r23', titleEn: 'Carbon Pricing Mechanisms — Comparative Analysis', titleAr: 'آليات تسعير الكربون — تحليل مقارن', resourceType: 'Document', categoryId: 'c1-1', countryId: null, publishedOn: '2025-03-21', viewCount: 5536 },
  { id: 'r24', titleEn: 'Hydrogen Strategy 2025 — Implementation Plan', titleAr: 'استراتيجية الهيدروجين 2025 — خطة التنفيذ', resourceType: 'Document', categoryId: 'c2', countryId: 'sa', publishedOn: '2025-04-12', viewCount: 11247 },
  { id: 'r25', titleEn: 'Riyadh Urban Sustainability Index — Methodology', titleAr: 'مؤشر استدامة الرياض الحضري — المنهجية', resourceType: 'Document', categoryId: 'c4', countryId: 'sa', publishedOn: '2024-09-09', viewCount: 6418 },
];
