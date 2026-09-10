// Kiểm tool xếp màn: round-trip format game, kiểm dữ liệu blocker, luật blocker JS.
// Chạy: node demo/tool-check.mjs
// Như check.mjs: không copy logic — tách thẳng khối PHẦN 1..3b trong wordstack.html
// ra để kiểm đúng code đang chạy trong trình duyệt.
import { readFileSync } from 'node:fs';

const html = readFileSync(new URL('./wordstack.html', import.meta.url), 'utf8').replace(/\r\n/g, '\n');
const BAR = '/* ==========================================================================\n   PHẦN ';
const a = html.indexOf(BAR + '1'), b = html.indexOf(BAR + '4');
if (a < 0 || b < 0) throw new Error('không thấy marker PHẦN 1 / PHẦN 4 trong wordstack.html');

const T = new Function('"use strict";\n' + html.slice(a, b) +
  '\nreturn { parseLevel, applyMove, checkStatus, stateKey, solve, rawToGame, gameToRaw, gameJson, cloneState };')();

let failed = 0;
const ok = (cond, msg) => { if (!cond) { failed++; console.error('FAIL —', msg); } };
const LEVELS = new URL('../Assets/_Game/Content/Levels/', import.meta.url);
const loadGame = f => JSON.parse(readFileSync(new URL(f, LEVELS), 'utf8'));
const roundTrip = gv => { const r = T.rawToGame(T.gameToRaw(gv), null); return r.errors ? r : JSON.parse(r.json); };

// ---- 1. Round-trip màn ship: không mất gì game dùng ----
{
  const rel = o => JSON.stringify(Object.fromEntries(o.meaning.groups.map(g => [g.id, g.group || null])));
  const cnt = o => o.meaning.groups.map(g => g.id + ':' + g.cards.length).sort().join(',');
  for (const f of ['lv-001.json', 'lv-002.json', 'lv-008.json']){
    const orig = loadGame(f), back = roundTrip(orig);
    ok(!back.errors, `${f}: xuất lại bị chặn — ${back.errors && back.errors[0]}`);
    if (back.errors) continue;
    ok(orig.moves === back.moves, `${f}: moves ${orig.moves} → ${back.moves}`);
    ok(orig.difficulty === back.difficulty, `${f}: difficulty ${orig.difficulty} → ${back.difficulty}`);
    ok(rel(orig) === rel(back), `${f}: quan hệ collapse đổi`);
    ok(cnt(orig) === cnt(back), `${f}: số thẻ mỗi nhóm đổi`);
  }
}

// ---- 2. Round-trip blocker ----
// Fixture hợp lệ cả với luật kiểm ở Task 4: thẻ chìa là thẻ đầu nhóm đầu; hộp có ổ nằm
// ở một stack KHÔNG chứa thẻ nào của nhóm đó (spec luật 6) và hộp trên CÓ thẻ — Task 4
// cần đổi chỗ một thẻ vào hộp này, hộp trống thì không có gì để đổi; hộp khoá ở stack khác.
// lv-001 hôm nay: nhóm đầu là g_dog (nhóm con, collapse vào g_pet), chọn trúng stack 1.
function blockerFixture(){
  const gv = loadGame('lv-001.json');
  const g0 = new Set(gv.meaning.groups[0].cards.map(c => c.id));
  const keyStack = gv.layout.stacks.findIndex(s =>
    s.boxes[0].slots.some(Boolean) && !s.boxes.some(bx => bx.slots.some(id => g0.has(id))));
  const lockStack = gv.layout.stacks.findIndex((s, i) => i !== keyStack);
  gv.meaning.groups[0].cards[0].blockers = { ice: 3, key: 'k1' };
  gv.layout.stacks[keyStack].boxes[0].blockers = { keylock: 'k1' };
  gv.layout.stacks[lockStack].boxes[0].blockers = { locked: 2 };
  return { gv, keyStack, lockStack };
}
{
  const { gv, keyStack } = blockerFixture();
  ok(keyStack >= 0, 'lv-001 phải có một stack không chứa thẻ nào của nhóm đầu');
  const back = roundTrip(gv);
  ok(!back.errors, 'màn có blocker xuất lại bị chặn: ' + (back.errors && back.errors[0]));
  if (!back.errors){
    ok(JSON.stringify(back.meaning.groups[0].cards[0].blockers) === '{"ice":3,"key":"k1"}',
       'blocker thẻ phải còn nguyên sau Nhập → Xuất');
    const boxBlk = back.layout.stacks.flatMap(s => s.boxes).filter(x => x.blockers)
      .map(x => JSON.stringify(x.blockers)).sort();
    ok(JSON.stringify(boxBlk) === JSON.stringify(['{"keylock":"k1"}', '{"locked":2}']),
       'blocker hộp phải còn nguyên sau Nhập → Xuất, được: ' + boxBlk.join(' '));
    ok(back.meaning.groups.slice(1).every(g => g.cards.every(c => !c.blockers)),
       'thẻ không khai blocker thì xuất ra không có "blockers"');
  }
}

// (Các mục 3..4 thêm ở Task 4, 5.)

if (failed) { console.error(`\n${failed} check FAIL`); process.exit(1); }
console.log('tool-check OK');
