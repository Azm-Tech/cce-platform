const fs = require('fs');
const path = require('path');

const LUCIDE_DIR = '/Users/ayman/work/Azm/cce-platform/frontend/node_modules/lucide-static/icons';
const OUT_FILE = '/Users/ayman/work/Azm/cce-platform/frontend/libs/ui-kit/src/lib/icons/cce-icons.ts';

const ICON_NAMES = [
  'arrow-right','arrow-left','circle-arrow-left','log-in','leaf','sprout',
  'arrow-down','astroid','sparkles','apple','book-marked','book-open',
  'book-open-check','messages-square','users','globe','earth','telescope',
  'calendar','calendar-heart','calendar-days','clock','map-pin','plus',
  'bookmark','reply','arrow-up-right','arrow-down-right','flag','image',
  'chevron-right','chevron-left','chevron-down','chevron-up',
  'file-search-corner','user-star','phone','mail','expand','search-x',
  'zoom-in','eye','list','house','ellipsis','ellipsis-vertical','lightbulb',
  'search','sliders-horizontal','share-2','share','download','calendar-plus',
  'thumbs-up','message-square-text','info','bot','send','shield-alert',
  'triangle-alert','funnel','arrow-down-up','cloud','menu','bell','bell-dot',
  'user','circle-user-round','newspaper','solar-panel','cloud-sun','pointer',
  'circle-check','check','file','arrow-big-up','arrow-big-down','languages',
  'recycle','repeat-2','flame','diamond-minus','zap','sun','car',
  'building-2','building','factory','shield-check','refresh-ccw','trash-2',
  'trash','calculator','cloud-off','star','rotate-ccw','timer',
  'laptop-minimal','user-round-check','audio-lines','ear','square-user-round',
  'utensils-crossed','briefcase-business','notebook-pen','pencil',
  'network','settings','log-out','lock-keyhole','user-round-plus','orbit',
  'atom','blend','bubbles','shapes','message-circle-question-mark',
  'x','external-link','activity','file-text','layout-list','settings-2',
  'file-sliders','user-cog','badge-check','circle-x','bookmark-x',
  'octagon-alert','octagon-x','ban','circle-slash-2','arrow-down-from-line',
  'book-copy','library-big','library','map','sticky-note-plus',
  'sticky-note-check','sticky-notes','sticky-note-x','sticky-note-minus',
  'sticky-note-off','file-plus','bookmark-plus','message-circle-plus',
  'image-plus','bookmark-check','bookmark-minus','square-plus',
  'calendar-sync','calendar-clock','link','grip-vertical','compass',
  'list-filter','eye-off','eye-closed','upload','message-square-plus',
  'message-square-check','copy','square-user','shield-user','file-user',
];

const LINKEDIN_SVG = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M0 12.279C0 11.6625 0.499774 11.1628 1.11628 11.1628H7.81393C8.43043 11.1628 8.93021 11.6625 8.93021 12.279V30.8836C8.93021 31.5001 8.43043 31.9999 7.81393 31.9999H1.11628C0.499774 31.9999 0 31.5001 0 30.8836V12.279ZM2.23255 13.3953V29.7674H6.69766V13.3953H2.23255Z"/><path fill-rule="evenodd" clip-rule="evenodd" d="M4.4651 2.23255C3.2321 2.23255 2.23255 3.2321 2.23255 4.4651C2.23255 5.69811 3.2321 6.69766 4.4651 6.69766C5.69811 6.69766 6.69766 5.69811 6.69766 4.4651C6.69766 3.2321 5.69811 2.23255 4.4651 2.23255ZM0 4.4651C0 1.9991 1.9991 0 4.4651 0C6.93111 0 8.93021 1.9991 8.93021 4.4651C8.93021 6.93111 6.93111 8.93021 4.4651 8.93021C1.9991 8.93021 0 6.93111 0 4.4651Z"/><path fill-rule="evenodd" clip-rule="evenodd" d="M11.1628 12.279C11.1628 11.6625 11.6625 11.1628 12.279 11.1628H18.9768C19.5862 11.1628 20.0815 11.6511 20.0929 12.2578C21.1981 11.5652 22.4931 11.1631 23.8835 11.1631C27.895 11.1631 32 14.5778 32 18.977L31.998 30.8838C31.9979 31.5003 31.4982 31.9999 30.8817 31.9999H24.1859C23.5694 31.9999 23.0696 31.5001 23.0696 30.8836V21.5816C23.0696 20.7597 22.4033 20.0933 21.5813 20.0933C20.7594 20.0933 20.0931 20.7597 20.0931 21.5816V30.8836C20.0931 31.5001 19.5933 31.9999 18.9768 31.9999H12.279C11.6625 31.9999 11.1628 31.5001 11.1628 30.8836V12.279ZM17.8605 13.3953H13.3953V29.7674H17.8605V21.5816C17.8605 19.5267 19.5264 17.8608 21.5813 17.8608C23.6363 17.8608 25.3022 19.5267 25.3022 21.5816V29.7674H29.7657L29.7674 18.977C29.7673 15.9784 26.8364 13.3957 23.8835 13.3957C22.2797 13.3957 20.8287 14.1798 19.8652 15.4463C19.5751 15.8276 19.0741 15.9813 18.6202 15.8283C18.1662 15.6752 17.8605 15.2496 17.8605 14.7705V13.3953Z"/></svg>`;

function minifySvg(svg) {
  return svg.replace(/\n\s*/g, ' ').replace(/\s{2,}/g, ' ').replace(/> </g, '><').trim();
}

const entries = [];
const missing = [];

for (const name of ICON_NAMES) {
  const filePath = path.join(LUCIDE_DIR, `${name}.svg`);
  if (fs.existsSync(filePath)) {
    const content = minifySvg(fs.readFileSync(filePath, 'utf8'));
    entries.push([name, content]);
  } else {
    missing.push(name);
  }
}

entries.push(['linkedin', LINKEDIN_SVG]);
entries.sort((a, b) => a[0].localeCompare(b[0]));

let ts = `// Auto-generated from lucide-static + custom CCE icons. Regenerate: node scripts/gen-cce-icons.cjs\n`;
ts += `export const CCE_ICONS: Readonly<Record<string, string>> = {\n`;

for (const [name, svg] of entries) {
  const escaped = svg.replace(/\\/g, '\\\\').replace(/`/g, '\\`').replace(/\$\{/g, '\\${');
  ts += `  '${name}': \`${escaped}\`,\n`;
}

ts += `};\n`;

fs.mkdirSync(path.dirname(OUT_FILE), { recursive: true });
fs.writeFileSync(OUT_FILE, ts);

console.log(`Generated ${entries.length} icons`);
if (missing.length) console.error('Missing:', missing);
