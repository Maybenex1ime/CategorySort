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
  '\nreturn { parseLevel, applyMove, checkStatus, stateKey, solve, rawToGame, gameToRaw, gameJson, cloneState, isBoxOpen, canPick };')();

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

// ---- 4. Luật blocker JS — cùng kịch bản với SelfCheck mục 8 ----
{
  const G3 = [ { id:'ga', name:'A', words:['a1','a2','a3','a4'] },
               { id:'gb', name:'B', words:['b1','b2','b3','b4'] },
               { id:'gc', name:'C', words:['c1','c2','c3','c4'] } ];
  // Như RulesLv: s0 [a1 a2 · ·] trên [a3 a4 · ·] · s1 [b1 b2 b3 c1] · s2 [b4 c2] · s3 [c3 c4] · s4 trống
  const base = () => ({ id:'t', title:'t', boxCapacity:4, groups: structuredClone(G3), stacks: [
    { boxes:[ { tiles:['a1','a2',null,null] }, { tiles:['a3','a4',null,null] } ] },
    { boxes:[ { tiles:['b1','b2','b3','c1'] } ] },
    { boxes:[ { tiles:['b4','c2',null,null] } ] },
    { boxes:[ { tiles:['c3','c4',null,null] } ] },
    { boxes:[ { tiles:[null,null,null,null] } ] } ] });
  const load = raw => { const p = T.parseLevel(raw); if (p.errors.length) throw new Error(p.errors.join(' | ')); return p.state; };
  const tile = (st, label) => st.stacks.flatMap(s => s.boxes).flatMap(b => b.tiles).find(t => t && t.label === label);
  const mv = (st, from, label, to) => T.applyMove(st, st.stacks[from].id, tile(st, label).id, st.stacks[to].id, false);
  const step = (st, from, label, to, msg) => { const r = mv(st, from, label, to); ok(r.ok, msg + ' — ' + r.reason); return r.ok ? r.state : st; };

  // 8b. Nước đi
  { const raw = base(); raw.stacks[2].boxes[0].blockers = { locked: 1 }; const st = load(raw);
    ok(!mv(st, 2, 'b4', 0).ok, 'hộp khoá: không nhặt thẻ ra');
    ok(!mv(st, 0, 'a1', 2).ok, 'hộp khoá: không thả thẻ vào');
    ok(mv(st, 0, 'a1', 3).ok, 'hộp mở khác vẫn nhận thẻ'); }
  { const raw = base(); raw.cardBlockers = { a1: { ice: 2 } }; let st = load(raw);
    ok(!mv(st, 0, 'a1', 3).ok, 'thẻ băng không kéo đi được');
    st = step(st, 0, 'a2', 3, 'nước hợp lệ thứ nhất');
    ok(tile(st, 'a1').ice && tile(st, 'a1').ice.have === 1, 'sau một nước, băng đếm 1');
    st = step(st, 3, 'a2', 4, 'nước hợp lệ thứ hai');
    ok(!tile(st, 'a1').ice, 'đủ hai nước thì băng tan');
    ok(mv(st, 0, 'a1', 4).ok, 'tan rồi thì kéo được'); }
  { const raw = base(); raw.cardBlockers = { a3: { ice: 1 } }; let st = load(raw);
    st = step(st, 0, 'a1', 4, 'nước hợp lệ');
    ok(tile(st, 'a3').ice.have === 0, 'thẻ băng còn chìm thì không đếm'); }
  { const raw = base(); raw.cardBlockers = { a1: { ice: 1 } }; const st = load(raw);
    ok(!mv(st, 0, 'a2', 0).ok && tile(st, 'a1').ice.have === 0, 'nước bị từ chối không giảm băng'); }
  { const raw = base(); raw.stacks[2].boxes[0].blockers = { locked: 9 }; raw.cardBlockers = { b4: { ice: 1 } }; let st = load(raw);
    st = step(st, 0, 'a1', 4, 'nước hợp lệ');
    ok(!tile(st, 'b4').ice, 'băng trong hộp khoá ở trên cùng vẫn tan theo nước đi'); }
  { const raw = base(); [0, 1, 2, 3].forEach(i => raw.stacks[i].boxes[0].blockers = { locked: 99 });
    ok(load(raw).status === 'stuck', 'còn ô trống ở stack rỗng mà không thẻ nào nhặt được → kẹt'); }
  { const raw = base(); raw.cardBlockers = Object.fromEntries(['a1','a2','b1','b2','b3','c1','b4','c2','c3','c4'].map(w => [w, { ice: 99 }]));
    ok(load(raw).status === 'stuck', 'mọi thẻ lộ đều băng → kẹt thật, không thua ngầm'); }
  ok(load(base()).status === 'playing', 'bàn thường vẫn playing');

  // 8c. Dây chuyền
  { const raw = { id:'t', title:'t', boxCapacity:4, groups:[ G3[0], { id:'gd', name:'D', words:['d1','d2','d3','d4'] } ], stacks:[
      { boxes:[ { tiles:['a1','a2','a3',null] } ] }, { boxes:[ { tiles:['a4',null,null,null] } ] },
      { boxes:[ { tiles:['d1','d2','d3','d4'], blockers:{ locked:1 } } ] }, { boxes:[ { tiles:[null,null,null,null] } ] } ] };
    let st = load(raw);
    st = step(st, 1, 'a4', 3, 'nước không gom gì');
    ok(st.solved.size === 0, 'hộp khoá đủ bộ vẫn không nổ');
    st = step(st, 3, 'a4', 0, 'thẻ thứ 4 của A');
    ok(st.solved.size === 2, 'gom A → hộp khoá mở → D nổ ngay trong cùng dây chuyền'); }
  { const raw = base(); raw.stacks[0].boxes.unshift({ tiles:[null,null,null,null], blockers:{ locked:9 } }); let st = load(raw);
    st = step(st, 2, 'b4', 4, 'nước ở chỗ khác');
    ok(st.stacks[0].boxes.length === 3, 'hộp khoá rỗng không bị xoá'); }
  { const raw = { id:'t', title:'t', boxCapacity:4, groups:[ G3[0], G3[1] ], cardBlockers:{ a1:{ ice:2 } }, stacks:[
      { boxes:[ { tiles:['a1','a2','a3','a4'] } ] }, { boxes:[ { tiles:['b1',null,null,null] } ] },
      { boxes:[ { tiles:[null,null,null,null] } ] }, { boxes:[ { tiles:['b2','b3','b4',null] } ] } ] };
    let st = load(raw);
    st = step(st, 1, 'b1', 2, 'nước thứ nhất');
    ok(st.solved.size === 0, '3 thẻ + 1 băng cùng nhóm thì chưa nổ');
    st = step(st, 2, 'b1', 1, 'nước thứ hai');
    ok(st.solved.size === 1, 'băng tan sau nước đó → nhóm nổ ngay trong cùng nhịp'); }
  const keyRaw = cardBlk => ({ id:'t', title:'t', boxCapacity:4, groups:[ G3[0], G3[1] ], cardBlockers: cardBlk, stacks:[
      { boxes:[ { tiles:['a1','a2','a3',null] } ] }, { boxes:[ { tiles:['a4','b4',null,null] } ] },
      { boxes:[ { tiles:[null,null,null,null], blockers:{ keylock:'k1' } } ] }, { boxes:[ { tiles:['b1','b2','b3',null] } ] } ] });
  // Hộp có ổ ở stack 2 là hộp đáy nên không bao giờ bị xoá — đọc trạng thái mở của nó được
  // cả sau khi bàn đã sạch.
  const lockBox = st => st.stacks[2].boxes[0];
  { let st = load(keyRaw({ b4: { key:'k1' } }));
    ok(!mv(st, 0, 'a1', 2).ok, 'ổ đóng: không thả vào');
    st = step(st, 1, 'a4', 0, 'gom A');
    ok(st.solved.size === 1 && !T.isBoxOpen(st, lockBox(st)), 'nhóm không có chìa nổ thì ổ vẫn đóng');
    ok(!mv(st, 3, 'b1', 2).ok, 'ổ còn đóng thì vẫn không thả vào');
    st = step(st, 1, 'b4', 3, 'thẻ chìa kéo được như thẻ thường');
    ok(st.solved.size === 2 && T.isBoxOpen(st, lockBox(st)), 'thẻ chìa bị gom thì ổ mở'); }
  { let st = load(keyRaw({ b4: { key:'k1', ice:1 } }));
    ok(!mv(st, 1, 'b4', 3).ok, 'còn băng thì chìa chưa kéo được');
    st = step(st, 1, 'a4', 0, 'nước làm tan băng (b4 đang ở hộp trên)');
    st = step(st, 1, 'b4', 3, 'tan rồi kéo chìa');
    ok(st.solved.size === 2 && T.isBoxOpen(st, lockBox(st)), 'băng tan → chìa bị gom → ổ mở'); }

  // 8d. Bộ giải
  { const a = load(base()), rb = base(); rb.cardBlockers = { a1: { ice:3 } }; const b = load(rb);
    ok(T.stateKey(a) !== T.stateKey(b), 'băng phải vào mã trạng thái');
    const b2 = T.cloneState(b); const t0 = b2.stacks[0].boxes[0].tiles[0];
    b2.stacks[0].boxes[0].tiles[0] = { ...t0, ice: { need: 3, have: 1 } };
    ok(T.stateKey(b) !== T.stateKey(b2), 'hai mức băng là hai trạng thái');
    const rl = base(); rl.stacks[2].boxes[0].blockers = { locked:1 };
    ok(T.stateKey(a) === T.stateKey(load(rl)), 'khoá hộp không đổi mã — trạng thái suy từ bố cục'); }
  { const raw = base(); raw.stacks[2].boxes[0].blockers = { locked:1 }; raw.stacks[3].boxes[0].blockers = { keylock:'k1' };
    raw.cardBlockers = { c3: { ice:2 }, b4: { key:'k1' } };
    ok(T.solve(load(raw), 25000).solved, 'bàn có đủ ba blocker phải giải được'); }
  { const st = load(base()); st.stacks[2].boxes[0].lock = { kind:'key', keyId:'k1' }; tile(st, 'b4').key = 'k1';
    st.status = T.checkStatus(st);
    ok(!T.solve(st, 25000).solved, 'chìa nằm trong chính hộp nó mở → bộ giải phải báo không giải được'); }
}

if (failed) { console.error(`\n${failed} check FAIL`); process.exit(1); }
console.log('tool-check OK');
