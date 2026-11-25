// GPL-3.0-or-later. Preserve existing YAML structurally and keep a rollback copy.
import { readFileSync, writeFileSync, mkdirSync, existsSync, copyFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import YAML from 'yaml';
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const option = process.argv.indexOf('--user-dir');
if (option < 0 || !process.argv[option + 1]) throw new Error('--user-dir required');
const target = resolve(process.argv[option + 1]);
mkdirSync(target, { recursive: true });
const backup = join(target, `winintel_backup_${Date.now()}`);
mkdirSync(backup);
const put = (relative, content) => {
  const path = join(target, relative);
  mkdirSync(dirname(path), { recursive: true });
  if (existsSync(path)) { mkdirSync(dirname(join(backup, relative)), { recursive: true }); copyFileSync(path, join(backup, relative)); }
  writeFileSync(path, content, 'utf8');
};
// Parse configurations before changing any files: malformed YAML must not be discarded.
const load = name => existsSync(join(target, name)) ? YAML.parseDocument(readFileSync(join(target, name), 'utf8')) : new YAML.Document({ patch: {} });
const defaults = load('default.custom.yaml'), appearance = load('weasel.custom.yaml');
for (const doc of [defaults, appearance]) {
  if (doc.errors.length) throw new Error(doc.errors[0].message);
  if (!doc.contents) doc.contents = doc.createNode({ patch: {} });
  if (!doc.has('patch')) doc.set('patch', {});
  if (!YAML.isMap(doc.get('patch'))) throw new Error('Expected a YAML patch mapping');
}
const patch = defaults.get('patch');
const schemas = patch.get('schema_list');
if (YAML.isSeq(schemas)) {
  if (!schemas.items.some(row => YAML.isMap(row) && row.get('schema') === 'winintel')) schemas.items.unshift(defaults.createNode({ schema: 'winintel' }));
} else {
  const additions = patch.get('schema_list/+');
  if (YAML.isSeq(additions)) {
    if (!additions.items.some(row => YAML.isMap(row) && row.get('schema') === 'winintel')) additions.items.unshift(defaults.createNode({ schema: 'winintel' }));
  } else if (!patch.has('schema_list/+')) patch.set('schema_list/+', [{ schema: 'winintel' }]);
  else throw new Error('Expected schema_list/+ to be a list');
}
const style = appearance.get('patch');
style.set('style/color_scheme', 'winintel_blue');
style.set('preset_color_schemes/winintel_blue', {
  name: '数萌蓝', author: 'winintel AI', back_color: '0xfafafa', text_color: '0x353535',
  candidate_text_color: '0x353535', comment_text_color: '0x999999', hilited_back_color: '0xfafafa',
  hilited_candidate_back_color: '0xd8711c', hilited_candidate_text_color: '0xffffff',
  border_color: '0xe8e8e8', label_color: '0x888888', hilited_label_color: '0xffffff'
});
for (const file of ['winintel.schema.yaml', 'winintel.dict.yaml', 'winintel_english.dict.yaml']) {
  let body = readFileSync(join(root, 'rime', file), 'utf8');
  if (file.endsWith('schema.yaml')) body = body.replace('lua_filter@*winintel_ai', 'lua_filter@*winintel_desktop');
  put(file, body);
}
for (const file of ['8105.dict.yaml', 'base.dict.yaml', 'en.dict.yaml', 'en_ext.dict.yaml']) put(file, readFileSync(join(root, 'data', 'rime-ice', file), 'utf8'));
const rows = readFileSync(join(root, 'data', 'starter.tsv'), 'utf8').split(/\r?\n/).filter(line => /^[^#].*\t[a-z ]+\t/.test(line) && /[\u3400-\u9fff]/.test(line));
put('winintel_starter.dict.yaml', `---\nname: winintel_starter\nversion: "0.1.0"\nsort: by_weight\n...\n${rows.join('\n')}\n`);
put('lua/winintel_desktop.lua', readFileSync(join(root, 'desktop', 'winintel_desktop.lua'), 'utf8'));
put('winintel_english.schema.yaml', readFileSync(join(root, 'desktop', 'winintel_english.schema.yaml'), 'utf8'));
mkdirSync(join(target, 'winintel_bridge'), { recursive: true });
put('default.custom.yaml', defaults.toString());
put('weasel.custom.yaml', appearance.toString());
console.log(`Configured winintel schema; backup: ${backup}`);
