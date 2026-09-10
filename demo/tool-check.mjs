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

// ---- 3. Kiểm dữ liệu blocker (spec Mục 5, sáu luật) ----
// Mỗi ca so đúng MỘT đoạn thông điệp, để không qua được vì một lỗi khác tình cờ xuất hiện.
{
  const rawOf = () => T.gameToRaw(blockerFixture().gv);
  const good = T.parseLevel(rawOf());
  ok(good.errors.length === 0, 'fixture hợp lệ phải qua kiểm: ' + good.errors.join(' | '));

  const broken = (mutate, expect, label) => {
    const r = rawOf(); mutate(r);
    const errs = T.parseLevel(r).errors;
    ok(errs.some(e => e.includes(expect)),
       `phải từ chối (${label}) với "${expect}", được: ${errs.join(' | ') || '(không lỗi)'}`);
  };
  const boxes = r => r.stacks.flatMap(s => s.boxes);
  const keyLabel = r => Object.keys(r.cardBlockers)[0];
  const keyBox = r => boxes(r).find(b => b.blockers && b.blockers.keylock);
  const lockBox = r => boxes(r).find(b => b.blockers && b.blockers.locked);
  const mateOf = r => {
    const g = r.groups.find(x => x.words.includes(keyLabel(r)));
    return g.words.find(w => w !== keyLabel(r) && boxes(r).some(b => b.tiles.includes(w)));
  };
  const swapInto = (r, label, dst) => {
    const src = boxes(r).find(b => b.tiles.includes(label));
    const i = src.tiles.indexOf(label), j = dst.tiles.findIndex(Boolean);
    [src.tiles[i], dst.tiles[j]] = [dst.tiles[j], src.tiles[i]];
  };

  // Luật 1 — id có trong sổ và đúng phía
  broken(r => r.cardBlockers[keyLabel(r)].frost = 1, 'không có trong sổ đăng ký', 'id lạ trên thẻ');
  broken(r => r.cardBlockers[keyLabel(r)].locked = 1, 'là blocker của hộp', 'id của hộp đặt trên thẻ');
  broken(r => boxes(r).find(b => !b.blockers).blockers = { ice: 1 }, 'là blocker của thẻ', 'id của thẻ đặt trên hộp');
  broken(r => boxes(r).find(b => !b.blockers).blockers = { chain: 1 }, 'không có trong sổ đăng ký', 'id lạ trên hộp');
  // Luật 2 — hộp tối đa một blocker
  broken(r => lockBox(r).blockers.keylock = 'k1', 'tối đa một blocker', 'hộp mang hai blocker');
  // Luật 3 — sổ hiện chỉ có một cặp (ice + key) và nó được phép: không có ca từ chối để kiểm.
  // Luật 4 — số đếm nguyên ≥ 1
  broken(r => r.cardBlockers[keyLabel(r)].ice = 0, 'ice phải là số nguyên', 'ice = 0');
  broken(r => r.cardBlockers[keyLabel(r)].ice = 1.5, 'ice phải là số nguyên', 'ice không nguyên');
  broken(r => lockBox(r).blockers.locked = '3', 'locked phải là số nguyên', 'locked là chuỗi');
  // Luật 5 — mỗi id chìa đúng một thẻ mang
  broken(r => keyBox(r).blockers.keylock = 'k9', 'không có thẻ nào mang chìa', 'keylock trỏ chìa không ai mang');
  broken(r => {
    const other = boxes(r).flatMap(b => b.tiles).find(t => t && t !== keyLabel(r));
    r.cardBlockers[other] = { key: 'k1' };
  }, 'có hai thẻ mang', 'hai thẻ cùng mang một chìa');
  // Luật 6 — thẻ chìa và mọi thẻ cùng nhóm không nằm trong hoặc dưới hộp chìa đó mở
  broken(r => swapInto(r, keyLabel(r), keyBox(r)), 'khoá vĩnh viễn', 'thẻ chìa nằm trong hộp nó mở');
  broken(r => swapInto(r, mateOf(r), keyBox(r)), 'khoá vĩnh viễn', 'thẻ cùng nhóm nằm trong hộp chìa mở');
  broken(r => {
    const mate = mateOf(r), src = boxes(r).find(b => b.tiles.includes(mate));
    src.tiles[src.tiles.indexOf(mate)] = null;
    r.stacks.find(s => s.boxes.includes(keyBox(r))).boxes.push({ tiles: [mate, null, null, null] });
  }, 'khoá vĩnh viễn', 'thẻ cùng nhóm nằm DƯỚI hộp chìa mở');
  // Ngoài phạm vi — blocker trên thẻ sinh ra từ COLLAPSE
  broken(r => {
    const child = r.groups.find(g => r.groups.some(o => o !== g && o.words.includes(g.name)));
    r.cardBlockers[child.name] = { ice: 1 };
  }, 'COLLAPSE', 'blocker trên thẻ sinh ra từ COLLAPSE');
}

// (Mục 4 thêm ở Task 5.)

if (failed) { console.error(`\n${failed} check FAIL`); process.exit(1); }
console.log('tool-check OK');
