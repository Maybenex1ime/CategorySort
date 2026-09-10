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
