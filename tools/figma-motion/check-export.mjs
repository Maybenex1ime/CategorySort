// Kiểm export-figma-motion.js NGOÀI Figma: dựng đối tượng `figma` giả từ dữ liệu thật của timeline mở
// hộp khoá (Group 547, đọc qua Plugin API 2026-09-28) rồi chạy nguyên script.
//   node tools/figma-motion/check-export.mjs          → kiểm
//   node tools/figma-motion/check-export.mjs --print  → in JSON xuất ra
import { readFileSync } from 'node:fs';
import assert from 'node:assert/strict';

const EO = { type: 'EASE_OUT', easingFunctionCubicBezier: { x1: 0, y1: 0, x2: 0.58, y2: 1 } };
const f = v => ({ type: 'FLOAT', value: v });
let kid = 0;
const track = (id, op, ...kv) => ({ id: 'KeyframeTrackId:' + id, keyframeOperation: op,
  keyframes: kv.map(([t, v]) => ({ id: 'k' + kid++, timelinePosition: t, value: f(v), easing: EO })) });
const style = (id, kind, offset, duration) => ({ id: 'AnimationPresetId:' + id, name: 'motion.preset_name.' + kind, timelineOffset: offset, duration });
const node = (id, name, abs, animations, animationStyles) =>
  ({ id, name, type: 'GROUP', absoluteTransform: abs, animations, animationStyles, manualKeyframeTracks: {} });
const bind = (base, ...tracks) => ({ baseValue: f(base), timelineDuration: 2, tracks });
const I = (x, y) => [[1, 0, x], [0, 1, y]];
const FLIP = (x, y) => [[-1, 0, x], [0, 1, y]];

// Tấm cửa + lớp nan: bên phải (Lit 3) bị lật ngang, số translation giống hệt bên trái.
const shutter = (id, abs, [sScale, sPosA, sPosB], [tX1, tX2, tY1, tY2, tSX, tSY], name) => node(id, name, abs, {
  TRANSLATION_X: bind(abs[0][2], track(tX1, 'OFFSET', [0, 0], [0.229, -30]), track(tX2, 'OFFSET', [0, 0], [0.229, -7])),
  TRANSLATION_Y: bind(abs[1][2], track(tY1, 'OFFSET', [0, 0], [0.229, 0]), track(tY2, 'OFFSET', [0, 0], [0.229, 0])),
  SCALE_X: bind(1, track(tSX, 'SCALE', [0, 1], [0.400292, 0])),
  SCALE_Y: bind(1, track(tSY, 'SCALE', [0, 1], [0.400292, 1])),
}, [style(sScale, 'scale', 0.057708, 0.400292), style(sPosA, 'position', 0, 0.229), style(sPosB, 'position', 0.229, 0.229)]);
const slats = (id, abs, [sPosA, sScale, sPosB], [tX1, tX2, tY1, tY2, tSX, tSY], name) => node(id, name, abs, {
  TRANSLATION_X: bind(abs[0][2], track(tX1, 'OFFSET', [0, 0], [0.25, -32]), track(tX2, 'OFFSET', [0, 0], [0.213, -7])),
  TRANSLATION_Y: bind(abs[1][2], track(tY1, 'OFFSET', [0, 0], [0.25, 0]), track(tY2, 'OFFSET', [0, 0], [0.213, 0])),
  SCALE_X: bind(1, track(tSX, 'SCALE', [0, 1], [0.395, 0])),
  SCALE_Y: bind(1, track(tSY, 'SCALE', [0, 1], [0.395, 1])),
}, [style(sPosA, 'position', 0, 0.25), style(sScale, 'scale', 0.063, 0.395), style(sPosB, 'position', 0.245, 0.213)]);
const bar = (id, abs, [sScale, sPos], [tX, tY, tSX, tSY], name) => node(id, name, abs, {
  TRANSLATION_X: bind(abs[0][2], track(tX, 'OFFSET', [0, 0], [0.03, 0])),
  TRANSLATION_Y: bind(abs[1][2], track(tY, 'OFFSET', [0, 0], [0.03, 5])),
  SCALE_X: bind(1, track(tSX, 'SCALE', [0, 1], [0.03, 1])),
  SCALE_Y: bind(1, track(tSY, 'SCALE', [0, 1], [0.03, 0])),
}, [style(sScale, 'scale', 0.459, 0.03), style(sPos, 'position', 0.459, 0.03)]);

const children = [
  shutter('373:1225', I(989, 2752), ['519:1213', '523:1572', '524:1593'], ['523:1573', '524:1594', '523:1576', '524:1597', '519:1214', '519:1217'], 'Rectangle 309'),
  slats('519:1024', I(1006, 2759), ['519:1119', '519:1230', '524:1586'], ['519:1121', '524:1587', '519:1124', '524:1590', '519:1231', '519:1234'], 'Upper Layer'),
  shutter('524:1761', FLIP(1146, 2752), ['524:1770', '524:1777', '524:1784'], ['524:1778', '524:1785', '524:1781', '524:1788', '524:1771', '524:1774'], 'Rectangle 309'),
  slats('524:1762', FLIP(1130, 2759), ['524:1791', '524:1798', '524:1805'], ['524:1792', '524:1806', '524:1795', '524:1809', '524:1799', '524:1802'], 'Upper Layer'),
  bar('536:1904', I(998.489, 2737), ['536:2032', '536:2046'], ['536:2048', '536:2051', '536:2034', '536:2037'], 'Group 558'),
  bar('535:1180', I(1001, 2959), ['536:2033', '536:2047'], ['536:2054', '536:2057', '536:2040', '536:2043'], 'Group 557'),
  node('531:2002', 'Upper', I(978, 2729), {
    TRANSLATION_X: bind(978, track('535:1041', 'OFFSET', [0, 0], [0.03, 0])),
    TRANSLATION_Y: bind(2729, track('535:1044', 'OFFSET', [0, 0], [0.03, 20])),
  }, [style('535:1034', 'position', 0.46, 0.03)]),
];
const root = node('373:1245', 'Group 547', I(978, 2729), {
  OPACITY: bind(1, track('536:2069', 'SCALE', [0, 1], [0.03, 0])),
  SCALE_X: bind(1, track('536:2061', 'SCALE', [0, 1], [0.03, 0.9])),
  SCALE_Y: bind(1, track('536:2064', 'SCALE', [0, 1], [0.03, 0.9])),
}, [style('536:2060', 'scale', 0.492, 0.03), style('536:2068', 'opacity', 0.49, 0.03)]);
root.parent = { type: 'PAGE' };
root.findAll = pred => children.filter(pred);
const byId = Object.fromEntries([root, ...children].map(n => [n.id, n]));
const figma = { getNodeByIdAsync: async id => byId[id] ?? null, setCurrentPageAsync: async () => {} };

const src = readFileSync(new URL('../../Assets/_Modules/FigmaMotion/Exporter/export-figma-motion.js', import.meta.url), 'utf8');
const run = new (Object.getPrototypeOf(async function () {}).constructor)('figma', src);
const { json, warnings } = await run(figma);
if (process.argv.includes('--print')) { console.log(JSON.stringify(json, null, 2)); process.exit(0); }

const get = (target, prop) => {
  const t = json.tracks.find(x => x.target === target && x.property === prop);
  assert.ok(t, `thiếu track ${target} · ${prop}`);
  return t;
};
const EASE_OUT = 'cubic-bezier(0, 0, 0.58, 1)';
const near = (a, b, msg, eps = 2e-3) => assert.ok(Math.abs(a - b) <= eps, `${msg}: cần ${b}, ra ${a}`);

assert.deepEqual(warnings, []);
// Tấm trái trượt trái; tấm phải BỊ LẬT nên cùng số −30/−37 nhưng trượt PHẢI trong group gốc.
assert.deepEqual(get('Lit', 'x').keys, [
  { time: 0, value: 0, ease: EASE_OUT }, { time: 0.229, value: -30, ease: EASE_OUT }, { time: 0.458, value: -37 }]);
assert.deepEqual(get('Lit (1)', 'x').keys.map(k => k.value), [0, 30, 37]);
assert.deepEqual(get('Lit (1)', 'scaleX').keys.map(k => [k.time, k.value]), [[0.0577, 1], [0.458, 0]]);
assert.ok(!json.tracks.some(t => t.target === 'Lit' && t.property === 'y'), 'y không đổi thì không có track');

// Lớp nan: hai track chồng nhau → lấy mẫu. Tổng phải trùng số Figma tự tính (get_motion_context).
const at = (t, time) => { const k = t.keys; for (let i = 1; i < k.length; i++) if (k[i].time >= time - 1e-9) {
  const a = k[i - 1], b = k[i]; return a.value + (b.value - a.value) * (time - a.time) / (b.time - a.time || 1); } };
const figmaSamples = [[0.1, -18.268], [0.2, -30.007], [0.245, -31.976], [0.25, -32.275], [0.3, -34.725], [0.4, -38.237], [0.458, -39]];
for (const [time, v] of figmaSamples) {
  near(at(get('UpperLit[0]', 'x'), time), v, `nan trái @${time}s`, 0.05);
  near(at(get('UpperLit[1]', 'x'), time), -v, `nan phải @${time}s`, 0.05);
}

// Thanh Middle, Upper: y Figma hướng xuống (importer lật dấu). Root: scale đều hai trục → "scale".
assert.deepEqual(get('Middle/Top', 'y').keys.map(k => [k.time, k.value]), [[0.459, 0], [0.489, 5]]);
assert.deepEqual(get('Middle/Bottom', 'scaleY').keys.map(k => k.value), [1, 0]);
assert.deepEqual(get('Upper', 'y').keys.map(k => [k.time, k.value]), [[0.46, 0], [0.49, 20]]);
assert.deepEqual(get('', 'opacity').keys.map(k => [k.time, k.value]), [[0.49, 1], [0.52, 0]]);
assert.deepEqual(get('', 'scale').keys.map(k => [k.time, k.value]), [[0.492, 1], [0.522, 0.9]]);
assert.equal(json.tracks.length, 15);
console.log(`check-export OK — ${json.tracks.length} track, khớp số Figma (kể cả tấm lật và lớp nan chồng track)`);
