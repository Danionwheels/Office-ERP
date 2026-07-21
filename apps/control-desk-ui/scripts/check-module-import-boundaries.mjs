import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../src/modules');
const baselinePath = path.resolve(root, '../../../tests/frontend-module-private-imports.txt');
const files = [];
function walk(dir) { for (const e of fs.readdirSync(dir, {withFileTypes:true})) { const p=path.join(dir,e.name); if(e.isDirectory()) walk(p); else if(/\.(tsx?|jsx?)$/.test(e.name)) files.push(p); } }
walk(root);
const violations = [];
const publicExportViolations = [];
for (const file of files) {
  const sourceModule = path.relative(root, file).split(path.sep)[0];
  const text = fs.readFileSync(file, 'utf8');
  const re = /(?:from\s+|import\s*\(\s*)(['"])([^'"\n]+)\1/g;
  for (const m of text.matchAll(re)) {
    const spec = m[2];
    const match = spec.match(/^\.\.\/([^/]+)(?:\/(.*))?$/);
    if (!match || match[1] === sourceModule) continue;
    const targetModuleRoot = path.join(root, match[1]);
    if (!match[2] || match[2] === 'index') {
      if (!['.ts','.tsx'].some(ext => fs.existsSync(path.join(targetModuleRoot, `index${ext}`))))
        publicExportViolations.push(`${sourceModule} -> ${match[1]} (${spec})`);
      continue;
    }
    if (!['api','types','components','hooks','utils','mappers','pages'].includes(match[2].split('/')[0])) continue;
    const rel = path.relative(path.resolve(root, '..', sourceModule), file).replaceAll('\\','/');
    violations.push(`${sourceModule}|${match[1]}|${path.relative(path.resolve(root, '../../../'), file).replaceAll('\\','/')}|${spec}`);
  }
}
if (publicExportViolations.length) { console.error('Frontend public export missing:\n' + publicExportViolations.join('\n')); process.exit(1); }
violations.sort();
if (process.argv.includes('--write-baseline')) { fs.mkdirSync(path.dirname(baselinePath), {recursive:true}); fs.writeFileSync(baselinePath, '# Current frontend private imports\n'+[...new Set(violations)].join('\n')+'\n'); process.exit(0); }
const expected = fs.readFileSync(baselinePath,'utf8').split(/\r?\n/).filter(x=>x && !x.startsWith('#')).sort();
const actual = [...new Set(violations)].sort();
const added = actual.filter(x=>!expected.includes(x)); const removed=expected.filter(x=>!actual.includes(x));
if (added.length || removed.length) { console.error('Frontend private-import baseline changed.', {added, removed}); process.exit(1); }
console.log(`Frontend module import baseline passed (${actual.length} entries).`);
