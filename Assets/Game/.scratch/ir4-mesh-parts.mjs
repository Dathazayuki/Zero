import fs from 'node:fs';

function parseObj(file) {
  const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
  const vertices = [null], faces = [], keys = new Map(), parents = [];
  let group = 'Mesh';
  for (let line = 0; line < lines.length; line++) {
    const fields = lines[line].trim().split(/\s+/);
    if (fields[0] === 'o') group = fields[1];
    if (fields[0] === 'v') vertices.push({xyz: fields.slice(1, 4).map(Number), line});
    if (fields[0] !== 'f') continue;
    const ids = fields.slice(1).map(token => Number(token.split('/')[0]));
    const nodes = ids.map(id => {
      const key = group + ':' + vertices[id].xyz.join(',');
      if (!keys.has(key)) { keys.set(key, parents.length); parents.push(parents.length); }
      return keys.get(key);
    });
    for (const node of nodes) parents[root(parents, node)] = root(parents, nodes[0]);
    faces.push({group, ids, tokens: fields.slice(1), line, node: nodes[0]});
  }
  const uvs = [null, ...lines.filter(line => line.startsWith('vt ')).map(line => line.trim().split(/\s+/).slice(1).map(Number))];
  return {file, lines, vertices, faces, parents, uvs};
}

function root(parents, id) {
  while (parents[id] !== id) { parents[id] = parents[parents[id]]; id = parents[id]; }
  return id;
}

function partsOf(mesh) {
  const parts = new Map();
  for (const face of mesh.faces) {
    const id = root(mesh.parents, face.node);
    if (!parts.has(id)) parts.set(id, {group: face.group, ids: new Set(), faces: []});
    const part = parts.get(id);
    face.ids.forEach(index => part.ids.add(index));
    part.faces.push(face.line);
  }
  return [...parts.values()].map((part, index) => {
    const xyz = [...part.ids].map(id => mesh.vertices[id].xyz);
    const min = [0, 1, 2].map(axis => Math.min(...xyz.map(v => v[axis])));
    const max = [0, 1, 2].map(axis => Math.max(...xyz.map(v => v[axis])));
    return {...part, index, min, max};
  });
}

function transferQuadFace(mesh, face, lookup, replacements, removed) {
  const key = id => mesh.vertices[id].xyz.map(n => n.toFixed(6)).join(',');
  const corners = face.keys, allowed = new Set(corners);
  const candidates = [...new Set(corners.flatMap(c => lookup.get(c) || []))];
  const pair = candidates.filter(f => f.ids.every(id => allowed.has(key(id))));
  if (pair.length !== 2) throw new Error('Quad does not match two triangles');
  const tokens = new Map();
  for (const f of pair) for (let i = 0; i < f.ids.length; i++) {
    const k = key(f.ids[i]), previous = tokens.get(k), current = f.tokens[i];
    const uv = token => mesh.uvs[Number(token.split('/')[1])]?.join(',');
    if (previous && uv(previous) !== uv(current)) return false;
    tokens.set(k, current);
  }
  replacements.set(pair[0].line, 'f ' + corners.map(c => tokens.get(c)).join(' '));
  removed.add(pair[1].line);
  return true;
}

function restoreQuads(file, originalFile) {
  const backup = '.scratch/IR4_BeforeRepair/' + file.split('/').pop() + '.refined.bak';
  const mesh = parseObj(fs.existsSync(backup) ? backup : file), source = parseObj(originalFile), lookup = new Map();
  const key = xyz => xyz.map(n => n.toFixed(6)).join(',');
  for (const face of mesh.faces) for (const id of face.ids) {
    const k = key(mesh.vertices[id].xyz);
    if (!lookup.has(k)) lookup.set(k, []);
    lookup.get(k).push(face);
  }
  const replacements = new Map(), removed = new Set();
  let seamQuads = 0;
  for (const face of source.faces) {
    if (face.ids.length === 3) continue;
    if (face.ids.length !== 4) throw new Error('Unexpected original polygon');
    face.keys = face.ids.map(id => key(source.vertices[id].xyz));
    if (!transferQuadFace(mesh, face, lookup, replacements, removed)) seamQuads++;
  }
  const triangles = source.faces.filter(face => face.ids.length === 3).length;
  if (removed.size * 2 + seamQuads*2 + triangles !== mesh.faces.length) throw new Error('Incomplete topology transfer');
  if (!fs.existsSync(backup)) fs.copyFileSync(file, backup);
  const lines = mesh.lines.map((line, i) => replacements.get(i) || line).filter((_, i) => !removed.has(i));
  fs.writeFileSync(file, lines.join('\n'));
  console.log(JSON.stringify({file, quads: removed.size, triangles: triangles+seamQuads*2, preservesGeometry: true, preservesUV: true, backup}));
}

function repairObj(mesh, removed, transforms = {}) {
  const parts = partsOf(mesh), removeLines = new Set();
  for (const part of parts) {
    if (removed.includes(part.index)) part.faces.forEach(line => removeLines.add(line));
    if (!transforms[part.index]) continue;
    for (const id of part.ids) {
      const vertex = mesh.vertices[id];
      mesh.lines[vertex.line] = 'v ' + transforms[part.index](vertex.xyz).map(n => n.toFixed(5)).join(' ');
    }
  }
  const backup = '.scratch/IR4_BeforeRepair/' + mesh.file.split('/').pop() + '.bak';
  fs.mkdirSync('.scratch/IR4_BeforeRepair', {recursive: true});
  if (fs.existsSync(backup)) throw new Error('Already repaired: ' + mesh.file);
  fs.copyFileSync(mesh.file, backup);
  fs.writeFileSync(mesh.file, mesh.lines.filter((_, line) => !removeLines.has(line)).join('\n'));
  console.log(JSON.stringify({file: mesh.file, removedFaces: removeLines.size, backup}));
}

if (process.argv[2] === '--separate-tray') {
  const mesh = parseObj('Models/InfinityRoom4Demo/IR4_ChoppingStation.obj');
  const parts = partsOf(mesh).filter(part => part.group === 'Metal' && part.max[0] < -.34);
  if (parts.length !== 9) throw new Error('Unexpected tray geometry');
  for (const part of parts) for (const id of part.ids) {
    const v = mesh.vertices[id];
    mesh.lines[v.line] = 'v ' + [v.xyz[0]-.04, v.xyz[1], v.xyz[2]].map(n => n.toFixed(5)).join(' ');
  }
  fs.writeFileSync(mesh.file, mesh.lines.join('\n'));
  console.log(JSON.stringify({file: mesh.file, trayShift: -.04, backup: '.scratch/IR4_BeforeRepair/IR4_ChoppingStation.obj.bak'}));
  process.exit(0);
}

if (process.argv[2] === '--publish-quad-source') {
  const base = 'Models/Meshy/HotelKitchen/QuadSource/';
  for (const name of ['ChefKnife','FoldedTowel','UtensilCrock','BowlStack','SoapDispenser','PantryJar'])
    fs.copyFileSync(base+'Original/'+name+'.obj', base+name+'.obj');
  process.exit(0);
}

if (process.argv[2] === '--restore-quads') {
  const base = 'Models/Meshy/HotelKitchen/QuadSource/';
  for (const name of ['ChefKnife','FoldedTowel','UtensilCrock','BowlStack','SoapDispenser','PantryJar'])
    restoreQuads(base+name+'.obj', base+'Original/'+name+'.obj');
  process.exit(0);
}

if (process.argv[2] === '--summary') {
  for (const file of process.argv.slice(3)) {
    const mesh = parseObj(file), counts = {};
    mesh.faces.forEach(face => { counts[face.ids.length] = (counts[face.ids.length] || 0)+1; });
    const xyz = mesh.vertices.slice(1).map(v => v.xyz);
    const min = [0,1,2].map(axis => Math.min(...xyz.map(v => v[axis])));
    const max = [0,1,2].map(axis => Math.max(...xyz.map(v => v[axis])));
    console.log(JSON.stringify({file, faces: mesh.faces.length, counts, min, max}));
  }
  process.exit(0);
}

if (process.argv[2] === '--repair') {
  const base = 'Models/InfinityRoom4Demo/';
  repairObj(parseObj(base + 'IR4_DoubleSink.obj'), [27], {20: ([x,y,z]) => [z-.3,.94+(y-.94)*.72,.12-x]});
  repairObj(parseObj(base + 'IR4_ChoppingStation.obj'), [4,5,15], {
    1: ([x,y,z]) => [x,y-.0195,z], 2: ([x,y,z]) => [x,y-.0195,z], 3: ([x,y,z]) => [x,y-.0195,z]
  });
  repairObj(parseObj(base + 'IR4_PrepIsland.obj'), [45,63]);
  repairObj(parseObj(base + 'IR4_PlateStation.obj'), Array.from({length: 20}, (_, i) => i+9));
  process.exit(0);
}

for (const file of process.argv.slice(2)) {
  const mesh = parseObj(file);
  const report = partsOf(mesh).map(({ids, faces, ...part}) => ({...part, vertices: ids.size, faces: faces.length}));
  console.log(JSON.stringify({file, parts: report}));
}
