// Xuất timeline Motion của Figma → JSON figma-motion/v1 cho Figma Motion Importer (Unity).
// Chạy qua use_figma (Plugin API): dán nguyên file làm `code`, sửa CONFIG. CHỈ ĐỌC — không đụng file
// Figma. Giá trị trả về có `json` (lưu thành *.figma-motion.json) và `warnings`.
//
// Số tính theo GROUP GỐC (CONFIG.rootId = Root trong Unity), không theo world/page:
//  - Translation Figma ghi theo trục riêng của layer. Layer bị lật/xoay (hoặc nằm trong group bị
//    lật/xoay) thì "trượt trái" trên số có thể là trượt phải trên màn — script nhân độ dời với hướng
//    của layer so với group gốc để ra hướng thật. Không còn cần mirrorX bên Unity.
//  - Scale theo trục riêng của layer: xoay 90° thì scaleX của nó là chiều dọc trong group gốc.
//  - Góc xoay đảo chiều khi layer bị lật (định thức âm).
// Timeline: một thuộc tính có thể là tổng nhiều track (animation style preset + keyframe tay), mỗi
// track có mốc bắt đầu riêng. Track không chồng nhau → giữ đúng easing từng đoạn (cubic-bezier);
// chồng nhau → lấy mẫu CONFIG.sampleRate lần/giây, nội suy thẳng.

const CONFIG = {
  rootId: '373:1245',              // group/frame gốc của animation = Root trong Unity
  name: 'Mở hộp khoá nhóm',
  pxToUnit: 0.005,                 // ghi thẳng vào JSON; importer dùng khi đổi px → unit
  sampleRate: 120,
  // node Figma có animation → đường dẫn trong prefab tính từ Root ("" = Root). Thiếu thì dùng tên
  // layer Figma và cảnh báo — importer sẽ báo "không thấy node" nếu tên không khớp.
  targets: {
    '373:1245': '',                // Group 547
    '373:1225': 'Lit',             // Lit 2 › Rectangle 309 (tấm trái)
    '519:1024': 'UpperLit[0]',     // Lit 2 › Upper Layer
    '524:1761': 'Lit (1)',         // Lit 3 › Rectangle 309 (tấm phải, bị lật)
    '524:1762': 'UpperLit[1]',     // Lit 3 › Upper Layer
    '536:1904': 'Middle/Top',      // Group 558
    '535:1180': 'Middle/Bottom',   // Group 557
    '531:2002': 'Upper',
  },
};

const EPS = 1e-4;
const LINEAR = { b: [1 / 3, 1 / 3, 2 / 3, 2 / 3], linear: true };
const warnings = [];

const first = await figma.getNodeByIdAsync(CONFIG.rootId);
if (!first) throw new Error('Không thấy node gốc ' + CONFIG.rootId);
let page = first;
while (page && page.type !== 'PAGE') page = page.parent;
await figma.setCurrentPageAsync(page);
const root = await figma.getNodeByIdAsync(CONFIG.rootId);

// ---- hướng 2×2 (absoluteTransform luôn có trục đơn vị: chỉ còn xoay + lật)
const lin = t => [[t[0][0], t[0][1]], [t[1][0], t[1][1]]];
const inv = m => { const d = m[0][0] * m[1][1] - m[0][1] * m[1][0]; return [[m[1][1] / d, -m[0][1] / d], [-m[1][0] / d, m[0][0] / d]]; };
const mul = (a, b) => [[a[0][0] * b[0][0] + a[0][1] * b[1][0], a[0][0] * b[0][1] + a[0][1] * b[1][1]],
                       [a[1][0] * b[0][0] + a[1][1] * b[1][0], a[1][0] * b[0][1] + a[1][1] * b[1][1]]];
const rootInv = inv(lin(root.absoluteTransform));
const near = (v, t) => Math.abs(v - t) < 1e-3;
const isAxisAligned = L => L.every(row => (near(Math.abs(row[0]), 1) && near(row[1], 0)) || (near(row[0], 0) && near(Math.abs(row[1]), 1)));

// ---- easing
function bezierY(b, p) {   // CSS cubic-bezier: tìm u để x(u) = p, trả y(u)
  const c = (u, a1, a2) => 3 * (1 - u) * (1 - u) * u * a1 + 3 * (1 - u) * u * u * a2 + u * u * u;
  let lo = 0, hi = 1;
  for (let i = 0; i < 60; i++) { const m = (lo + hi) / 2; if (c(m, b[0], b[2]) < p) lo = m; else hi = m; }
  return c((lo + hi) / 2, b[1], b[3]);
}
function easeOf(e, where) {
  if (!e || !('type' in e)) { warnings.push(`${where}: easing gắn biến — tạm dùng ease-out.`); return { b: [0, 0, 0.58, 1] }; }
  if (e.type === 'LINEAR') return LINEAR;
  if (e.type === 'HOLD') return { hold: true };
  if (e.easingFunctionCubicBezier) { const f = e.easingFunctionCubicBezier; return { b: [f.x1, f.y1, f.x2, f.y2] }; }
  warnings.push(`${where}: easing ${e.type} (spring) chưa hỗ trợ — tạm dùng ease-out.`);
  return { b: [0, 0, 0.58, 1] };
}
const easeAt = (ez, p) => ez.hold ? (p >= 1 ? 1 : 0) : bezierY(ez.b, p);
const easeName = ez => ez.linear ? 'linear' : `cubic-bezier(${ez.b.map(v => +v.toFixed(4)).join(', ')})`;

// ---- track → đoạn có thời điểm tuyệt đối
const idNum = id => (id.match(/(\d+):(\d+)$/) || [0, 0, 0]).slice(1).map(Number);
const byId = (a, b) => { const x = idNum(a.id), y = idNum(b.id); return x[0] - y[0] || x[1] - y[1]; };
const styleKind = f => f.startsWith('TRANSLATION') ? 'position' : f.startsWith('SCALE') ? 'scale'
                     : f === 'OPACITY' ? 'opacity' : f === 'ROTATION' ? 'rotat' : '?';
const numOf = (v, comp) => v.type === 'VECTOR' ? v.value[comp] : v.value;

// Track sinh ra từ animation style mang keyframe tính từ đầu style: gán track về style cùng loại + cùng
// thời lượng (trùng thì theo thứ tự id) để lấy mốc bắt đầu. Track keyframe tay thì mốc đã tuyệt đối.
function fieldsOf(n) {
  const styles = (n.animationStyles || []).slice().sort(byId);
  const manualIds = new Set(Object.values(n.manualKeyframeTracks || {}).filter(Boolean).map(b => b.id));
  const out = {};
  for (const [field, binding] of Object.entries(n.animations || {})) {
    if (!binding || !binding.tracks) continue;
    const parts = field.endsWith('_XY') ? [[field.replace('_XY', '_X'), 'x'], [field.replace('_XY', '_Y'), 'y']] : [[field, null]];
    const used = new Set();
    for (const tr of binding.tracks.slice().sort(byId)) {
      const keys = tr.keyframes.slice().sort((a, b) => a.timelinePosition - b.timelinePosition);
      if (keys.length < 2) continue;
      let offset = 0;
      if (!manualIds.has(tr.id)) {
        const last = keys[keys.length - 1].timelinePosition;
        const s = styles.find(st => !used.has(st.id) && st.name.includes(styleKind(field)) && Math.abs(st.duration - last) < 0.002);
        if (s) { used.add(s.id); offset = s.timelineOffset || 0; }
        else warnings.push(`${n.name} (${n.id}) ${field}: không ghép được track ${tr.id} với animation style nào — coi như bắt đầu ở 0 s.`);
      }
      for (const [name, comp] of parts) {
        const f = out[name] || (out[name] = { base: numOf(binding.baseValue, comp), tracks: [] });
        const segs = [];
        for (let i = 0; i + 1 < keys.length; i++) {
          segs.push({ t0: offset + keys[i].timelinePosition, t1: offset + keys[i + 1].timelinePosition,
                      v0: numOf(keys[i].value, comp), v1: numOf(keys[i + 1].value, comp),
                      ez: easeOf(keys[i].easing, `${n.name} ${field}`) });
        }
        f.tracks.push({ op: tr.keyframeOperation, segs });
      }
    }
  }
  return out;
}

function trackAt(tr, t) {
  const s = tr.segs;
  if (t <= s[0].t0) return s[0].v0;
  for (const g of s) if (t <= g.t1) return t < g.t0 ? g.v0 : g.v0 + (g.v1 - g.v0) * easeAt(g.ez, (t - g.t0) / (g.t1 - g.t0 || 1));
  return s[s.length - 1].v1;
}
function valueAt(f, t) {
  if (!f) return null;
  let v = f.base;
  for (const tr of f.tracks) { const x = trackAt(tr, t); v = tr.op === 'SET' ? x : tr.op === 'SCALE' ? v * x : v + x; }
  return v;
}
const moving = f => f ? f.tracks.flatMap(tr => tr.segs.filter(s => Math.abs(s.v1 - s.v0) > 1e-6)) : [];

// Key cho một đại lượng: đoạn thay đổi không chồng nhau → key ở mép đoạn + easing đúng đoạn đó;
// chồng nhau → lấy mẫu đều. Khoảng nghỉ giữa hai đoạn giữ nguyên giá trị (đoạn phẳng).
function keysFor(fn, segs) {
  if (!segs.length) return null;
  segs = segs.slice().sort((a, b) => a.t0 - b.t0);
  const end = Math.max(...segs.map(s => s.t1));
  if (segs.some((s, i) => i > 0 && s.t0 < segs[i - 1].t1 - EPS)) {
    // Lưới đều + mọi mép đoạn: chỗ một track bắt đầu/kết thúc là chỗ đường cong gãy, bỏ qua là cắt góc.
    const times = segs.flatMap(s => [s.t0, s.t1]);
    for (let t = segs[0].t0; t < end; t += 1 / CONFIG.sampleRate) times.push(t);
    times.sort((a, b) => a - b);
    const keys = [];
    for (const t of times) if (!keys.length || t - keys[keys.length - 1].time > EPS) keys.push({ time: t, value: fn(t), ease: LINEAR });
    delete keys[keys.length - 1].ease;
    return keys;
  }
  const keys = [];
  for (const s of segs) {
    const ez = s.ez.hold ? LINEAR : s.ez;
    const last = keys[keys.length - 1];
    if (last && Math.abs(last.time - s.t0) < EPS) last.ease = ez;
    else keys.push({ time: s.t0, value: fn(s.t0), ease: ez });
    if (s.ez.hold) keys.push({ time: s.t1 - 0.001, value: fn(s.t0), ease: LINEAR });   // HOLD: đứng rồi nhảy
    keys.push({ time: s.t1, value: fn(s.t1) });
  }
  return keys;
}

const r = (v, d) => Math.round(v * d) / d;
const tracks = [];
function emit(label, target, prop, keys, digits) {
  if (!keys || keys.length < 2) return;
  tracks.push({ name: `${label} · ${prop}`, target, property: prop,
                keys: keys.map(k => Object.assign({ time: r(k.time, 1e4), value: r(k.value, digits) }, k.ease ? { ease: easeName(k.ease) } : {})) });
}
const SUPPORTED = /^(TRANSLATION_(X|Y|XY)|SCALE_(X|Y|XY)|ROTATION|OPACITY)$/;

for (const n of [root, ...root.findAll(x => 'animations' in x && Object.keys(x.animations || {}).length > 0)]) {
  const F = fieldsOf(n);
  if (!Object.keys(F).length) continue;
  for (const k of Object.keys(n.animations || {})) if (!SUPPORTED.test(k)) warnings.push(`${n.name} (${n.id}): thuộc tính ${k} chưa hỗ trợ — bỏ qua.`);

  let target = CONFIG.targets[n.id];
  if (target === undefined) { target = n.name; warnings.push(`${n.name} (${n.id}) chưa có trong CONFIG.targets — tạm dùng tên layer.`); }
  const label = target === '' ? root.name : target;

  const L = mul(rootInv, lin(n.absoluteTransform));   // hướng layer so với group gốc (xoay + lật)
  const flipped = L[0][0] * L[1][1] - L[0][1] * L[1][0] < 0;
  if (!isAxisAligned(L)) warnings.push(`${n.name} (${n.id}) xoay góc lẻ so với group gốc — scale lấy theo trục gần nhất.`);

  // Translation: độ dời trên trục riêng của layer → trục của group gốc.
  const TX = F.TRANSLATION_X, TY = F.TRANSLATION_Y;
  const dx = t => TX ? valueAt(TX, t) - TX.base : 0, dy = t => TY ? valueAt(TY, t) - TY.base : 0;
  const pick = (a, b) => [...(Math.abs(a) > 1e-6 ? moving(TX) : []), ...(Math.abs(b) > 1e-6 ? moving(TY) : [])];
  emit(label, target, 'x', keysFor(t => L[0][0] * dx(t) + L[0][1] * dy(t), pick(L[0][0], L[0][1])), 1e3);
  emit(label, target, 'y', keysFor(t => L[1][0] * dx(t) + L[1][1] * dy(t), pick(L[1][0], L[1][1])), 1e3);

  // Scale: trục riêng → trục group gốc (xoay 90° đổi chỗ X/Y; lật không đổi độ lớn).
  const SX = F.SCALE_X, SY = F.SCALE_Y;
  const sx = t => SX ? valueAt(SX, t) / (SX.base || 1) : 1, sy = t => SY ? valueAt(SY, t) / (SY.base || 1) : 1;
  const xFromX = Math.abs(L[0][0]) >= Math.abs(L[0][1]);
  const kx = keysFor(xFromX ? sx : sy, moving(xFromX ? SX : SY));
  const ky = keysFor(xFromX ? sy : sx, moving(xFromX ? SY : SX));
  if (kx && ky && JSON.stringify(kx) === JSON.stringify(ky)) emit(label, target, 'scale', kx, 1e4);
  else { emit(label, target, 'scaleX', kx, 1e4); emit(label, target, 'scaleY', ky, 1e4); }

  // Xoay: layer bị lật thì chiều quay trên màn ngược dấu với số.
  const RO = F.ROTATION;
  emit(label, target, 'rotation', keysFor(t => (valueAt(RO, t) - RO.base) * (flipped ? -1 : 1), moving(RO)), 1e3);

  // Độ mờ: hệ số so với opacity author của layer (importer nhân lên alpha author bên Unity).
  const OP = F.OPACITY;
  emit(label, target, 'opacity', keysFor(t => valueAt(OP, t) / (OP.base || 1), moving(OP)), 1e4);
}

return {
  json: {
    schema: 'figma-motion/v1',
    name: CONFIG.name,
    source: `Figma · ${root.name} (node ${root.id}) — xuất bằng FigmaMotion/Exporter/export-figma-motion.js`,
    pxToUnit: CONFIG.pxToUnit,
    flipY: true,
    timeUnit: 's',
    mode: 'parallel',
    ease: 'ease-out',
    tracks,
  },
  warnings,
};
