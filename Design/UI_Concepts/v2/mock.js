// Hand-built UI mockups for 블록냥 (no generated imagery).
// Each concept page sets window.T (its look) and calls render().

const BOARD = [
  [1, 0, 2, 2, 0, 0, 3, 4],
  [1, 0, 0, 2, 0, 5, 3, 0],
  [0, 0, 0, 0, 0, 5, 0, 0],
  [4, 4, 0, 0, 0, 0, 0, 1],
  [0, 3, 0, 5, 5, 0, 0, 1],
  [0, 3, 0, 0, 5, 0, 2, 2],
  [2, 2, 0, 4, 4, 4, 0, 3],
  [2, 0, 0, 4, 0, 1, 1, 3],
];

// tray: L (3 tall + foot), 2x2, straight 3
const PIECES = [
  { color: 3, cells: ['X.', 'X.', 'XX'] },
  { color: 5, cells: ['XX', 'XX'] },
  { color: 1, cells: ['XXX'] },
];

// "블록냥" lettered on a 9x12 block grid
const LOGO = {
  '블': ['.X.....X.', '.XXXXXXX.', '.X.....X.', '.XXXXXXX.', '.........', 'XXXXXXXXX', '.........', '.XXXXXXX.', '.......X.', '.XXXXXXX.', '.X.......', '.XXXXXXX.'],
  '록': ['.XXXXXXX.', '.......X.', '.XXXXXXX.', '.X.......', '.XXXXXXX.', '....X....', 'XXXXXXXXX', '.........', '.XXXXXXX.', '.......X.', '.......X.', '.......X.'],
  '냥': ['X.....X..', 'X.....XXX', 'X.....X..', 'XXXXX.XXX', '......X..', '.........', '.........', '..XXXXX..', '.X.....X.', '.X.....X.', '.X.....X.', '..XXXXX..'],
};

const ICONS = {
  gear: '<circle cx="12" cy="12" r="6.6"/><circle cx="12" cy="12" r="2.4"/><path d="M12 2.6v2.6M12 18.8v2.6M2.6 12h2.6M18.8 12h2.6M5.4 5.4l1.8 1.8M16.8 16.8l1.8 1.8M5.4 18.6l1.8-1.8M16.8 7.2l1.8-1.8" stroke-width="2.4"/>',
  plus: '<path d="M12 6.5v11M6.5 12h11" stroke-width="2.8"/>',
  pause: '<path d="M9 6.5v11M15 6.5v11" stroke-width="3"/>',
  crown: '<path class="f" d="M3.6 8.6l4.3 3.5L12 5.2l4.1 6.9 4.3-3.5-1.7 9.4H5.3z"/>',
  flag: '<path d="M6.5 21V4" stroke-width="2.4"/><path class="f" d="M6.5 4.4h11l-2.6 3.9 2.6 3.9h-11z"/>',
  calendar: '<rect x="4" y="5.5" width="16" height="14.5" rx="2.6"/><path d="M4 10.2h16M8.5 3.4v4.2M15.5 3.4v4.2"/>',
  trophy: '<path d="M8 4.4h8v4.8a4 4 0 0 1-8 0z"/><path d="M8 6.4H5.4a2.6 2.6 0 0 0 2.8 3.6M16 6.4h2.6a2.6 2.6 0 0 1-2.8 3.6M12 13.2v3.4M8.4 20.2h7.2M9.6 16.6h4.8"/>',
  gift: '<rect x="4.6" y="10" width="14.8" height="10" rx="1.6"/><path d="M3.6 7.4h16.8v2.6H3.6zM12 7.4V20"/><path d="M12 7.4C10.6 4.2 7.1 4.1 7.3 6.1c.2 1.4 2.4 1.3 4.7 1.3M12 7.4c1.4-3.2 4.9-3.3 4.7-1.3-.2 1.4-2.4 1.3-4.7 1.3"/>',
  mission: '<rect x="5" y="5" width="14" height="16" rx="2.6"/><path d="M9 3.8h6v3.2H9z"/><path d="M8.6 11.6l1.6 1.6 3.2-3.2M8.6 16.6h6.8"/>',
  wheel: '<circle cx="12" cy="12" r="8.4"/><path d="M12 3.6v16.8M3.6 12h16.8M6.1 6.1l11.8 11.8M17.9 6.1L6.1 17.9"/><circle class="f" cx="12" cy="12" r="2.2"/>',
  shop: '<path d="M4.2 9.6l1.6-4.6h12.4l1.6 4.6"/><path d="M4.2 9.6c0 1.6 1.2 2.6 2.6 2.6s2.6-1 2.6-2.6c0 1.6 1.2 2.6 2.6 2.6s2.6-1 2.6-2.6c0 1.6 1.2 2.6 2.6 2.6s2.6-1 2.6-2.6"/><path d="M5.6 12.2v7.8h12.8v-7.8M10 20v-4.4h4V20"/>',
  paw: '<ellipse class="f" cx="12" cy="15.6" rx="4.6" ry="3.9"/><circle class="f" cx="6.3" cy="10.7" r="2.1"/><circle class="f" cx="9.7" cy="6.9" r="2.1"/><circle class="f" cx="14.3" cy="6.9" r="2.1"/><circle class="f" cx="17.7" cy="10.7" r="2.1"/>',
  hammer: '<path d="M14.6 9.4L5 19" stroke-width="2.8"/><rect class="f" x="11" y="3.4" width="11" height="6" rx="1.4" transform="rotate(45 16.5 6.4)"/>',
  refresh: '<path d="M19.4 12a7.4 7.4 0 0 1-12.8 5.1M4.6 12a7.4 7.4 0 0 1 12.8-5.1"/><path d="M17.8 3.6v3.7h-3.7M6.2 20.4v-3.7h3.7"/>',
  rotate: '<path d="M19.4 12A7.4 7.4 0 1 1 17 6.5"/><path d="M19.6 3.6v4.3h-4.3"/>',
  tail: '<path d="M3.6 15.4c3.6 0 5.4-8 9.9-8 3 0 3.6 3.2 6.6 2" stroke-width="2.6"/><path d="M3.4 8.8h3.2M3.4 12h2.2"/>',
};

function icon(name, size = 24, cls = '') {
  return `<svg class="ic ${cls}" viewBox="0 0 24 24" width="${size}" height="${size}">${ICONS[name]}</svg>`;
}

function blocksIcon(size = 26) {
  return `<svg class="ic blocks" viewBox="0 0 24 24" width="${size}" height="${size}">
    <rect class="b1" x="3" y="3" width="8.2" height="8.2" rx="1.8"/><rect class="b2" x="12.8" y="3" width="8.2" height="8.2" rx="1.8"/>
    <rect class="b3" x="3" y="12.8" width="8.2" height="8.2" rx="1.8"/><rect class="b4" x="12.8" y="12.8" width="8.2" height="8.2" rx="1.8"/></svg>`;
}

function statusBar() {
  return `<div class="statusbar"><span>9:41</span><span class="sb-icons">
    <svg width="18" height="12" viewBox="0 0 18 12"><rect x="0" y="8" width="3" height="4" rx=".8"/><rect x="5" y="5.5" width="3" height="6.5" rx=".8"/><rect x="10" y="3" width="3" height="9" rx=".8"/><rect x="15" y="0" width="3" height="12" rx=".8"/></svg>
    <svg width="16" height="12" viewBox="0 0 16 12"><path d="M8 11.2l2.4-2.6a3.4 3.4 0 0 0-4.8 0z"/><path d="M3.6 6.4a6.2 6.2 0 0 1 8.8 0l-1.4 1.5a4.2 4.2 0 0 0-6 0z"/><path d="M1 3.6a9.8 9.8 0 0 1 14 0l-1.4 1.5a7.8 7.8 0 0 0-11.2 0z"/></svg>
    <svg width="26" height="12" viewBox="0 0 26 12"><rect x=".6" y=".6" width="22" height="10.8" rx="3" fill="none" stroke-width="1.2"/><rect x="2.4" y="2.4" width="16" height="7.2" rx="1.6"/><rect x="23.6" y="4" width="1.8" height="4" rx=".8"/></svg>
  </span></div>`;
}

function logoSVG(o) {
  const pitch = o.unit + o.gap;
  const sylW = 9 * pitch - o.gap, sylH = 12 * pitch - o.gap, space = Math.round(pitch * 1.6);
  const W = 3 * sylW + 2 * space, H = sylH, p = o.pad || 0;
  let out = '';
  ['블', '록', '냥'].forEach((s, si) => {
    const ox = si * (sylW + space);
    LOGO[s].forEach((row, r) => [...row].forEach((ch, c) => {
      if (ch === 'X') out += o.unitFn(ox + c * pitch, r * pitch, o.unit, si, r, c);
    }));
  });
  return `<svg width="${W + 2 * p}" height="${H + 2 * p}" viewBox="${-p} ${-p} ${W + 2 * p} ${H + 2 * p}">${o.defs || ''}${o.wrap ? o.wrap(out) : out}</svg>`;
}

// ------------------------------------------------------------------ the cat (one drawing, three renderings)
const CAT = {
  tail: 'M150 172 C 186 166, 192 128, 170 110',
  body: 'M56 194 C 50 150, 64 124, 100 124 C 136 124, 150 150, 144 194 Z',
  belly: 'M80 194 C 79 168, 87 153, 100 153 C 113 153, 121 168, 120 194 Z',
  earL: 'M47 74 L56 18 L97 46 Z', earR: 'M153 74 L144 18 L103 46 Z',
  earInL: 'M60 60 L64 32 L84 47 Z', earInR: 'M140 60 L136 32 L116 47 Z',
  stripes: 'M100 37 v13 M87 39 l3 11 M113 39 l-3 11',
  nose: 'M95 99 h10 l-5 6 z',
  mouth: 'M100 105 q-5 7 -11 3 M100 105 q5 7 11 3',
  whiskers: 'M57 101 h-22 M59 109 l-20 6 M143 101 h22 M141 109 l20 6',
};
const ell = (cx, cy, rx, ry, a) => `<ellipse cx="${cx}" cy="${cy}" rx="${rx}" ry="${ry}" ${a}/>`;

function catFlat(c) {
  return `<g>
    ${ell(100, 196, 62, 6, `fill="${c.shadow}"`)}
    <path d="${CAT.tail}" stroke="${c.fur}" stroke-width="15" fill="none" stroke-linecap="round"/>
    <path d="${CAT.body}" fill="${c.fur}"/><path d="${CAT.belly}" fill="${c.cream}"/>
    ${ell(82, 194, 13, 7.5, `fill="${c.cream}"`)}${ell(118, 194, 13, 7.5, `fill="${c.cream}"`)}
    <path d="${CAT.earL}" fill="${c.fur}" stroke="${c.fur}" stroke-width="8" stroke-linejoin="round"/>
    <path d="${CAT.earR}" fill="${c.fur}" stroke="${c.fur}" stroke-width="8" stroke-linejoin="round"/>
    <path d="${CAT.earInL}" fill="${c.pink}"/><path d="${CAT.earInR}" fill="${c.pink}"/>
    ${ell(100, 86, 60, 50, `fill="${c.fur}"`)}
    <path d="${CAT.stripes}" stroke="${c.stripe}" stroke-width="5" stroke-linecap="round"/>
    ${ell(100, 106, 25, 17, `fill="${c.cream}"`)}
    ${ell(67, 104, 7, 4, `fill="${c.pink}" opacity=".9"`)}${ell(133, 104, 7, 4, `fill="${c.pink}" opacity=".9"`)}
    ${ell(78, 88, 5.5, 7.5, `fill="${c.ink}"`)}${ell(122, 88, 5.5, 7.5, `fill="${c.ink}"`)}
    <path d="${CAT.nose}" fill="${c.nose}" stroke="${c.nose}" stroke-width="2" stroke-linejoin="round"/>
    <path d="${CAT.mouth}" stroke="${c.ink}" stroke-width="2.6" fill="none" stroke-linecap="round"/>
    <path d="${CAT.whiskers}" stroke="${c.ink}" stroke-width="2" stroke-linecap="round" opacity=".7"/>
  </g>`;
}

// riso: flat ink shapes, details printed in black slightly out of register, halftone on the fur
function catRiso(c) {
  return `<g>
    <defs><pattern id="ht" width="5" height="5" patternUnits="userSpaceOnUse"><circle cx="2.5" cy="2.5" r="1.15" fill="${c.ink}" opacity=".28"/></pattern></defs>
    <g style="mix-blend-mode:multiply">
      <path d="${CAT.tail}" stroke="${c.fur}" stroke-width="15" fill="none" stroke-linecap="round"/>
      <path d="${CAT.body}" fill="${c.fur}"/>
      <path d="${CAT.earL}" fill="${c.fur}" stroke="${c.fur}" stroke-width="8" stroke-linejoin="round"/>
      <path d="${CAT.earR}" fill="${c.fur}" stroke="${c.fur}" stroke-width="8" stroke-linejoin="round"/>
      ${ell(100, 86, 60, 50, `fill="${c.fur}"`)}
      <path d="${CAT.body}" fill="url(#ht)"/>${ell(100, 86, 60, 50, 'fill="url(#ht)"')}
      <path d="${CAT.belly}" fill="${c.paper}"/>${ell(100, 106, 25, 17, `fill="${c.paper}"`)}
      ${ell(82, 194, 13, 7.5, `fill="${c.paper}"`)}${ell(118, 194, 13, 7.5, `fill="${c.paper}"`)}
      <path d="${CAT.earInL}" fill="${c.pink}"/><path d="${CAT.earInR}" fill="${c.pink}"/>
      ${ell(67, 104, 7, 4, `fill="${c.pink}"`)}${ell(133, 104, 7, 4, `fill="${c.pink}"`)}
      <path d="${CAT.nose}" fill="${c.pink}" stroke="${c.pink}" stroke-width="2" stroke-linejoin="round"/>
    </g>
    <g transform="translate(1.6 1.1)" style="mix-blend-mode:multiply">
      <path d="${CAT.stripes}" stroke="${c.ink}" stroke-width="5" stroke-linecap="round"/>
      ${ell(78, 88, 5.5, 7.5, `fill="${c.ink}"`)}${ell(122, 88, 5.5, 7.5, `fill="${c.ink}"`)}
      <path d="${CAT.mouth}" stroke="${c.ink}" stroke-width="2.6" fill="none" stroke-linecap="round"/>
      <path d="${CAT.whiskers}" stroke="${c.ink}" stroke-width="2" stroke-linecap="round"/>
      <path d="M58 194 h84" stroke="${c.ink}" stroke-width="2.4" stroke-linecap="round"/>
    </g>
  </g>`;
}

// doodle: ballpoint outlines with a shaky line, highlighter colored slightly outside the lines
function catDoodle(c) {
  const hl = `style="mix-blend-mode:multiply" opacity=".85"`;
  return `<g>
    <g filter="url(#rough)" stroke="${c.ink}" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round">
      <path d="${CAT.tail}" stroke-width="15" fill="none"/><path d="${CAT.tail}" stroke="${c.paper}" stroke-width="10" fill="none"/>
      <path d="${CAT.body}" fill="${c.paper}"/>
      <path d="${CAT.earL}" fill="${c.paper}"/><path d="${CAT.earR}" fill="${c.paper}"/>
      ${ell(100, 86, 60, 50, `fill="${c.paper}"`)}
      <path d="${CAT.belly}" fill="none" stroke-width="2"/>
      <path d="M68 194 q0-8 14-8 q14 0 14 8 M104 194 q0-8 14-8 q14 0 14 8" fill="none" stroke-width="2.2"/>
      <path d="${CAT.earInL}" fill="none" stroke-width="2"/><path d="${CAT.earInR}" fill="none" stroke-width="2"/>
      <path d="${CAT.stripes}" stroke-width="3"/>
      <path d="M72 88 q6 -8 12 0 M116 88 q6 -8 12 0" fill="none" stroke-width="3"/>
      <path d="${CAT.nose}" fill="${c.ink}" stroke-width="1.6"/>
      <path d="${CAT.mouth}" fill="none" stroke-width="2.4"/>
      <path d="${CAT.whiskers}" stroke-width="1.8"/>
    </g>
    <g ${hl}>
      <path d="${CAT.tail}" stroke="${c.fur}" stroke-width="11" fill="none" stroke-linecap="round" transform="translate(4 3)"/>
      <path d="${CAT.body}" fill="${c.fur}" transform="translate(5 4)"/>
      ${ell(105, 90, 58, 48, `fill="${c.fur}"`)}
      <path d="${CAT.earL}" fill="${c.fur}" transform="translate(4 4)"/><path d="${CAT.earR}" fill="${c.fur}" transform="translate(4 4)"/>
      ${ell(70, 107, 8, 5, `fill="${c.pink}"`)}${ell(136, 107, 8, 5, `fill="${c.pink}"`)}
    </g>
  </g>`;
}

function catSVG(size, headOnly) {
  const vb = headOnly ? '30 14 140 132' : '0 0 200 204';
  const art = T.cat === 'riso' ? catRiso(T.catColors) : T.cat === 'doodle' ? catDoodle(T.catColors) : catFlat(T.catColors);
  return `<svg width="${size}" height="${headOnly ? size : size * 1.02}" viewBox="${vb}">${art}</svg>`;
}

// ------------------------------------------------------------------ screens

function cells(rows, cls = '') {
  return rows.map(r => r.map(v => `<div class="cell c${v} ${cls}"></div>`).join('')).join('');
}

function pieceHTML(p) {
  const grid = p.cells.map(r => [...r].map(ch => (ch === 'X' ? p.color : 0)));
  return `<div class="piece" style="grid-template-columns:repeat(${grid[0].length},24px)">${grid.map(r => r.map(v => `<div class="cell ${v ? 'c' + v : 'ghost'}"></div>`).join('')).join('')}</div>`;
}

function partnerRing() {
  const r = 33, circ = 2 * Math.PI * r, fill = 0.62;
  return `<div class="partner">
    <svg class="ring" width="82" height="82" viewBox="0 0 82 82">
      <circle class="track" cx="41" cy="41" r="${r}"/>
      <circle class="prog" cx="41" cy="41" r="${r}" stroke-dasharray="${(circ * fill).toFixed(1)} ${circ.toFixed(1)}" transform="rotate(-90 41 41)"/>
    </svg>
    <div class="face">${catSVG(58, true)}</div>
    <div class="skill">${icon('tail', 15)}</div>
    <div class="count">5/8</div>
  </div>`;
}

function homeScreen() {
  const menu = [['gift', '출석', 1], ['mission', '미션', 0], ['wheel', '룰렛', 1], ['shop', '상점', 0], ['paw', '냥이', 1]];
  return `<div class="screen s1 home">
    ${T.backdrop || ''}
    ${statusBar()}
    <div class="topbar">
      <div class="iconbtn">${icon('gear', 24)}</div>
      <div class="coins"><span class="coin"></span><span>300</span><span class="plus">${icon('plus', 16)}</span></div>
    </div>
    <div class="logo">${T.logo()}</div>
    <div class="tagline">${T.tagline || ''}</div>
    <div class="cat">${catSVG(200)}</div>
    <div class="caption"><b>치즈</b> Lv.2 · 꼬리 휩쓸기</div>
    <div class="btn primary">${blocksIcon(26)}<span>클래식</span></div>
    <div class="btn adv">${icon('flag', 22)}<span>모험 · 레벨 5</span></div>
    <div class="row2">
      <div class="btn half daily">${icon('calendar', 20)}<span>오늘의 도전</span><i class="dot"></i></div>
      <div class="btn half duel">${icon('trophy', 20)}<span>친구 대결</span></div>
    </div>
    <div class="menu">${menu.map(([ic, label, dot]) => `<div class="item"><div class="tile">${icon(ic, 24)}</div><span>${label}</span>${dot ? '<i class="dot"></i>' : ''}</div>`).join('')}</div>
    <div class="ad"><span class="adtag">AD</span><span class="adtext">광고 배너 영역 · 320×50</span></div>
    <div class="homebar"></div>
  </div>`;
}

function gameScreen() {
  return `<div class="screen s2 game">
    ${T.backdrop || ''}
    ${statusBar()}
    <div class="pause iconbtn">${icon('pause', 22)}</div>
    <div class="best">${icon('crown', 18)}<span>55,297</span></div>
    <div class="score">12,480</div>
    <div class="combo"><span>콤보 4</span></div>
    ${partnerRing()}
    <div class="board">${T.boardUnder || ''}${cells(BOARD)}</div>
    <div class="tray">${PIECES.map(p => `<div class="slot">${pieceHTML(p)}</div>`).join('')}</div>
    <div class="boosters">${['hammer', 'refresh', 'rotate'].map(n => `<div class="boost">${icon(n, 26)}<span class="count">2</span></div>`).join('')}</div>
    <div class="ad"><span class="adtag">AD</span><span class="adtext">광고 배너 영역 · 320×50</span></div>
    <div class="homebar"></div>
  </div>`;
}

function render() {
  document.body.innerHTML = `${T.defs || ''}<div class="sheet">
    <h1>${T.title}</h1><p class="desc">${T.desc}</p>
    ${homeScreen()}${gameScreen()}
  </div>`;
}
