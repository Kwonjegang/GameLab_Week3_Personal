const fs = require('fs');
const path = require('path');

const outDir = 'C:/KSW/GameLab_Week3_Personal/Assets/GeneratedModels/LowPolySunLounger';
fs.mkdirSync(outDir, { recursive: true });
const objPath = path.join(outDir, 'SunLounger_LowPoly.obj');
const mtlPath = path.join(outDir, 'SunLounger_LowPoly.mtl');

const verts = [];
const faces = [];

function rotZ([x, y, z], deg) {
  const r = deg * Math.PI / 180;
  const c = Math.cos(r);
  const s = Math.sin(r);
  return [x * c - y * s, x * s + y * c, z];
}

function addBox(name, center, size, mat = 'beige', rotDeg = 0) {
  const [cx, cy, cz] = center;
  const [sx, sy, sz] = size;
  const hx = sx / 2, hy = sy / 2, hz = sz / 2;
  const local = [
    [-hx, -hy, -hz], [ hx, -hy, -hz], [ hx, -hy,  hz], [-hx, -hy,  hz],
    [-hx,  hy, -hz], [ hx,  hy, -hz], [ hx,  hy,  hz], [-hx,  hy,  hz],
  ];

  const start = verts.length + 1;
  for (const p of local) {
    const [x, y, z] = rotZ(p, rotDeg);
    verts.push([cx + x, cy + y, cz + z]);
  }

  const quads = [
    [1,2,3,4],
    [5,8,7,6],
    [1,5,6,2],
    [2,6,7,3],
    [3,7,8,4],
    [4,8,5,1],
  ];

  for (const q of quads) {
    faces.push({ name, mat, idx: q.map(i => start + i - 1) });
  }
}

function addWedgeLeg(name, center, topSize, bottomSize, height, mat = 'beige') {
  const [cx, cy, cz] = center;
  const [tx, tz] = topSize;
  const [bx, bz] = bottomSize;
  const yTop = cy + height / 2;
  const yBottom = cy - height / 2;
  const local = [
    [-bx/2, yBottom, -bz/2], [ bx/2, yBottom, -bz/2], [ bx/2, yBottom,  bz/2], [-bx/2, yBottom,  bz/2],
    [-tx/2, yTop,    -tz/2], [ tx/2, yTop,    -tz/2], [ tx/2, yTop,     tz/2], [-tx/2, yTop,     tz/2],
  ];

  const start = verts.length + 1;
  for (const [x, y, z] of local) {
    verts.push([cx + x, y, cz + z]);
  }

  const quads = [
    [1,2,3,4],
    [5,8,7,6],
    [1,5,6,2],
    [2,6,7,3],
    [3,7,8,4],
    [4,8,5,1],
  ];

  for (const q of quads) {
    faces.push({ name, mat, idx: q.map(i => start + i - 1) });
  }
}

// Main proportions: long and narrow, low-poly, based on the reference image.
addBox('main_deck', [0.75, 0.72, 0], [5.25, 0.22, 1.55]);
addBox('front_frame', [3.45, 0.82, 0], [0.18, 0.34, 1.78]);
addBox('rear_frame', [-2.45, 0.82, 0], [0.18, 0.34, 1.78]);
addBox('left_side_frame', [0.55, 0.84, -0.86], [5.75, 0.32, 0.14]);
addBox('right_side_frame', [0.55, 0.84, 0.86], [5.75, 0.32, 0.14]);

for (let i = 0; i < 17; i++) {
  const x = -1.75 + i * 0.29;
  addBox(`seat_slat_${String(i + 1).padStart(2, '0')}`, [x, 0.99, 0], [0.055, 0.075, 1.43], 'slat_dark');
}

const angle = -52;
addBox('back_panel_base', [-2.35, 1.46, 0], [2.05, 0.20, 1.50], 'beige', angle);
addBox('back_left_rail', [-2.35, 1.46, -0.78], [2.15, 0.24, 0.13], 'beige', angle);
addBox('back_right_rail', [-2.35, 1.46, 0.78], [2.15, 0.24, 0.13], 'beige', angle);
addBox('back_top_rail', [-3.02, 2.28, 0], [0.16, 0.22, 1.55], 'beige', angle);
addBox('back_bottom_rail', [-1.68, 0.66, 0], [0.16, 0.22, 1.55], 'beige', angle);

for (let i = 0; i < 8; i++) {
  const local = -0.78 + i * 0.22;
  const cx = -2.35 + local * Math.cos(angle * Math.PI / 180);
  const cy = 1.46 + local * Math.sin(angle * Math.PI / 180);
  addBox(`back_slat_${String(i + 1).padStart(2, '0')}`, [cx, cy, 0], [0.045, 0.07, 1.30], 'slat_dark', angle);
}

for (const [name, x] of [['rear', -2.35], ['middle', 0.25], ['front', 3.18]]) {
  addWedgeLeg(`${name}_leg_left`, [x, 0.29, -0.67], [0.24, 0.23], [0.17, 0.17], 0.68);
  addWedgeLeg(`${name}_leg_right`, [x, 0.29, 0.67], [0.24, 0.23], [0.17, 0.17], 0.68);
}

addBox('back_support_left', [-1.83, 0.96, -0.67], [1.25, 0.10, 0.10], 'beige', -47);
addBox('back_support_right', [-1.83, 0.96, 0.67], [1.25, 0.10, 0.10], 'beige', -47);
addBox('left_hinge', [-1.15, 0.67, -0.72], [0.22, 0.10, 0.10], 'dark');
addBox('right_hinge', [-1.15, 0.67, 0.72], [0.22, 0.10, 0.10], 'dark');

const mtl = `newmtl beige
Kd 0.62 0.58 0.51
Ka 0.62 0.58 0.51
Ks 0.08 0.08 0.08
Ns 20

newmtl slat_dark
Kd 0.28 0.27 0.24
Ka 0.28 0.27 0.24
Ks 0.03 0.03 0.03
Ns 5

newmtl dark
Kd 0.08 0.07 0.06
Ka 0.08 0.07 0.06
Ks 0.02 0.02 0.02
Ns 5
`;
fs.writeFileSync(mtlPath, mtl, 'utf8');

const lines = [];
lines.push('# Low poly sun lounger generated for Unity');
lines.push('mtllib SunLounger_LowPoly.mtl');
for (const [x, y, z] of verts) {
  lines.push(`v ${x.toFixed(5)} ${y.toFixed(5)} ${z.toFixed(5)}`);
}

let lastName = null;
let lastMat = null;
for (const face of faces) {
  if (face.name !== lastName) {
    lines.push(`o ${face.name}`);
    lastName = face.name;
    lastMat = null;
  }
  if (face.mat !== lastMat) {
    lines.push(`usemtl ${face.mat}`);
    lastMat = face.mat;
  }
  lines.push(`f ${face.idx.join(' ')}`);
}

fs.writeFileSync(objPath, lines.join('\n') + '\n', 'utf8');
console.log(objPath);
console.log(mtlPath);
console.log(`vertices=${verts.length} faces=${faces.length} objects=${new Set(faces.map(f => f.name)).size}`);