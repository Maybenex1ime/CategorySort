// Kiểm luật + xác minh mọi level giải được. Chạy: node demo/check.mjs
// Không copy logic — trích thẳng phần engine trong file demo ra để test đúng code đang chạy.
import { readFileSync } from 'node:fs';

const html = readFileSync(new URL('./wordstack-clear-demo.html', import.meta.url), 'utf8');

const LEVELS = JSON.parse(
  html.match(/<script id="levels" type="application\/json">([\s\S]*?)<\/script>/)[1]);

const engineSrc = html.slice(
  html.indexOf('// ── Hằng'),
  html.indexOf('// ── Render'));
const engine = new Function(engineSrc + '\nreturn {BOX_CAPACITY,GROUP_SIZE,ART,REMOVE_EMPTY_NONBOTTOM_BOX,' +
  'validate,build,topBox,freeCount,firstFree,isEmpty,moveTile,findCompletedGroup,' +
  'findRemovableBox,totalTiles,checkStatus,boxColors};')();

const { validate, build, topBox, freeCount, isEmpty, moveTile,
        findCompletedGroup, findRemovableBox, totalTiles, checkStatus, boxColors } = engine;

let failed = 0;
const ok = (cond, msg) => { if (!cond) { failed++; console.error('FAIL —', msg); } };

// Mirror của settle(), bỏ phần animation.
// drain=true  → hộp rỗng biến mất bất kể rỗng vì đâu (REMOVE_EMPTY_NONBOTTOM_BOX)
// drain=false → đọc chặt §7: chỉ CLEAR mới xoá hộp
function resolve(st, drain = engine.REMOVE_EMPTY_NONBOTTOM_BOX) {
  for (;;) {
    const hit = findCompletedGroup(st);
    if (hit) {
      const box = topBox(st.stacks[hit.s]);
      for (let i = 0; i < box.slots.length; i++)
        if (box.slots[i] && box.slots[i].groupId === hit.g) box.slots[i] = null;
      st.cleared++;
      if (isEmpty(box) && !box.isBottom) st.stacks[hit.s].boxes.shift();
      continue;
    }
    if (drain) {
      const s = findRemovableBox(st);
      if (s >= 0) { st.stacks[s].boxes.shift(); continue; }
    }
    break;
  }
  st.status = checkStatus(st);
  return st;
}

const load = (i, drain) => resolve(build(validate(LEVELS[i])), drain);

// Vị trí slot không đổi luật → sort nội dung top box. Stack cũng hoán vị được → sort.
const encode = st => st.stacks.map(k => k.boxes.map((b, i) =>
  i === 0 ? b.slots.filter(Boolean).map(t => t.cardId).sort().join(',')
          : b.slots.map(t => t ? t.cardId : '_').join(',')).join('/')).sort().join('|');

// Gom thẻ cùng group vào một box là tiến bộ; giữ slot trống cũng là tiến bộ.
function score(st) {
  let s = st.cleared * 1000 - totalTiles(st) * 10;
  for (const k of st.stacks) {
    const box = topBox(k), cnt = {};
    for (const t of box.slots) if (t) cnt[t.groupId] = (cnt[t.groupId] || 0) + 1;
    for (const g in cnt) s += cnt[g] * cnt[g];
    s += freeCount(box);
  }
  return s;
}

// Beam search: bộ nhớ có trần, đủ để chứng minh level giải được.
// Không tìm ra ≠ chắc chắn vô nghiệm — nới BEAM nếu nghi ngờ.
function solve(levelIdx, drain, beam = 600, maxDepth = 250) {
  let layer = [load(levelIdx, drain)], nodes = 0, bestCleared = -1, stale = 0;
  for (let d = 0; d < maxDepth; d++) {
    const next = [], seen = new Set();
    for (const cur of layer) {
      if (cur.status === 'won') return { ok: true, depth: d, nodes };
      if (cur.status !== 'playing') continue;
      for (let from = 0; from < cur.stacks.length; from++) {
        for (const t of topBox(cur.stacks[from]).slots) {
          if (!t) continue;
          for (let to = 0; to < cur.stacks.length; to++) {
            if (to === from || freeCount(topBox(cur.stacks[to])) === 0) continue;
            const n = structuredClone(cur);
            if (!moveTile(n, from, t.uid, to)) continue;
            resolve(n, drain);
            nodes++;
            if (n.status === 'won') return { ok: true, depth: d + 1, nodes };
            if (n.status === 'stuck') continue;
            const k = encode(n);
            if (seen.has(k)) continue;
            seen.add(k);
            next.push(n);
          }
        }
      }
    }
    if (!next.length) return { ok: false, why: `hết nhánh ở độ sâu ${d}`, nodes };
    const top = Math.max(...next.map(n => n.cleared));
    stale = top > bestCleared ? (bestCleared = top, 0) : stale + 1;
    if (stale > 40) return { ok: false, why: `bế tắc ở ${bestCleared} nhóm`, nodes };
    next.sort((a, b) => score(b) - score(a));
    layer = next.slice(0, beam);
  }
  return { ok: false, why: 'quá maxDepth', nodes };
}

// ── 1. Validate bắt được level hỏng ─────────────────────────────────────────
const broken = (mutate, label) => {
  const lv = structuredClone(LEVELS[0]);
  mutate(lv);
  let threw = false;
  try { validate(lv); } catch { threw = true; }
  ok(threw, `validate phải ném lỗi: ${label}`);
};
const allCards = l => l.meaning.groups.flatMap(g => g.cards);

broken(l => l.layout.stacks[0].boxes[0].slots[3] = 'apple', 'card trùng trên bàn');
broken(l => l.layout.stacks[0].boxes[0].slots[0] = null, 'card thiếu trên bàn');
broken(l => l.layout.stacks[0].boxes[0].slots[0] = 'fruit', 'đặt group lên bàn');
broken(l => l.meaning.groups[0].cards.pop(), 'group thiếu thành viên');
broken(l => l.meaning.groups[0].group = 'animal', 'group có cha (COLLAPSE)');
broken(l => delete l.meaning.groups[0].cards[2].text, 'card không có text lẫn art');
broken(l => allCards(l).forEach(c => c.text = c.text || c.id), 'level không còn thẻ chỉ-ảnh');
broken(l => {
  const withArt = allCards(l).filter(c => c.art);
  withArt[1].art = withArt[0].art;
}, 'hai thẻ dùng chung một ảnh');
// Mọi thẻ đều có art → không còn thẻ chỉ-chữ. Phải cấp key GIẢ và DUY NHẤT cho từng thẻ
// (và khai vào bảng ART), nếu không luật "trùng ảnh" sẽ bắn trước và test này kiểm nhầm thứ.
broken(l => {
  let i = 0;
  for (const c of allCards(l)) if (!c.art) { c.art = `fake-${i++}`; engine.ART[c.art] = '?'; }
}, 'level không còn thẻ chỉ-chữ');
broken(l => l.layout.stacks[1].pos = [0, 0], 'hai stack trùng pos');
broken(l => l.layout.stacks[0].boxes[0].slots.pop(), 'box không đủ 4 slot');
broken(l => allCards(l)[0].art = 'khong-ton-tai', 'art trỏ file không có');
LEVELS.forEach((l, i) => { try { validate(l); } catch (e) { failed++; console.error('FAIL — level', i, e.message); } });

// ── 2. Luật nước đi ─────────────────────────────────────────────────────────
{
  const st = load(0);
  const src = topBox(st.stacks[0]), uid = src.slots.find(Boolean).uid;
  ok(!moveTile(st, 0, uid, 0), 'thả về chính stack cũ phải bị từ chối');
  // Chọn đích đã có thẻ sẵn + còn chỗ, để chứng minh thẻ vào slot trống ĐẦU TIÊN
  // chứ không phải slot 0.
  const di = st.stacks.findIndex((k, i) =>
    i !== 0 && topBox(k).slots.some(Boolean) && freeCount(topBox(k)) > 0);
  ok(di > 0, 'level phải có ít nhất 1 stack đích vừa có thẻ vừa còn chỗ');
  const dst = topBox(st.stacks[di]), j = dst.slots.indexOf(null);
  ok(moveTile(st, 0, uid, di), 'thả sang stack khác còn chỗ phải được');
  ok(dst.slots[j] && dst.slots[j].uid === uid, `thẻ phải vào slot trống ĐẦU TIÊN (slot ${j})`);
  const buried = st.stacks[0].boxes[1].slots.find(Boolean).uid;
  ok(!moveTile(st, 0, buried, 3), 'không kéo được thẻ trong box bị che');

  const full = load(0);
  const jammed = topBox(full.stacks[1]);
  jammed.slots[2] = { uid: 'x1', cardId: 'z', groupId: 'zz' };
  jammed.slots[3] = { uid: 'x2', cardId: 'z', groupId: 'zz' };
  const before = encode(full);
  ok(!moveTile(full, 0, topBox(full.stacks[0]).slots.find(Boolean).uid, 1),
     'thả vào box đầy phải bị từ chối');
  ok(encode(full) === before, 'thả hụt thì state không đổi');
}

// ── 3. CLEAR + xoá box + lộ box dưới ────────────────────────────────────────
{
  const st = load(0);
  const box = topBox(st.stacks[0]);
  const G = box.slots.find(Boolean).groupId;
  const depthBefore = st.stacks[0].boxes.length;
  box.slots.forEach((_, i) => box.slots[i] = { uid: 'g' + i, cardId: 'c' + i, groupId: G });
  resolve(st);
  ok(st.cleared === 1, 'đủ 4 thành viên cùng group → CLEAR');
  ok(st.stacks[0].boxes.length === depthBefore - 1, 'box không-đáy rỗng sau CLEAR → bị xoá, box dưới lộ ra');

  const bot = load(0);
  const bbox = topBox(bot.stacks[2]);                 // stack 2 chỉ có box đáy
  ok(bbox.isBottom, 'stack buffer là box đáy');
  bbox.slots.forEach((_, i) => bbox.slots[i] = { uid: 'b' + i, cardId: 'c' + i, groupId: 'fruit' });
  resolve(bot);
  ok(bot.stacks[2].boxes.length === 1 && isEmpty(topBox(bot.stacks[2])),
     'CLEAR ở box đáy → box ở lại và rỗng');
}

// ── 4. Màu gợi ý (R2) ───────────────────────────────────────────────────────
{
  const mk = (uid, g) => ({ uid, cardId: uid, groupId: g });
  ok(boxColors({ slots: [mk('a', 'fruit'), mk('b', 'animal'), null, null] }).size === 0,
     'mỗi group 1 thẻ → không tô màu');
  const two = boxColors({ slots: [mk('a', 'fruit'), mk('b', 'animal'), mk('c', 'fruit'), null] });
  ok(two.size === 2 && two.get('a') === two.get('c'), '2 thẻ cùng group → cùng màu, thẻ lẻ không màu');
  const four = boxColors({ slots: [mk('a', 'fruit'), mk('b', 'animal'), mk('c', 'fruit'), mk('d', 'animal')] });
  ok(new Set(four.values()).size === 2, 'hai group ≥2 thẻ → hai màu khác nhau');
}

// ── 5. Thắng / kẹt ──────────────────────────────────────────────────────────
{
  const st = load(0);
  st.stacks.forEach(k => k.boxes.forEach(b => b.slots.fill(null)));
  ok(checkStatus(st) === 'won', 'sạch bàn → won');

  const jam = load(0);
  jam.stacks.forEach(k => {
    k.boxes.length = 1;
    k.boxes[0].slots.forEach((_, i) =>
      k.boxes[0].slots[i] = { uid: k.pos.join('') + i, cardId: 'x', groupId: 'g' + i });
  });
  ok(checkStatus(jam) === 'stuck', 'mọi top box đầy, không nhóm nào đủ → stuck');
}

// ── 6. Mọi level giải được, ở CẢ HAI cách đọc luật xoá hộp ──────────────────
// Bắt buộc phải giải được ở chế độ CHẶT: mọi hộp ẩn chỉ mở bằng một CLEAR dùng
// thẻ đang với tới được. Level chỉ giải được ở chế độ rộng nghĩa là nó bắt người
// chơi tự mò ra luật "kéo rỗng hộp thì hộp biến mất" — không dạy đúng vòng lặp lõi.
LEVELS.forEach((lv, i) => {
  for (const drain of [false, true]) {
    const t = Date.now();
    const r = solve(i, drain);
    const mode = drain ? 'rộng ' : 'chặt ';
    ok(r.ok, `level ${lv.id} phải giải được ở chế độ ${mode.trim()} (${r.why || ''})`);
    if (r.ok) console.log(`  ${lv.id} ${mode}— ${r.depth} nước (${r.nodes} nút, ${Date.now() - t}ms)`);
  }
});

console.log(failed ? `\n${failed} CHECK HỎNG` : '\nOK — mọi check pass');
process.exit(failed ? 1 : 0);
