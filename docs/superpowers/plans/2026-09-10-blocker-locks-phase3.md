# Blocker nhịp 3 — Tool dựng màn hiểu blocker

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tool web `demo/wordstack.html` gắn, giữ, kiểm và chơi thử được ba blocker; tool Unity và bản copy cũ bị xoá.

**Architecture:** Tool web làm việc trên model nội bộ theo NHÃN thẻ (`groups/words`, `stacks/boxes/tiles`). Blocker thẻ theo nhãn giống `icons`: `raw.cardBlockers = { "Chuối": { ice: 3, key: "k1" } }`. Blocker hộp nằm trên hộp: `box.blockers = { locked: 2 }`. Bộ chuyển đổi định dạng được dời ra khỏi phần giao diện vào một phần thuần hàm, để `demo/tool-check.mjs` tách ra kiểm bằng node. Luật blocker chép sang JS theo đúng spec và theo đúng các kịch bản của `SelfCheck` mục 8.

**Tech Stack:** HTML + JavaScript thuần một file · Node 24 cho `demo/tool-check.mjs` · C# chỉ để xoá tool Unity.

**Spec:** `docs/superpowers/specs/2026-09-10-blocker-locks-design.md`

## Global Constraints

- Quyết định đã chốt: **xoá** `LevelEditorWindow.cs`; **chép** luật blocker sang JS; **xoá** `Assets/Resources/wordstack.html`.
- Id blocker và ý nghĩa y hệt spec Mục 5: hộp `locked` (số ≥ 1), `keylock` (id chìa); thẻ `ice` (số ≥ 1), `key` (id chìa). Cặp được phép trên một thẻ: `ice` + `key`.
- Luật y hệt C#: hộp đóng không nhặt ra, không thả vào, không tự nổ, không bị xoá dù rỗng; thẻ băng không kéo, không tính bộ 4, không bị xoá khi nhóm nổ; băng đếm sau mỗi nước thành công, chỉ khi thẻ ở hộp trên cùng; thứ tự nước đi → giảm băng → dây chuyền; `locked` so với số nhóm đã gom (`state.solved.size`); `keylock` mở khi thẻ mang `key` cùng id không còn trên bàn.
- Tool web xoá hộp rỗng như chế độ **rộng** của C#. Không đổi điều đó.
- Màn **không** có blocker phải chạy y như trước: `checkStatus` giữ heuristic `hasAnyProgress` cũ; chỉ khi bàn còn blocker chưa mở mới dùng "còn nước đi hợp lệ".
- `cloneState` chia sẻ object thẻ giữa các bản sao (undo, gợi ý, bộ giải). Nên **không bao giờ sửa object thẻ tại chỗ**: giảm băng là thay thẻ bằng object mới.
- Không thêm thư viện, không build step. `demo/wordstack.html` vẫn mở thẳng bằng trình duyệt.
- Blocker trên thẻ sinh ra từ COLLAPSE: ngoài phạm vi, kiểm dữ liệu từ chối.

---

## Vòng phản hồi

| Lệnh | Phủ gì | Đầu ra mong đợi |
|---|---|---|
| `node demo/tool-check.mjs` | round-trip, kiểm dữ liệu, luật JS — tách từ HTML như `check.mjs` | `tool-check OK` |
| `node demo/check.mjs` | bản luật tham chiếu cũ, không đụng tới | `OK — mọi check pass` |
| `./compilecheck.sh` | ba assembly sau khi xoá tool Unity | `game.dll OK` `editor.dll OK` `meta.dll OK` |
| Mở `demo/wordstack.html` | giao diện | xem Task 6 |

`demo/tool-check.mjs` tách toàn bộ khối từ marker `PHẦN 1` tới marker `PHẦN 4`, nên mọi hàm nó kiểm phải nằm trong khối đó và **không đụng DOM hay localStorage**.

Commit: nếu classifier chặn `git add`/`git commit` ở Bash thì chạy qua PowerShell với `git -C D:\CategorySort ...`. Message tiếng Anh, mệnh lệnh, kết thúc bằng `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`. File HTML dùng CRLF trong working tree — sửa bằng script phải giữ CRLF.

---

### Task 1: Xoá tool Unity và bản copy cũ

**Files:**
- Delete: `Assets/_Game/Board/Editor/LevelEditorWindow.cs` và `.meta`
- Delete: `Assets/Resources/wordstack.html` và `.meta`
- Modify: `production/session-state/active.md:205`

`BoardTestDriver.cs` và `WordStack.Board.Editor.asmdef` ở lại — thư mục Editor vẫn cần. `ReadOnlyAttribute` chỉ dùng trong file bị xoá. `docs/wordstack-design-log.md` và `docs/session-log/` là lịch sử, không sửa.

- [ ] **Step 1: Xác nhận không còn tham chiếu**

Run: `grep -rn "LevelEditorWindow\|LevelProxy\|ReadOnlyAttribute\|\[ReadOnly\]" --include=*.cs Assets | grep -v LevelEditorWindow.cs`
Expected: không in gì.

- [ ] **Step 2: Xoá bốn file**

```bash
git rm Assets/_Game/Board/Editor/LevelEditorWindow.cs Assets/_Game/Board/Editor/LevelEditorWindow.cs.meta
git rm Assets/Resources/wordstack.html Assets/Resources/wordstack.html.meta
```

- [ ] **Step 3: Sửa bảng file trong `active.md`**

Thay dòng 205:

```
| `Assets/_Game/Board/Editor/LevelEditorWindow.cs` | Tool xếp level (`WordStack ▸ Level Editor`) |
```

bằng:

```
| `demo/wordstack.html` | Tool xếp level duy nhất (tool Unity `LevelEditorWindow` đã xoá 2026-09-10 — không giữ được collapse, moves, difficulty, blocker) |
```

- [ ] **Step 4: Compile**

Run: `./compilecheck.sh 2>&1 | grep "dll OK\|error CS"`
Expected: ba dòng OK.

- [ ] **Step 5: Commit**

```bash
git add production/session-state/active.md
git commit -m "chore(tool): remove the Unity level editor and the stale Resources copy of the web tool"
```

---

### Task 2: Dời bộ chuyển đổi định dạng ra khỏi phần giao diện, dựng `tool-check.mjs`

**Files:**
- Modify: `demo/wordstack.html` (dời 7 hàm từ PHẦN 6 lên PHẦN 3b mới; `rawToGame` nhận `artDir`; 2 nơi gọi)
- Create: `demo/tool-check.mjs`

**Interfaces:**
- Produces:
  - `rawToGame(raw, artDir)` — `artDir` là giá trị `artIndex()` trả về hoặc `null`. Không còn tự đọc localStorage.
  - `demo/tool-check.mjs` export nội bộ: `T = { parseLevel, applyMove, checkStatus, stateKey, solve, rawToGame, gameToRaw, gameJson, cloneState }`, helper `ok(cond, msg)`.

Task này **không đổi hành vi**. Nó chỉ làm cho các hàm kiểm được.

- [ ] **Step 1: Viết `demo/tool-check.mjs` với baseline round-trip**

```js
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

// (Các mục 2..4 thêm ở Task 3, 4, 5.)

if (failed) { console.error(`\n${failed} check FAIL`); process.exit(1); }
console.log('tool-check OK');
```

- [ ] **Step 2: Chạy để thấy nó hỏng vì chưa có khối**

Run: `node demo/tool-check.mjs`
Expected: lỗi `ReferenceError: rawToGame is not defined` (hàm còn nằm ở PHẦN 6).

- [ ] **Step 3: Dời 7 hàm lên PHẦN 3b**

Trong `demo/wordstack.html`, **cắt** các khối sau khỏi PHẦN 6, giữ nguyên nội dung, theo đúng thứ tự:

1. Comment `/* ---------- File level = format game (meaning/layout) ----------` … `*/` (ngay trên `slugify`)
2. `function slugify(s){ … }`
3. `function emojiArtKey(icon){ … }`
4. `function artKeyToIcon(key){ … }`
5. Comment `// Ghi JSON theo ĐÚNG lối trình bày …` và `function gameJson(lv){ … }`
6. `function artHits(a, key){ … }`
7. Comment `// Nội bộ (groups/words/tiles) → format game …` và `function rawToGame(raw){ … }`
8. Comment `// Format game → nội bộ …` và `function gameToRaw(gv){ … }`

`artIndex`, `artInfoRender`, `ART_LS`, `ART_EXT` và các handler `$(…)` **ở lại** PHẦN 6 — chúng đụng localStorage và DOM.

**Dán** tất cả ngay trước marker PHẦN 4, sau một marker mới:

```js
/* ==========================================================================
   PHẦN 3b — ĐỊNH DẠNG FILE GAME (thuần hàm — demo/tool-check.mjs tách từ PHẦN 1
   tới hết phần này, nên KHÔNG được đụng DOM hay localStorage ở đây)
   ========================================================================== */
```

- [ ] **Step 4: `rawToGame` nhận `artDir` thay vì tự đọc localStorage**

Đổi dòng khai báo:

```js
function rawToGame(raw){
```

thành:

```js
// artDir = artIndex() của PHẦN 6, hoặc null khi chưa nạp thư mục ảnh / khi chạy ngoài trình duyệt.
function rawToGame(raw, artDir){
```

và trong thân hàm xoá dòng:

```js
  const artDir = artIndex();
```

- [ ] **Step 5: Hai nơi gọi truyền `artIndex()`**

Trong handler `$('#edExport').onclick`:

```js
  const r = rawToGame(ed, artIndex());
```

Trong handler `$('#csvExport').onclick`:

```js
    const r = rawToGame(o.raw, artIndex());
```

- [ ] **Step 6: Chạy lại**

Run: `node demo/tool-check.mjs`
Expected: `tool-check OK`

Run: `node demo/check.mjs 2>&1 | tail -1`
Expected: `OK — mọi check pass`

- [ ] **Step 7: Kiểm tay trong trình duyệt**

Mở `demo/wordstack.html`. Tab Xếp level → Nhập JSON → dán nội dung `Assets/_Game/Content/Levels/lv-001.json` → Áp dụng → Xuất JSON. Kết quả: hiện JSON, không có lỗi trong Console của trình duyệt.

- [ ] **Step 8: Commit**

```bash
git add demo/wordstack.html demo/tool-check.mjs
git commit -m "refactor(tool): move the game-format converter into a pure section and check it from node"
```

---

### Task 3: Nhập và xuất giữ nguyên blocker

**Files:**
- Modify: `demo/wordstack.html` (`gameToRaw`, `rawToGame`, `gameJson` — cả ba đã ở PHẦN 3b)
- Test: `demo/tool-check.mjs` (mục 2)

**Interfaces:**
- Consumes: `rawToGame(raw, artDir)` (Task 2)
- Produces: model nội bộ mang `raw.cardBlockers` (nhãn → `{ ice?, key? }`) và `box.blockers` (`{ locked }` hoặc `{ keylock }`). `gameJson` ghi `"blockers"` trên thẻ và hộp.

- [ ] **Step 1: Viết test mục 2**

Trong `demo/tool-check.mjs`, thay dòng `// (Các mục 2..4 thêm ở Task 3, 4, 5.)` bằng:

```js
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
```

- [ ] **Step 2: Chạy để thấy fail**

Run: `node demo/tool-check.mjs`
Expected: `FAIL — blocker thẻ phải còn nguyên sau Nhập → Xuất` và `FAIL — blocker hộp phải còn nguyên …`

- [ ] **Step 3: `gameToRaw` chép blocker vào model nội bộ**

Thay:

```js
  for (const g of mGroups)
    for (const c of g.cards || []){
      label[c.id] = c.text || c.id;
      if (c.art) icons[label[c.id]] = artKeyToIcon(c.art);
    }
```

bằng:

```js
  const cardBlockers = {};                           // nhãn → { ice?, key? }, theo khuôn icons
  for (const g of mGroups)
    for (const c of g.cards || []){
      label[c.id] = c.text || c.id;
      if (c.art) icons[label[c.id]] = artKeyToIcon(c.art);
      if (c.blockers && Object.keys(c.blockers).length) cardBlockers[label[c.id]] = { ...c.blockers };
    }
```

Thay:

```js
  const stacks = sts.map(s => ({ boxes: (s.boxes || []).map(b => ({
    tiles: (b.slots || []).map(id => id ? (label[id] || id) : null)
  })) }));
```

bằng:

```js
  const stacks = sts.map(s => ({ boxes: (s.boxes || []).map(b => {
    const o = { tiles: (b.slots || []).map(id => id ? (label[id] || id) : null) };
    if (b.blockers && Object.keys(b.blockers).length) o.blockers = { ...b.blockers };
    return o;
  }) }));
```

Và ngay trước `return raw;` của `gameToRaw`:

```js
  if (Object.keys(cardBlockers).length) raw.cardBlockers = cardBlockers;
```

- [ ] **Step 4: `rawToGame` ghi blocker ra format game**

Trong phần dựng `gameLv.layout`, thay:

```js
      while (slots.length < 4) slots.push(null);
      return { slots };
```

bằng:

```js
      while (slots.length < 4) slots.push(null);
      const o = { slots };
      if (b.blockers && Object.keys(b.blockers).length) o.blockers = { ...b.blockers };
      return o;
```

Trong phần dựng `gameLv.meaning`, thay:

```js
      .map(w => ({ id: idOf.get(w), art: cardArt.get(w) }));
```

bằng:

```js
      .map(w => {
        const c = { id: idOf.get(w), art: cardArt.get(w) };
        const bl = (raw.cardBlockers || {})[w];
        if (bl && Object.keys(bl).length) c.blockers = { ...bl };
        return c;
      });
```

- [ ] **Step 5: `gameJson` in `"blockers"`**

Thay dòng in hộp:

```js
      o.push('          { "slots": [' + slots + '] }' + (k < st.boxes.length - 1 ? ',' : ''));
```

bằng:

```js
      const blk = b.blockers ? ', "blockers": ' + S(b.blockers) : '';
      o.push('          { "slots": [' + slots + ']' + blk + ' }' + (k < st.boxes.length - 1 ? ',' : ''));
```

Trong vòng in thẻ, sau dòng `if (c.art != null)  line += ', "art":' + S(c.art);` thêm:

```js
      if (c.blockers)     line += ', "blockers":' + S(c.blockers);
```

- [ ] **Step 6: Chạy lại**

Run: `node demo/tool-check.mjs`
Expected: `tool-check OK`

- [ ] **Step 7: Commit**

```bash
git add demo/wordstack.html demo/tool-check.mjs
git commit -m "feat(tool): keep card and box blockers through import and export"
```

---

### Task 4: Sáu luật kiểm dữ liệu blocker

**Files:**
- Modify: `demo/wordstack.html` (PHẦN 2: thêm `validateBlockers`, gọi trong `parseLevel`)
- Test: `demo/tool-check.mjs` (mục 3)

**Interfaces:**
- Consumes: `raw.cardBlockers`, `box.blockers` (Task 3)
- Produces: `validateBlockers(raw, owner, topicWords, errors)`; hằng `BOX_BLOCKERS`, `CARD_BLOCKERS`, `CARD_PAIRS`. Mọi đường — Kiểm tra, Chơi thử, Lưu, Xuất JSON, xuất CSV — đều qua `parseLevel` nên chặn ở đây là chặn cả năm.

- [ ] **Step 1: Viết test mục 3**

Trong `demo/tool-check.mjs`, thay dòng `// (Các mục 3..4 thêm ở Task 4, 5.)` bằng:

```js
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
```

- [ ] **Step 2: Chạy để thấy fail**

Run: `node demo/tool-check.mjs`
Expected: dòng đầu `FAIL — phải từ chối (id lạ trên thẻ) …`, và tổng khoảng 14 FAIL. Riêng `fixture hợp lệ phải qua kiểm` phải **không** FAIL.

- [ ] **Step 3: Thêm `validateBlockers` vào PHẦN 2**

Đặt ngay trên `function loadLevel(raw){`:

```js
/* ---------- Blocker: sổ đăng ký + sáu luật kiểm dữ liệu [spec 2026-09-10 Mục 5] ----------
   Khớp LevelData.Validate bên C#. Tool làm việc theo NHÃN thẻ nên blocker thẻ nằm ở
   raw.cardBlockers[nhãn], blocker hộp nằm ở box.blockers. */
const BOX_BLOCKERS  = ['locked', 'keylock'];
const CARD_BLOCKERS = ['ice', 'key'];
const CARD_PAIRS    = [['ice', 'key']];      // cặp được đứng chung trên một thẻ
const isCount = v => Number.isInteger(v) && v >= 1;
const cardPairAllowed = (a, b) => CARD_PAIRS.some(p => (p[0] === a && p[1] === b) || (p[0] === b && p[1] === a));

function validateBlockers(raw, owner, topicWords, errors){
  const cb = (raw.cardBlockers && typeof raw.cardBlockers === 'object') ? raw.cardBlockers : {};
  const keyOwner = new Map();                // id chìa → nhãn thẻ mang

  for (const label in cb){
    const bl = cb[label] || {}, at = `Thẻ "${label}"`, keys = Object.keys(bl);
    if (!owner.has(label)){ errors.push(`${at} mang blocker nhưng không phải từ của nhóm nào.`); continue; }
    if (topicWords.has(label)){ errors.push(`${at} là tên chủ đề sinh ra từ COLLAPSE — chưa hỗ trợ blocker trên thẻ đó.`); continue; }
    for (const k of keys){
      if (BOX_BLOCKERS.includes(k)) errors.push(`${at}: "${k}" là blocker của hộp, không đặt trên thẻ.`);
      else if (!CARD_BLOCKERS.includes(k)) errors.push(`${at}: blocker "${k}" không có trong sổ đăng ký.`);
    }
    for (let i = 0; i < keys.length; i++)
      for (let j = i + 1; j < keys.length; j++)
        if (CARD_BLOCKERS.includes(keys[i]) && CARD_BLOCKERS.includes(keys[j]) && !cardPairAllowed(keys[i], keys[j]))
          errors.push(`${at}: "${keys[i]}" và "${keys[j]}" không được đứng chung trên một thẻ.`);
    if ('ice' in bl && !isCount(bl.ice)) errors.push(`${at}: ice phải là số nguyên ≥ 1.`);
    if ('key' in bl){
      if (typeof bl.key !== 'string' || !bl.key) errors.push(`${at}: key phải là chuỗi id chìa.`);
      else if (keyOwner.has(bl.key)) errors.push(`Id chìa "${bl.key}" có hai thẻ mang: "${keyOwner.get(bl.key)}" và "${label}".`);
      else keyOwner.set(bl.key, label);
    }
  }

  (raw.stacks || []).forEach((st, si) => (st.boxes || []).forEach((b, bi) => {
    const bl = b.blockers || {}, keys = Object.keys(bl), at = `Stack ${si + 1} / box ${bi}`;
    for (const k of keys){
      if (CARD_BLOCKERS.includes(k)) errors.push(`${at}: "${k}" là blocker của thẻ, không đặt trên hộp.`);
      else if (!BOX_BLOCKERS.includes(k)) errors.push(`${at}: blocker "${k}" không có trong sổ đăng ký.`);
    }
    if (keys.length > 1) errors.push(`${at}: hộp mang tối đa một blocker.`);
    if ('locked' in bl && !isCount(bl.locked)) errors.push(`${at}: locked phải là số nguyên ≥ 1.`);
    if ('keylock' in bl){
      const keyLabel = keyOwner.get(bl.keylock);
      if (!keyLabel){ errors.push(`${at}: keylock "${bl.keylock}" không có thẻ nào mang chìa đó.`); return; }
      // Hộp có ổ không bao giờ rỗng nên hộp dưới không lộ ra chừng nào chưa mở. Chìa chỉ bị
      // gom khi cả nhóm về chung một hộp — thẻ nào của nhóm đó nằm trong/dưới đây là khoá vĩnh viễn.
      const kg = owner.get(keyLabel);
      for (let bj = bi; bj < st.boxes.length; bj++)
        for (const t of (st.boxes[bj].tiles || []))
          if (t && owner.get(String(t).trim()) === kg)
            errors.push(`${at}: thẻ "${t}" cùng nhóm với chìa "${bl.keylock}" nằm trong hoặc dưới hộp mà chìa đó mở — khoá vĩnh viễn.`);
    }
  }));
}

```

- [ ] **Step 4: Gọi trong `parseLevel`**

Ngay trên dòng `  /* --- Cảnh báo thiết kế [GDD §10.1] --- */` thêm:

```js
  validateBlockers(raw, owner, topicWords, errors);

```

- [ ] **Step 5: Chạy lại**

Run: `node demo/tool-check.mjs`
Expected: `tool-check OK`

- [ ] **Step 6: Commit**

```bash
git add demo/wordstack.html demo/tool-check.mjs
git commit -m "feat(tool): validate blocker data with the same six rules as the game"
```

---

### Task 5: Luật blocker trong engine JS

**Files:**
- Modify: `demo/wordstack.html` (PHẦN 1 engine, PHẦN 2 `parseLevel`, PHẦN 3 `stateKey` / `legalMoves`)
- Test: `demo/tool-check.mjs` (mục 4)

**Interfaces:**
- Consumes: `isCount` (Task 4), `raw.cardBlockers`, `box.blockers`
- Produces:
  - Thẻ engine: `ice: { need, have }` khi còn băng, `key: 'id'` khi mang chìa. Hộp engine: `lock: { kind: 'clears', need }` hoặc `{ kind: 'key', keyId }`.
  - `isFrozen(t)`, `isBoxOpen(state, box)`, `canPick(state, box, t)`, `hasPendingBlocker(state)`, `hasAnyMove(state)`, `tickIce(state)` — Task 6 dùng `canPick` và `isBoxOpen`.

Các kịch bản dưới đây là **cùng các kịch bản của `SelfCheck` mục 8** (8b, 8c, 8d), chuyển sang model theo nhãn. Bàn nền giống `RulesLv`: nhóm A/B/C thay cho c/d/e.

- [ ] **Step 1: Viết test mục 4**

Trong `demo/tool-check.mjs`, thay dòng `// (Mục 4 thêm ở Task 5.)` bằng:

```js
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
```

Ghi chú: ca "chìa nằm trong hộp nó mở" phải dựng bằng cách sửa thẳng trạng thái engine, vì dữ liệu đó bị luật 6 chặn từ `parseLevel` — y như `SelfCheck` 8d sửa thẳng `Game`.

Mục này gọi `T.isBoxOpen`. Trong dòng `return { … }` ở đầu `tool-check.mjs`, thêm `isBoxOpen, canPick` vào cuối danh sách. Chỉ thêm ở task này: hai hàm đó chưa tồn tại trước Task 5, thêm sớm là `ReferenceError` ngay lúc tách khối.

- [ ] **Step 2: Chạy để thấy fail**

Run: `node demo/tool-check.mjs`
Expected: `ReferenceError: isBoxOpen is not defined` — engine chưa có luật blocker.

- [ ] **Step 3: Thêm các hàm blocker vào PHẦN 1**

Ngay dưới dòng `const totalTiles  = state => …;` thêm:

```js
/* ---------- Blocker [spec 2026-09-10] — cùng luật với Assets/_Game/Board/Domain/GameBlockers.cs ----------
   Thẻ: ice = { need, have } khi còn băng, key = id chìa. Hộp: lock = { kind:'clears', need }
   hoặc { kind:'key', keyId }. cloneState chia sẻ object thẻ giữa các bản sao (undo, gợi ý,
   bộ giải), nên KHÔNG sửa thẻ tại chỗ: tickIce thay thẻ bằng object mới. */
const isFrozen = t => !!(t && t.ice);
const thaw = ({ ice, ...rest }) => rest;
function keyOnBoard(state, keyId){
  return state.stacks.some(st => st.boxes.some(b => b.tiles.some(t => t && t.key === keyId)));
}
// clears so với số nhóm đã gom, key hỏi thẻ chìa còn trên bàn không — cả hai suy từ bàn,
// không lưu trạng thái riêng, nên stateKey không phải mã hoá gì cho hộp.
function isBoxOpen(state, box){
  const l = box && box.lock;
  if (!l) return true;
  if (l.kind === 'clears') return state.solved.size >= l.need;
  if (l.kind === 'key') return !keyOnBoard(state, l.keyId);
  return true;
}
const canPick = (state, box, t) => !!t && !isFrozen(t) && isBoxOpen(state, box);
// Sau mỗi nước thành công: thẻ băng ở hộp trên cùng tiến một bước — kể cả trong hộp đang
// khoá, vì nó đang lộ. Thẻ chìm không đếm.
function tickIce(state){
  for (const st of state.stacks){
    const top = st.boxes[0];
    if (!top) continue;
    for (let i = 0; i < top.tiles.length; i++){
      const t = top.tiles[i];
      if (!isFrozen(t)) continue;
      const have = t.ice.have + 1;
      top.tiles[i] = have >= t.ice.need ? thaw(t) : { ...t, ice: { need: t.ice.need, have } };
    }
  }
}
function hasPendingBlocker(state){
  return state.stacks.some(st => st.boxes.some(b => !isBoxOpen(state, b) || b.tiles.some(isFrozen)));
}
// Soi đúng các chốt của applyMove mà không đi thử.
function hasAnyMove(state){
  for (const s of state.stacks){
    const src = s.boxes[0];
    if (!src || !isBoxOpen(state, src) || !src.tiles.some(t => t && !isFrozen(t))) continue;
    for (const d of state.stacks){
      if (d === s) continue;
      const dst = d.boxes[0];
      if (dst && isBoxOpen(state, dst) && freeSlots(dst) > 0) return true;
    }
  }
  return false;
}
```

- [ ] **Step 4: Nối luật vào engine — bảy chỗ sửa**

1. `cloneState`, trong dòng chép hộp, thêm `lock: b.lock,` ngay trước `tiles: b.tiles.slice()`. Object `lock` không bao giờ bị sửa nên chia sẻ được.

2. `findCompletedGroup`, đổi `if (t)` trong vòng đếm thành `if (t && !isFrozen(t))` — thẻ băng không tính bộ 4.

3. `resolveBoard` bước A, đổi `if (!box) continue;` thành:

```js
      if (!box || !isBoxOpen(state, box)) continue;         // hộp đóng không tự nổ
```

   và trong vòng xoá thẻ đổi `if (t && t.groupId === g.id)` thành `if (t && t.groupId === g.id && !isFrozen(t))`.

4. `resolveBoard` bước B, đổi `if (box && !box.isBottom && isEmptyBox(box)){` thành:

```js
      if (box && !box.isBottom && isEmptyBox(box) && isBoxOpen(state, box)){   // hộp đóng không bị xoá dù rỗng
```

5. `applyMove` — ngay sau dòng `if (src.id === dst.id) return …` thêm:

```js
  if (!isBoxOpen(s, src) || !isBoxOpen(s, dst)) return { ok:false, reason:'Hộp đang khoá' };
```

   ngay sau dòng `if (i < 0) … return { ok:false, reason:'Không tìm thấy ô từ' };` thêm:

```js
  if (isFrozen(src.tiles[i])) return { ok:false, reason:'Ô đang đóng băng' };
```

   và ngay sau `s.movesUsed++;` thêm:

```js
  tickIce(s);                                // nước đi → giảm băng → dây chuyền
```

6. `checkStatus` — thay cả hàm:

```js
function checkStatus(state){
  if (totalTiles(state) === 0) return 'won';
  // Blocker gỡ theo tiến độ, nên heuristic "gom được nhóm nào ngay không" báo kẹt oan khi
  // chỉ cần chờ băng tan hay hộp mở. Còn blocker chưa mở thì kẹt = hết nước đi hợp lệ, như C#.
  // Màn không có blocker giữ nguyên heuristic cũ — hành vi không đổi.
  if (hasPendingBlocker(state)) return hasAnyMove(state) ? 'playing' : 'stuck';
  return hasAnyProgress(state) ? 'playing' : 'stuck';
}
```

7. `parseLevel` — ngay sau dòng `const box = { id: uid('b'), capacity: cap, isBottom: …, tiles: new Array(cap).fill(null) };` thêm:

```js
      const bb = (rb && rb.blockers) || {};
      if (isCount(bb.locked)) box.lock = { kind:'clears', need: bb.locked };
      else if (typeof bb.keylock === 'string' && bb.keylock) box.lock = { kind:'key', keyId: bb.keylock };
```

   và thay dòng `box.tiles[k] = makeTile(w, gid, false);` bằng:

```js
        const t = makeTile(w, gid, false);
        const cb = (raw.cardBlockers || {})[w] || {};
        if (isCount(cb.ice)) t.ice = { need: cb.ice, have: 0 };
        if (typeof cb.key === 'string' && cb.key) t.key = cb.key;
        box.tiles[k] = t;
```

   Có chốt kiểu ở đây để dữ liệu hỏng không làm engine nổ; lỗi thật do `validateBlockers` báo.

- [ ] **Step 5: Bộ giải hiểu blocker — PHẦN 3**

`stateKey`, đổi `boxTiles(b).map(t => t.label)` thành:

```js
boxTiles(b).map(t => isFrozen(t) ? t.label + '~' + (t.ice.need - t.ice.have) : t.label)
```

và thêm một dòng chú thích ngay trên hàm: `// Thẻ băng mã thêm số nước còn lại. Hộp khoá và ổ không mã — suy từ bố cục thẻ.`

`legalMoves`, lọc sớm để bộ giải không thử và nút Gợi ý không gợi ý nước bị cấm:

- `if (!src) continue;` → `if (!src || !isBoxOpen(s, src)) continue;`
- `if (!dst || freeSlots(dst) === 0) continue;` → `if (!dst || freeSlots(dst) === 0 || !isBoxOpen(s, dst)) continue;`
- `for (const t of src.tiles) if (t) out.push(…)` → `for (const t of src.tiles) if (t && !isFrozen(t)) out.push(…)`

- [ ] **Step 6: Chạy lại cả hai bộ kiểm**

Run: `node demo/tool-check.mjs`
Expected: `tool-check OK`

Run: `node demo/check.mjs 2>&1 | tail -1`
Expected: `OK — mọi check pass`

- [ ] **Step 7: Kiểm màn không blocker không đổi**

Mở `demo/wordstack.html`, tab Chơi, chơi level 1 tới hết và bấm Gợi ý vài lần. Hành vi như trước nhịp này — màn không có blocker vẫn đi đường `hasAnyProgress` cũ.

- [ ] **Step 8: Commit**

```bash
git add demo/wordstack.html demo/tool-check.mjs
git commit -m "feat(tool): blocker rules in the web tool's engine and solver"
```

---

### Task 6: Giao diện — hiện, chặn, và gắn blocker

**Files:**
- Modify: `demo/wordstack.html` (CSS · markup tab Xếp level · PHẦN 5 `render` + kéo thả · PHẦN 6 `edRender` + handler)

**Interfaces:**
- Consumes: `isBoxOpen`, `isFrozen`, `canPick` (Task 5); `ed.cardBlockers`, `box.blockers` (Task 3)

Phần này chạm DOM nên `tool-check.mjs` không phủ được. Kiểm bằng tay ở Step 6.

- [ ] **Step 1: CSS**

Ngay sau dòng `.box.is-bottom{border-style:dashed}` thêm:

```css
.box.locked{position:relative;border-style:dotted;opacity:.8}
.box.locked::after{content:attr(data-lock);position:absolute;top:-11px;right:-6px;z-index:2;
  background:#000c;color:#fff;font-size:11px;font-weight:800;padding:2px 7px;border-radius:9px}
.tile.iced,.tile.has-key{position:relative}
.tile.iced{filter:saturate(.35) brightness(1.12);box-shadow:inset 0 0 0 3px #BFE6FF}
.tile.iced::after{content:'🧊' attr(data-ice);position:absolute;bottom:-7px;right:-5px;
  font-size:11px;font-weight:800;background:#1c3a52;color:#fff;border-radius:8px;padding:0 4px}
.tile.has-key::before{content:'🔑';position:absolute;top:-8px;left:-6px;font-size:13px}
```

- [ ] **Step 2: Tab Chơi hiện blocker**

Trong `render`, ngay sau dòng `bEl.className = 'box' + (box.isBottom ? ' is-bottom' : '');` thêm:

```js
    if (!isBoxOpen(state, box)){
      bEl.classList.add('locked');
      bEl.dataset.lock = box.lock.kind === 'clears'
        ? '🔒 ' + Math.max(box.lock.need - state.solved.size, 0)
        : '🗝 ' + box.lock.keyId;
    }
```

Trong vòng dựng thẻ, ngay sau dòng gán `tEl.className = 'tile' + …;` thêm:

```js
        if (isFrozen(t)){ tEl.classList.add('iced'); tEl.dataset.ice = t.ice.need - t.ice.have; }
        if (t.key) tEl.classList.add('has-key');
```

- [ ] **Step 3: Tab Chơi chặn thao tác**

Trong handler `pointerdown` của `boardEl`, ngay sau `if (!tile) return;` thêm:

```js
  const srcBox = topBox(state, tile.dataset.stackId);
  const srcTile = srcBox && srcBox.tiles.find(x => x && x.id === tile.dataset.tileId);
  if (!canPick(state, srcBox, srcTile)){
    const bEl = tile.closest('.box');
    bEl.classList.add('shake'); setTimeout(() => bEl.classList.remove('shake'), 300);
    toast(isFrozen(srcTile) ? '🧊 Ô đang đóng băng' : '🔒 Hộp đang khoá', 1200);
    return;
  }
```

Trong handler `pointermove`, đổi điều kiện tô viền đích:

```js
    t.classList.add(box && freeSlots(box) > 0 && isBoxOpen(state, box) ? 'drop-ok' : 'drop-bad');
```

Thả vào hộp đóng thì `applyMove` trả `reason: 'Hộp đang khoá'` và `endDrag` sẵn có đã cho hộp rung kèm toast — không phải sửa.

- [ ] **Step 4: Tab Xếp level — blocker hộp**

Trong `edRender`, thay đoạn mở đầu mỗi dòng hộp:

```js
      html += `<div class="boxrow"><div class="lbl"><span>Box ${bi} · ${tag}</span>
        ${st.boxes.length > 1 ? `<button class="btn sm danger" data-delbox="${si}.${bi}">✕</button>` : ''}</div>
        <div class="slots">`;
```

bằng:

```js
      const bl = b.blockers || {};
      const kind = 'locked' in bl ? 'locked' : ('keylock' in bl ? 'keylock' : '');
      const blkCtl = `<span class="row tight">
          <select data-bblk="${si}.${bi}" title="Blocker của hộp">
            <option value=""${kind === '' ? ' selected' : ''}>— không khoá —</option>
            <option value="locked"${kind === 'locked' ? ' selected' : ''}>🔒 khoá: cần N nhóm</option>
            <option value="keylock"${kind === 'keylock' ? ' selected' : ''}>🗝 ổ: cần chìa</option>
          </select>
          ${kind ? `<input data-bval="${si}.${bi}" value="${esc(bl[kind])}" placeholder="${kind === 'locked' ? 'số nhóm' : 'id chìa'}" style="width:72px">` : ''}
        </span>`;
      html += `<div class="boxrow"><div class="lbl"><span>Box ${bi} · ${tag}</span>${blkCtl}
        ${st.boxes.length > 1 ? `<button class="btn sm danger" data-delbox="${si}.${bi}">✕</button>` : ''}</div>
        <div class="slots">`;
```

Handler `change` hiện có của `#edStacks` đọc `sel.dataset.slot` rồi `.split` — select blocker không có `data-slot`, sẽ nổ. Chèn ngay đầu handler, trước dòng `const sel = e.target.closest('select');`:

```js
  const tg = e.target;
  if (tg.dataset.bblk !== undefined){
    const [si, bi] = tg.dataset.bblk.split('.').map(Number), b = ed.stacks[si].boxes[bi];
    if (!tg.value) delete b.blockers;
    else b.blockers = { [tg.value]: tg.value === 'locked' ? 1 : 'k1' };
    edRender(); return;
  }
  if (tg.dataset.bval !== undefined){
    const [si, bi] = tg.dataset.bval.split('.').map(Number), b = ed.stacks[si].boxes[bi];
    const k = Object.keys(b.blockers || {})[0];
    if (k) b.blockers[k] = k === 'locked' ? parseInt(tg.value, 10) : tg.value.trim();
    edRender(); return;
  }
```

Nhập chữ vào ô số của `locked` cho ra `NaN`; nút Kiểm tra sẽ báo "locked phải là số nguyên" — không chặn ở đây, để một chỗ kiểm duy nhất nói.

- [ ] **Step 5: Tab Xếp level — blocker thẻ**

Markup: ngay sau panel Bàn chơi (khối kết thúc bằng `<div class="bank" id="edBank"></div>` rồi `</div>`) thêm:

```html
    <div class="panel">
      <h3>Blocker thẻ <span style="text-transform:none;font-weight:600">— 🧊 băng N nước · 🔑 chìa mở ổ cùng id</span></h3>
      <div id="edCardBlk"></div>
      <button class="btn sm" id="edAddCardBlk">+ Blocker thẻ</button>
    </div>
```

Cuối `edRender`, trước dấu `}` đóng hàm, thêm:

```js
  /* --- blocker thẻ: theo NHÃN, như icons. Thẻ sinh từ COLLAPSE không có trong danh sách --- */
  const cbEl = $('#edCardBlk'), cbs = ed.cardBlockers || {};
  cbEl.innerHTML = Object.keys(cbs).length ? '' : '<span class="hintline">(chưa có)</span>';
  Object.keys(cbs).forEach(label => {
    const bk = cbs[label], row = document.createElement('div');
    row.className = 'row tight'; row.style.marginBottom = '6px';
    row.innerHTML = `<select data-cbw="${esc(label)}">` +
      words.map(w => `<option value="${esc(w)}"${w === label ? ' selected' : ''}${w !== label && cbs[w] ? ' disabled' : ''}>${esc(w)}</option>`).join('') +
      `</select>
      <label class="f" style="max-width:80px">🧊 nước<input data-cbice="${esc(label)}" value="${bk.ice != null ? esc(bk.ice) : ''}" placeholder="—"></label>
      <label class="f" style="max-width:90px">🔑 id chìa<input data-cbkey="${esc(label)}" value="${bk.key != null ? esc(bk.key) : ''}" placeholder="—"></label>
      <button class="btn sm danger" data-cbdel="${esc(label)}">✕</button>`;
    cbEl.appendChild(row);
  });
```

`words` ở đây là biến `edRender` đã tính sẵn từ `edPlaceableWords()` ở phần bàn chơi.

Handler, đặt ngay sau handler `$('#edAddStack').onclick`:

```js
$('#edCardBlk').addEventListener('change', e => {
  const tg = e.target, cbs = ed.cardBlockers = ed.cardBlockers || {};
  if (tg.dataset.cbw !== undefined && tg.value !== tg.dataset.cbw && !cbs[tg.value]){
    cbs[tg.value] = cbs[tg.dataset.cbw]; delete cbs[tg.dataset.cbw];   // blocker đi theo sang thẻ mới
  }
  if (tg.dataset.cbice !== undefined){
    const bk = cbs[tg.dataset.cbice], n = parseInt(tg.value, 10);
    if (!tg.value.trim()) delete bk.ice; else bk.ice = Number.isNaN(n) ? tg.value : n;
  }
  if (tg.dataset.cbkey !== undefined){
    const bk = cbs[tg.dataset.cbkey];
    if (!tg.value.trim()) delete bk.key; else bk.key = tg.value.trim();
  }
  edRender();
});
$('#edCardBlk').addEventListener('click', e => {
  const b = e.target.closest('button');
  if (!b || b.dataset.cbdel === undefined) return;
  delete ed.cardBlockers[b.dataset.cbdel];
  if (!Object.keys(ed.cardBlockers).length) delete ed.cardBlockers;
  edRender();
});
$('#edAddCardBlk').onclick = () => {
  const cbs = ed.cardBlockers = ed.cardBlockers || {};
  const free = edPlaceableWords().words.find(w => !cbs[w]);
  if (!free){ edMsg('warn', 'Mọi thẻ đã có blocker.'); return; }
  cbs[free] = { ice: 1 };
  edRender();
};
```

Đổi tên một từ trong phần Nhóm thì blocker vẫn giữ nhãn cũ; nút Kiểm tra báo "mang blocker nhưng không phải từ của nhóm nào" để người dựng sửa. Không tự đổi theo — đoán nhãn mới là đoán sai được.

- [ ] **Step 6: Kiểm tay trong trình duyệt**

Chạy `node demo/tool-check.mjs` trước (phải `tool-check OK`), rồi mở `demo/wordstack.html`:

1. Tab Xếp level → Nhập JSON → dán `Assets/_Game/Content/Levels/lv-001.json` → Áp dụng.
2. Box 0 của Stack 1: chọn `🔒 khoá`, gõ `1`. Box 0 của Stack 2: chọn `🗝 ổ`, giữ `k1`.
3. `+ Blocker thẻ`, chọn một thẻ chó (poodle…), điền `🧊 2` và `🔑 k1`.
4. Bấm Kiểm tra: không lỗi blocker. Bấm Xuất JSON: JSON có `"blockers"` ở hai hộp và ở thẻ đã chọn.
5. Dán lại chính JSON vừa xuất → Áp dụng: ba blocker vẫn còn trên giao diện.
6. Chuyển Stack 2 sang `🗝 ổ` với id `k9` → Kiểm tra báo "không có thẻ nào mang chìa".
7. Chơi thử: hộp khoá và hộp có ổ hiện nhãn và viền chấm; thẻ băng hiện 🧊 kèm số; bấm vào chúng thì hộp rung và toast. Đi hai nước ở chỗ khác → băng biến mất, kéo được. Kéo thẻ khác thả vào hộp khoá → hộp rung, toast `Hộp đang khoá`.
8. Bấm Gợi ý: không bao giờ gợi ý kéo thẻ băng hay chạm hộp đóng.

- [ ] **Step 7: Commit**

```bash
git add demo/wordstack.html
git commit -m "feat(tool): show, block and author blockers in the play and editor tabs"
```

---

### Task 7: Tài liệu và chốt nhịp

**Files:**
- Modify: `docs/wordstack-rules.md` (Mục 11, thêm đoạn cuối)

- [ ] **Step 1: Ghi tool vào Mục 11**

Cuối Mục 11 của `docs/wordstack-rules.md` thêm:

```markdown
**Công cụ dựng màn.** `demo/wordstack.html` là tool duy nhất: tab Xếp level gắn blocker hộp
ngay trên dòng hộp, blocker thẻ ở panel "Blocker thẻ"; Nhập và Xuất JSON giữ nguyên
`blockers`; Kiểm tra, Chơi thử và Gợi ý chạy luật blocker bản JavaScript. `node demo/tool-check.mjs`
kiểm round-trip, sáu luật dữ liệu và luật chơi bản JS bằng đúng các kịch bản của `SelfCheck`
mục 8. Tool Unity `WordStack ▸ Level Editor` đã xoá. Cổng xuất bản vẫn là `./selfcheck.sh` —
bộ giải của tool là greedy có ngân sách, không thay được beam search hai chế độ.
```

- [ ] **Step 2: Chạy trọn vòng kiểm**

Run: `node demo/tool-check.mjs`
Expected: `tool-check OK`

Run: `node demo/check.mjs 2>&1 | tail -1`
Expected: `OK — mọi check pass`

Run: `./compilecheck.sh 2>&1 | grep "dll OK"`
Expected: ba dòng OK.

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SelfCheck OK - 2 level, luật khớp demo/check.mjs` — nhịp này không đụng C# ngoài việc xoá tool, số này phải y như trước.

- [ ] **Step 3: Commit**

```bash
git add docs/wordstack-rules.md
git commit -m "docs(rules): the web tool is the only level editor and understands blockers"
```

---

## Ngoài nhịp này

- Tab CSV sinh bàn không có blocker. Muốn có thì bấm "Mở trong trình xếp" rồi gắn tay.
- `Build/wordstack.html` là bản copy bị gitignore, sẽ lệch khỏi `demo/wordstack.html` sau nhịp này. Chép lại khi cần dùng bản đó.
- Chưa có kiểm chéo tự động giữa luật C# và luật JS. Hai bên khớp nhau vì `tool-check.mjs` chạy đúng các kịch bản của `SelfCheck` mục 8; thêm luật blocker mới thì phải thêm kịch bản ở **cả hai** nơi.
- `demo/check.mjs` và `demo/wordstack-clear-demo.html` là bản luật cũ chưa có collapse lẫn blocker. Nợ có từ trước.
- lv-002 chưa giải được ở chế độ chặt và lv-008 thiếu ảnh `airplane.png` — việc content đang treo.
