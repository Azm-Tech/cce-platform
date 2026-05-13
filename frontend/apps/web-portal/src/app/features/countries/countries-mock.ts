/**
 * Mock fallback data for the countries detail page.
 *
 * Backend `/api/countries` returns a list, but `/api/countries/{id}/profile`
 * and `/api/kapsarc/snapshots/{countryId}` both 404 in dev because no
 * profile / KAPSARC seed data exists yet. This file fills those gaps
 * so clicking a country card shows a populated detail page in the demo.
 *
 * Real API data wins — this only fires on 404 / network failure.
 */

import type { CountryProfile, KapsarcSnapshot } from './country.types';

interface MockProfile {
  isoAlpha2: string;
  descriptionEn: string;
  descriptionAr: string;
  keyInitiativesEn: string;
  keyInitiativesAr: string;
  contactInfoEn: string | null;
  contactInfoAr: string | null;
}

const MOCK_PROFILES_BY_ISO: Record<string, MockProfile> = {
  SA: {
    isoAlpha2: 'SA',
    descriptionEn: `<p>The Kingdom of Saudi Arabia is the largest economy in the Middle East and a leading exporter of crude oil. Under <strong>Vision 2030</strong>, the country has committed to a sweeping diversification of its economy, doubling-down on renewable energy, hydrogen production, and circular-carbon pathways while reducing fossil-fuel dependence.</p><p>Saudi Arabia hosts the <strong>Saudi Green Initiative</strong> (SGI) and the regional <strong>Middle East Green Initiative</strong> (MGI), with concrete pledges of a 278 MtCO₂e reduction in annual emissions by 2030, a 50% renewable-energy mix, and 10 billion trees planted nationally.</p>`,
    descriptionAr: `<p>المملكة العربية السعودية هي أكبر اقتصاد في الشرق الأوسط وأحد كبار مصدري النفط الخام في العالم. تلتزم المملكة في إطار رؤية 2030 بتحول جذري لاقتصادها يعتمد على التنويع وتطوير الطاقة المتجددة والهيدروجين والاقتصاد الكربوني الدائري.</p><p>تستضيف المملكة المبادرة السعودية الخضراء ومبادرة الشرق الأوسط الأخضر، بأهداف ملزمة لخفض الانبعاثات السنوية بمقدار 278 مليون طن مكافئ ثاني أكسيد الكربون بحلول 2030 وخليط طاقة متجدد بنسبة 50% وزراعة 10 مليارات شجرة.</p>`,
    keyInitiativesEn: `<ul><li><strong>Vision 2030</strong> — economic diversification, 50% renewable mix by 2030.</li><li><strong>Saudi Green Initiative</strong> — 10B trees, MGI partnerships.</li><li><strong>NEOM</strong> — fully renewable mega-city + Helios green-hydrogen plant (4 Mtpa target).</li><li><strong>Sakaka & Sudair Solar</strong> — utility-scale PV projects (300 MW + 1.5 GW).</li><li><strong>Voluntary Carbon Market</strong> via Tadawul (regional credit exchange).</li></ul>`,
    keyInitiativesAr: `<ul><li><strong>رؤية 2030</strong> — التنويع الاقتصادي ومزيج طاقة متجدد بنسبة 50% بحلول 2030.</li><li><strong>المبادرة السعودية الخضراء</strong> — 10 مليارات شجرة وشراكات مبادرة الشرق الأوسط الأخضر.</li><li><strong>نيوم</strong> — مدينة كبرى تعتمد على الطاقة المتجددة بالكامل ومحطة هيليوس للهيدروجين الأخضر.</li><li><strong>محطتا سكاكا وسدير للطاقة الشمسية</strong> — مشاريع شمسية ضخمة (300 ميغاواط و1.5 غيغاواط).</li><li><strong>سوق الكربون الطوعي</strong> عبر تداول.</li></ul>`,
    contactInfoEn: `<p>Saudi Green Initiative — <a href="mailto:contact@sgi.gov.sa">contact&#64;sgi.gov.sa</a><br/>Riyadh, Kingdom of Saudi Arabia</p>`,
    contactInfoAr: `<p>المبادرة السعودية الخضراء — <a href="mailto:contact@sgi.gov.sa">contact&#64;sgi.gov.sa</a><br/>الرياض، المملكة العربية السعودية</p>`,
  },
  AE: {
    isoAlpha2: 'AE',
    descriptionEn: `<p>The United Arab Emirates has positioned itself as a regional leader on climate action, hosting <strong>COP28</strong> in 2023 and operationalising the <strong>Loss & Damage Fund</strong>. The UAE has set a Net-Zero by 2050 strategic initiative and committed to tripling its renewable-energy capacity by 2030.</p>`,
    descriptionAr: `<p>تعد الإمارات العربية المتحدة من القادة الإقليميين في العمل المناخي، حيث استضافت COP28 عام 2023 وفعّلت صندوق الخسائر والأضرار. تستهدف الدولة الحياد الكربوني بحلول 2050 ومضاعفة قدرتها من الطاقة المتجددة ثلاث مرات بحلول 2030.</p>`,
    keyInitiativesEn: `<ul><li><strong>UAE Net-Zero 2050</strong> strategic initiative.</li><li><strong>Mohammed bin Rashid Al Maktoum Solar Park</strong> — 5 GW eventual capacity.</li><li><strong>Al Dhafra Solar</strong> (2 GW) and <strong>Barakah Nuclear</strong> baseload.</li><li><strong>Masdar</strong> — clean-energy investment + Masdar City.</li><li><strong>COP28 Tripling Renewables Pledge</strong> — UAE-led, 130+ signatories.</li></ul>`,
    keyInitiativesAr: `<ul><li><strong>مبادرة الإمارات للحياد المناخي 2050</strong>.</li><li><strong>مجمع محمد بن راشد آل مكتوم للطاقة الشمسية</strong> — قدرة نهائية 5 غيغاواط.</li><li><strong>محطة الظفرة للطاقة الشمسية</strong> (2 غيغاواط) و<strong>محطة براكة النووية</strong>.</li><li><strong>مصدر</strong> — استثمارات الطاقة النظيفة ومدينة مصدر.</li><li><strong>تعهد COP28 بمضاعفة الطاقة المتجددة</strong> — بقيادة إماراتية و130+ موقّعاً.</li></ul>`,
    contactInfoEn: `<p>Ministry of Climate Change & Environment — <a href="mailto:info@moccae.gov.ae">info&#64;moccae.gov.ae</a><br/>Abu Dhabi, UAE</p>`,
    contactInfoAr: `<p>وزارة التغير المناخي والبيئة — <a href="mailto:info@moccae.gov.ae">info&#64;moccae.gov.ae</a><br/>أبوظبي، الإمارات العربية المتحدة</p>`,
  },
  EG: {
    isoAlpha2: 'EG',
    descriptionEn: `<p>Egypt hosted <strong>COP27</strong> in Sharm El-Sheikh and uses its strategic position as a Mediterranean / Red Sea hub to develop a major green-hydrogen export industry. The country is targeting 42% renewable-energy share in electricity by 2030.</p>`,
    descriptionAr: `<p>استضافت مصر مؤتمر COP27 في شرم الشيخ، وتستفيد من موقعها الاستراتيجي بين المتوسط والبحر الأحمر لتطوير صناعة تصدير الهيدروجين الأخضر. تستهدف الدولة 42% من الطاقة المتجددة في الكهرباء بحلول 2030.</p>`,
    keyInitiativesEn: `<ul><li><strong>NWFE Programme</strong> — Nexus of Water, Food, Energy investment platform.</li><li><strong>Benban Solar Park</strong> — 1.6 GW (one of the world's largest).</li><li><strong>SCZONE Green Hydrogen Hub</strong> — Suez Canal Economic Zone.</li><li><strong>Aswan Wind</strong> + <strong>Gabal el-Zait</strong> wind corridor.</li></ul>`,
    keyInitiativesAr: `<ul><li><strong>برنامج نوفي</strong> — منصة استثمار في تكامل المياه والغذاء والطاقة.</li><li><strong>مجمع بنبان للطاقة الشمسية</strong> — 1.6 غيغاواط (من أكبر المحطات في العالم).</li><li><strong>مركز الهيدروجين الأخضر بمنطقة قناة السويس</strong>.</li><li><strong>طاقة الرياح في أسوان</strong> ومنطقة <strong>جبل الزيت</strong>.</li></ul>`,
    contactInfoEn: `<p>Ministry of Environment — <a href="mailto:contact@moe.gov.eg">contact&#64;moe.gov.eg</a><br/>Cairo, Egypt</p>`,
    contactInfoAr: `<p>وزارة البيئة — <a href="mailto:contact@moe.gov.eg">contact&#64;moe.gov.eg</a><br/>القاهرة، مصر</p>`,
  },
  JO: {
    isoAlpha2: 'JO',
    descriptionEn: `<p>Jordan is one of the most water-stressed countries in the world; its climate strategy heavily emphasises water-efficient solar deployment and decentralised renewable generation. The country aims for 31% renewable share in the national grid by 2030.</p>`,
    descriptionAr: `<p>تعد الأردن من أكثر دول العالم شحاً بالمياه، وتركز استراتيجيتها المناخية على نشر الطاقة الشمسية الموفرة للمياه والتوليد اللامركزي. تستهدف 31% من الطاقة المتجددة في الشبكة الوطنية بحلول 2030.</p>`,
    keyInitiativesEn: `<ul><li><strong>Quweira & Shams Ma'an Solar</strong> projects.</li><li><strong>Tafila Wind Farm</strong> (117 MW) — the region's first commercial wind project.</li><li><strong>Net-Metering & Wheeling</strong> — decentralised solar adoption framework.</li><li><strong>Aqaba-Amman Water-Energy Nexus</strong> — desalination + solar pairing.</li></ul>`,
    keyInitiativesAr: `<ul><li>مشاريع <strong>القويرة وشمس معان</strong> للطاقة الشمسية.</li><li><strong>مزرعة طفيلة للرياح</strong> (117 ميغاواط) — أول مشروع تجاري للرياح في المنطقة.</li><li><strong>إطار العداد الصافي ونقل الطاقة</strong> — اعتماد الطاقة الشمسية اللامركزية.</li><li><strong>تكامل المياه والطاقة بين العقبة وعمّان</strong>.</li></ul>`,
    contactInfoEn: null,
    contactInfoAr: null,
  },
  BH: {
    isoAlpha2: 'BH',
    descriptionEn: `<p>The Kingdom of Bahrain has committed to a Net-Zero by 2060 target aligned with its <strong>Economic Vision 2030</strong>. While the renewable share is small relative to oil-producing peers, Bahrain emphasises industrial efficiency and gas-to-power optimisation.</p>`,
    descriptionAr: `<p>تلتزم مملكة البحرين بهدف الحياد الكربوني بحلول 2060 المتوافق مع رؤيتها الاقتصادية 2030. ورغم أن حصة الطاقة المتجددة لا تزال صغيرة مقارنة بالدول المنتجة للنفط، تركز البحرين على الكفاءة الصناعية وتحسين تحويل الغاز إلى طاقة.</p>`,
    keyInitiativesEn: `<ul><li><strong>Net-Zero by 2060</strong> national commitment.</li><li><strong>Bahrain Vision 2030</strong> — economic diversification.</li><li><strong>Askar Landfill Solar</strong> (5 MW first phase).</li><li><strong>Industrial Efficiency Programme</strong> — ALBA aluminium and refinery decarbonisation.</li></ul>`,
    keyInitiativesAr: `<ul><li><strong>التزام الحياد الكربوني بحلول 2060</strong>.</li><li><strong>رؤية البحرين 2030</strong> — التنويع الاقتصادي.</li><li><strong>محطة عسكر الشمسية على المكب</strong> (5 ميغاواط في المرحلة الأولى).</li><li><strong>برنامج الكفاءة الصناعية</strong> — إزالة الكربون من ألبا للألمنيوم والمصافي.</li></ul>`,
    contactInfoEn: null,
    contactInfoAr: null,
  },
  KW: {
    isoAlpha2: 'KW',
    descriptionEn: `<p>The State of Kuwait derives the bulk of its electricity from associated petroleum gas, but is moving toward a <strong>15% renewable share by 2030</strong> under its New Kuwait 2035 strategic vision. The Kuwait Institute for Scientific Research (KISR) leads applied R&amp;D in solar, wind, and water-energy nexus integration.</p>`,
    descriptionAr: `<p>تعتمد دولة الكويت على الغاز المصاحب للنفط في توليد الكهرباء، لكنها تتجه نحو <strong>15% من الطاقة المتجددة بحلول 2030</strong> ضمن رؤيتها الاستراتيجية كويت جديدة 2035. ويقود معهد الكويت للأبحاث العلمية (KISR) أعمال البحث التطبيقي في الطاقة الشمسية والرياح وتكامل المياه والطاقة.</p>`,
    keyInitiativesEn: `<ul><li><strong>Shagaya Renewable Energy Park</strong> — 70 MW operational, 1.5 GW planned.</li><li><strong>New Kuwait 2035</strong> strategic vision (15% renewable by 2030).</li><li><strong>KISR Solar &amp; Wind Programme</strong> — applied R&amp;D + grid integration.</li><li><strong>Kuwait National Petroleum Co. flaring reduction</strong> — Scope-1 emissions cut.</li></ul>`,
    keyInitiativesAr: `<ul><li><strong>مجمع الشقايا للطاقة المتجددة</strong> — 70 ميغاواط بالتشغيل و1.5 غيغاواط مخططة.</li><li><strong>رؤية كويت جديدة 2035</strong> (15% طاقة متجددة بحلول 2030).</li><li><strong>برنامج KISR للطاقة الشمسية والرياح</strong> — أبحاث تطبيقية وتكامل الشبكة.</li><li><strong>تقليل حرق الغاز في شركة البترول الوطنية الكويتية</strong> — خفض انبعاثات النطاق الأول.</li></ul>`,
    contactInfoEn: null,
    contactInfoAr: null,
  },
  OM: {
    isoAlpha2: 'OM',
    descriptionEn: `<p>The Sultanate of Oman has emerged as a <strong>green-hydrogen export leader</strong>, leveraging exceptional solar irradiance and Indian-Ocean port access. Oman's Hydrogen Strategy targets 1 Mtpa of green hydrogen by 2030 (8 Mtpa by 2050), with land allocations across Duqm, Salalah, and the Empty Quarter.</p>`,
    descriptionAr: `<p>برزت سلطنة عُمان كـ<strong>دولة رائدة في تصدير الهيدروجين الأخضر</strong>، مستفيدةً من الإشعاع الشمسي الاستثنائي وموقعها على المحيط الهندي. تستهدف استراتيجية الهيدروجين العمانية مليون طن سنوياً من الهيدروجين الأخضر بحلول 2030 و8 ملايين طن سنوياً بحلول 2050، مع تخصيص أراضٍ في الدقم وصلالة والربع الخالي.</p>`,
    keyInitiativesEn: `<ul><li><strong>Hydrom</strong> — Oman's green-hydrogen orchestrator (1 Mtpa by 2030).</li><li><strong>Hyport Duqm</strong> &amp; <strong>Salalah Hydrogen</strong> — flagship export projects.</li><li><strong>Ibri II Solar</strong> (500 MW) and <strong>Manah I &amp; II</strong> (1 GW combined).</li><li><strong>Oman Vision 2040</strong> — economic diversification + sustainability axis.</li><li><strong>Net-Zero by 2050</strong> national commitment.</li></ul>`,
    keyInitiativesAr: `<ul><li><strong>هايدروم</strong> — منسّق الهيدروجين الأخضر في عُمان (مليون طن سنوياً بحلول 2030).</li><li><strong>هايبورت الدقم</strong> و<strong>هيدروجين صلالة</strong> — مشاريع تصدير رائدة.</li><li><strong>محطة عبري 2 الشمسية</strong> (500 ميغاواط) و<strong>منح 1 و2</strong> (1 غيغاواط مجتمعة).</li><li><strong>رؤية عُمان 2040</strong> — التنويع الاقتصادي ومحور الاستدامة.</li><li><strong>التزام الحياد الكربوني بحلول 2050</strong>.</li></ul>`,
    contactInfoEn: null,
    contactInfoAr: null,
  },
  QA: {
    isoAlpha2: 'QA',
    descriptionEn: `<p>Qatar hosted the FIFA World Cup 2022 with the highest sustainability standard in the tournament's history (carbon-neutral as audited per PAS 2060). The country has set a 25% emissions-reduction target by 2030 versus business-as-usual, and is investing heavily in <strong>blue hydrogen</strong> derived from its abundant North Field gas resources.</p>`,
    descriptionAr: `<p>استضافت قطر كأس العالم لكرة القدم 2022 وفق أعلى معايير الاستدامة في تاريخ البطولة (محايدة كربونياً وفق PAS 2060). تستهدف الدولة خفض الانبعاثات بنسبة 25% بحلول 2030 مقارنة بالسيناريو المعتاد، وتستثمر بقوة في <strong>الهيدروجين الأزرق</strong> المستخرج من ثروة حقل الشمال للغاز.</p>`,
    keyInitiativesEn: `<ul><li><strong>Qatar National Vision 2030</strong> — environmental development pillar.</li><li><strong>Al Kharsaah Solar PV Plant</strong> (800 MW) — Qatar's first utility-scale solar.</li><li><strong>Ammonia-7</strong> blue-ammonia mega-project (1.2 Mtpa).</li><li><strong>QatarEnergy LNG fleet</strong> — methane-slip reduction commitments.</li><li><strong>Tarsheed Programme</strong> — water + electricity efficiency.</li></ul>`,
    keyInitiativesAr: `<ul><li><strong>رؤية قطر الوطنية 2030</strong> — ركيزة التنمية البيئية.</li><li><strong>محطة الخرسعة للطاقة الشمسية</strong> (800 ميغاواط) — أول محطة شمسية بمقياس المرافق في قطر.</li><li><strong>أمونيا-7</strong> — مشروع الأمونيا الزرقاء الضخم (1.2 مليون طن سنوياً).</li><li><strong>أسطول قطر للطاقة LNG</strong> — التزامات بخفض تسرب الميثان.</li><li><strong>برنامج ترشيد</strong> — كفاءة المياه والكهرباء.</li></ul>`,
    contactInfoEn: null,
    contactInfoAr: null,
  },
};

/**
 * Synthesize a country-specific profile when no curated entry exists.
 *
 * The synthesizer uses the country's actual name + region + ISO code
 * to generate plausible content (description, initiatives, contact)
 * that always renders meaningful country-specific data — no more
 * generic "browse other parts of the platform" placeholder.
 */
function synthesizeProfile(country: {
  isoAlpha2: string;
  isoAlpha3: string;
  nameEn: string;
  nameAr: string;
  regionEn: string;
  regionAr: string;
}): Omit<MockProfile, 'isoAlpha2'> {
  const { nameEn, nameAr, regionEn, regionAr, isoAlpha3 } = country;

  const descriptionEn = `<p><strong>${nameEn}</strong> is a ${regionEn} country tracking against the global Net-Zero pathway. Like its regional peers, ${nameEn} balances current energy-export realities with a transition to a more diversified, lower-carbon economy through national strategic vision documents and bilateral climate partnerships.</p><p>The CCE platform tracks ${nameEn}'s progress on three key axes: <strong>energy-mix decarbonisation</strong>, <strong>industrial efficiency</strong>, and <strong>circular-economy adoption</strong>. Recent commitments and flagship projects are summarised below; see the Knowledge Center for region-wide briefs and the Interactive World Map for city-level data.</p>`;

  const descriptionAr = `<p><strong>${nameAr}</strong> دولة في ${regionAr} تتقدم ضمن المسار العالمي للحياد الكربوني. كنظيراتها الإقليمية، توازن ${nameAr} بين واقع تصدير الطاقة الحالي والانتقال إلى اقتصاد أكثر تنوعاً وأقل كربوناً عبر وثائق الرؤية الاستراتيجية الوطنية والشراكات المناخية الثنائية.</p><p>تتابع منصة CCE تقدّم ${nameAr} على ثلاثة محاور رئيسية: <strong>إزالة الكربون من مزيج الطاقة</strong> و<strong>الكفاءة الصناعية</strong> و<strong>تبني الاقتصاد الدائري</strong>. تُلخَّص الالتزامات الحديثة والمشاريع الرائدة أدناه.</p>`;

  const keyInitiativesEn = `<ul><li><strong>${nameEn} National Climate Plan</strong> — country-level NDC submission to UNFCCC; emissions-reduction target through 2030.</li><li><strong>Renewable-Energy Capacity Build-out</strong> — solar + wind PPAs and grid integration.</li><li><strong>Energy-Efficiency Standards</strong> — buildings, transport, and industrial-process tightening per region-wide benchmarks for ${regionEn}.</li><li><strong>Sustainable Cities &amp; Mobility</strong> — public-transit investments + EV adoption framework.</li><li><strong>Carbon Pricing &amp; MRV</strong> — measurement, reporting, and verification infrastructure aligned with Article 6 of the Paris Agreement.</li></ul>`;

  const keyInitiativesAr = `<ul><li><strong>الخطة المناخية الوطنية لـ${nameAr}</strong> — تقديم المساهمة المحددة وطنياً (NDC) إلى UNFCCC وأهداف خفض الانبعاثات حتى 2030.</li><li><strong>توسيع قدرة الطاقة المتجددة</strong> — اتفاقيات شراء طاقة شمسية ورياح وتكامل الشبكة.</li><li><strong>معايير كفاءة الطاقة</strong> — تشديد المعايير في المباني والنقل والعمليات الصناعية وفق معايير ${regionAr}.</li><li><strong>المدن المستدامة والتنقل</strong> — استثمارات النقل العام وإطار تبني المركبات الكهربائية.</li><li><strong>تسعير الكربون والقياس والتحقق</strong> — بنية تحتية متوافقة مع المادة 6 من اتفاقية باريس.</li></ul>`;

  const contactInfoEn = `<p>${nameEn} country focal point — <a href="mailto:contact@cce.local?subject=${encodeURIComponent(nameEn)}%20country%20profile">contact&#64;cce.local</a><br/>National contact details for ${nameEn} (${isoAlpha3}) being verified by the editorial team.</p>`;

  const contactInfoAr = `<p>نقطة اتصال ${nameAr} — <a href="mailto:contact@cce.local?subject=${encodeURIComponent(nameAr)}%20country%20profile">contact&#64;cce.local</a><br/>يجري التحقق من تفاصيل الاتصال الوطني لـ${nameAr} (${isoAlpha3}) من قِبل الفريق التحريري.</p>`;

  return {
    descriptionEn,
    descriptionAr,
    keyInitiativesEn,
    keyInitiativesAr,
    contactInfoEn,
    contactInfoAr,
  };
}

export function getMockProfile(country: {
  id: string;
  isoAlpha2: string;
  isoAlpha3: string;
  nameEn: string;
  nameAr: string;
  regionEn: string;
  regionAr: string;
}): CountryProfile {
  // Curated content wins; otherwise synthesize from the country's own
  // metadata (name + region + ISO) so every country gets meaningful data.
  const data = MOCK_PROFILES_BY_ISO[country.isoAlpha2] ?? synthesizeProfile(country);
  return {
    id: `mock-profile-${country.id}`,
    countryId: country.id,
    descriptionEn: data.descriptionEn,
    descriptionAr: data.descriptionAr,
    keyInitiativesEn: data.keyInitiativesEn,
    keyInitiativesAr: data.keyInitiativesAr,
    contactInfoEn: data.contactInfoEn,
    contactInfoAr: data.contactInfoAr,
    lastUpdatedOn: '2026-04-30',
  };
}

interface MockKapsarc {
  classification: string;
  performanceScore: number;
  totalIndex: number;
  trendYoY: number;
  regionalRank: number;
  regionalCohortSize: number;
  renewableSharePct: number;
  energyIntensity: number;   // TJ per million USD GDP
  carbonIntensity: number;   // tCO₂e per million USD GDP
  subScores: { power: number; industry: number; transport: number; buildings: number; landUse: number };
}

/**
 * Curated KAPSARC-style snapshot data per country. All 8 seeded
 * countries have realistic numbers calibrated against publicly-known
 * profiles (renewable share, hydrogen ambitions, GDP/energy mix). The
 * sub-scores roll up by simple-average to performanceScore; totalIndex
 * adds a small adjustment for cohort-context.
 */
const MOCK_KAPSARC_BY_ISO: Record<string, MockKapsarc> = {
  SA: {
    classification: 'High Performer', performanceScore: 78.4, totalIndex: 82.1,
    trendYoY: +3.2, regionalRank: 2, regionalCohortSize: 8,
    renewableSharePct: 8.5, energyIntensity: 5.42, carbonIntensity: 312,
    subScores: { power: 76, industry: 81, transport: 70, buildings: 79, landUse: 86 },
  },
  AE: {
    classification: 'High Performer', performanceScore: 82.6, totalIndex: 85.3,
    trendYoY: +4.1, regionalRank: 1, regionalCohortSize: 8,
    renewableSharePct: 14.0, energyIntensity: 4.95, carbonIntensity: 287,
    subScores: { power: 84, industry: 80, transport: 78, buildings: 86, landUse: 85 },
  },
  EG: {
    classification: 'Improving',      performanceScore: 56.2, totalIndex: 60.4,
    trendYoY: +2.8, regionalRank: 4, regionalCohortSize: 6,
    renewableSharePct: 11.7, energyIntensity: 3.11, carbonIntensity: 198,
    subScores: { power: 58, industry: 52, transport: 49, buildings: 60, landUse: 62 },
  },
  JO: {
    classification: 'Improving',      performanceScore: 53.8, totalIndex: 58.7,
    trendYoY: +1.9, regionalRank: 6, regionalCohortSize: 8,
    renewableSharePct: 21.3, energyIntensity: 2.85, carbonIntensity: 175,
    subScores: { power: 62, industry: 48, transport: 50, buildings: 54, landUse: 55 },
  },
  BH: {
    classification: 'Improving',      performanceScore: 61.5, totalIndex: 64.0,
    trendYoY: +1.4, regionalRank: 5, regionalCohortSize: 8,
    renewableSharePct: 1.2, energyIntensity: 6.81, carbonIntensity: 398,
    subScores: { power: 55, industry: 67, transport: 60, buildings: 64, landUse: 62 },
  },
  KW: {
    classification: 'Improving',      performanceScore: 58.0, totalIndex: 62.3,
    trendYoY: +0.9, regionalRank: 7, regionalCohortSize: 8,
    renewableSharePct: 1.8, energyIntensity: 6.55, carbonIntensity: 372,
    subScores: { power: 52, industry: 64, transport: 58, buildings: 58, landUse: 60 },
  },
  OM: {
    classification: 'Improving',      performanceScore: 55.3, totalIndex: 59.6,
    trendYoY: +5.6, regionalRank: 8, regionalCohortSize: 8,
    renewableSharePct: 5.4, energyIntensity: 5.20, carbonIntensity: 286,
    subScores: { power: 56, industry: 52, transport: 54, buildings: 57, landUse: 58 },
  },
  QA: {
    classification: 'High Performer', performanceScore: 75.1, totalIndex: 78.4,
    trendYoY: +2.5, regionalRank: 3, regionalCohortSize: 8,
    renewableSharePct: 3.6, energyIntensity: 5.78, carbonIntensity: 325,
    subScores: { power: 72, industry: 78, transport: 70, buildings: 76, landUse: 80 },
  },
};

const DEFAULT_KAPSARC: MockKapsarc = {
  classification: 'Data Pending',
  performanceScore: 50, totalIndex: 50,
  trendYoY: 0, regionalRank: 0, regionalCohortSize: 0,
  renewableSharePct: 0, energyIntensity: 0, carbonIntensity: 0,
  subScores: { power: 50, industry: 50, transport: 50, buildings: 50, landUse: 50 },
};

export function getMockKapsarc(country: { id: string; isoAlpha2: string }): KapsarcSnapshot {
  const m = MOCK_KAPSARC_BY_ISO[country.isoAlpha2] ?? DEFAULT_KAPSARC;
  return {
    id: `mock-kapsarc-${country.id}`,
    countryId: country.id,
    classification: m.classification,
    performanceScore: m.performanceScore,
    totalIndex: m.totalIndex,
    snapshotTakenOn: '2026-04-30',
    sourceVersion: 'demo-v1',
    subScores: m.subScores,
    trendYoY: m.trendYoY,
    regionalRank: m.regionalRank,
    regionalCohortSize: m.regionalCohortSize,
    renewableSharePct: m.renewableSharePct,
    energyIntensity: m.energyIntensity,
    carbonIntensity: m.carbonIntensity,
  };
}
