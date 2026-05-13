/**
 * Mock translation entries for the demo / when the backend doesn't yet
 * expose the `/api/admin/translations` endpoint. Real API responses
 * always win — this only fills the page when the API is unavailable.
 */
export interface TranslationEntry {
  key: string;
  en: string;
  ar: string;
  scope: 'common' | 'auth' | 'nav' | 'home' | 'resources' | 'events' | 'errors';
  updatedAt: string;
}

export const MOCK_TRANSLATIONS: TranslationEntry[] = [
  // common
  { key: 'common.actions.save', en: 'Save', ar: 'حفظ', scope: 'common', updatedAt: '2026-04-30' },
  { key: 'common.actions.cancel', en: 'Cancel', ar: 'إلغاء', scope: 'common', updatedAt: '2026-04-30' },
  { key: 'common.actions.delete', en: 'Delete', ar: 'حذف', scope: 'common', updatedAt: '2026-04-30' },
  { key: 'common.actions.edit', en: 'Edit', ar: 'تعديل', scope: 'common', updatedAt: '2026-04-30' },
  { key: 'common.actions.signIn', en: 'Sign in', ar: 'تسجيل الدخول', scope: 'common', updatedAt: '2026-04-30' },
  { key: 'common.actions.signOut', en: 'Sign out', ar: 'تسجيل الخروج', scope: 'common', updatedAt: '2026-04-30' },
  // auth
  { key: 'auth.welcome', en: 'Welcome to CCE', ar: 'مرحباً بك في CCE', scope: 'auth', updatedAt: '2026-05-04' },
  { key: 'auth.signInPrompt', en: 'Sign in to continue', ar: 'سجّل الدخول للمتابعة', scope: 'auth', updatedAt: '2026-05-04' },
  // nav
  { key: 'nav.users', en: 'Users', ar: 'المستخدمون', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.experts', en: 'Experts', ar: 'الخبراء', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.resources', en: 'Resources', ar: 'الموارد', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.news', en: 'News', ar: 'الأخبار', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.events', en: 'Events', ar: 'الفعاليات', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.pages', en: 'Pages', ar: 'الصفحات', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.taxonomies', en: 'Taxonomies', ar: 'التصنيفات', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.communityModeration', en: 'Community moderation', ar: 'الإشراف على المجتمع', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.countries', en: 'Countries', ar: 'الدول', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.notifications', en: 'Notifications', ar: 'الإشعارات', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.reports', en: 'Reports', ar: 'التقارير', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.audit', en: 'Audit log', ar: 'سجل التدقيق', scope: 'nav', updatedAt: '2026-04-30' },
  { key: 'nav.translations', en: 'Translations', ar: 'الترجمات', scope: 'nav', updatedAt: '2026-05-06' },
  { key: 'nav.settings', en: 'Settings', ar: 'الإعدادات', scope: 'nav', updatedAt: '2026-05-06' },
  // home
  { key: 'home.hero.title1', en: 'Knowledge for', ar: 'المعرفة من أجل', scope: 'home', updatedAt: '2026-04-30' },
  { key: 'home.hero.title2', en: 'a Carbon Circular Economy', ar: 'اقتصاد دائري منخفض الكربون', scope: 'home', updatedAt: '2026-04-30' },
  { key: 'home.hero.subtitle', en: 'Explore curated insights, build scenarios, and connect with experts shaping a sustainable future.', ar: 'استكشف رؤى منسّقة، وابنِ سيناريوهات، وتواصل مع الخبراء الذين يصنعون مستقبلاً مستداماً.', scope: 'home', updatedAt: '2026-04-30' },
  // resources
  { key: 'resources.title', en: 'Knowledge Center', ar: 'مركز المعرفة', scope: 'resources', updatedAt: '2026-04-30' },
  { key: 'resources.empty', en: 'No resources match your filters.', ar: 'لا توجد موارد تطابق عوامل التصفية.', scope: 'resources', updatedAt: '2026-04-30' },
  { key: 'resources.filter.country', en: 'Country', ar: 'الدولة', scope: 'resources', updatedAt: '2026-04-30' },
  { key: 'resources.filter.resourceType', en: 'Type', ar: 'النوع', scope: 'resources', updatedAt: '2026-04-30' },
  // events
  { key: 'events.title', en: 'Events', ar: 'الفعاليات', scope: 'events', updatedAt: '2026-04-30' },
  { key: 'events.upcoming', en: 'Upcoming', ar: 'القادمة', scope: 'events', updatedAt: '2026-04-30' },
  { key: 'events.past', en: 'Past', ar: 'السابقة', scope: 'events', updatedAt: '2026-04-30' },
  // errors
  { key: 'errors.network', en: 'Connection lost. Please retry.', ar: 'انقطع الاتصال. يرجى المحاولة مرة أخرى.', scope: 'errors', updatedAt: '2026-04-30' },
  { key: 'errors.unauthorized', en: 'Please sign in to continue.', ar: 'يرجى تسجيل الدخول للمتابعة.', scope: 'errors', updatedAt: '2026-04-30' },
  { key: 'errors.notFound', en: 'The item you requested was not found.', ar: 'العنصر الذي طلبته غير موجود.', scope: 'errors', updatedAt: '2026-04-30' },
  { key: 'errors.serverError', en: 'Something went wrong on our end. Please try again.', ar: 'حدث خطأ في جانبنا. يرجى المحاولة مرة أخرى.', scope: 'errors', updatedAt: '2026-04-30' },
];
