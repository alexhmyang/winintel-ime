import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, writeFileSync, readFileSync, readdirSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';
import YAML from 'yaml';
test('desktop setup preserves custom schemas and appearance and is repeatable', () => {
  const dir = mkdtempSync(join(tmpdir(), 'winintel-config-test-'));
  try {
    writeFileSync(join(dir, 'default.custom.yaml'), 'patch:\n  schema_list:\n    - schema: luna_pinyin\n  menu/page_size: 5\n');
    writeFileSync(join(dir, 'weasel.custom.yaml'), 'patch:\n  style/font_point: 18\n');
    for (let i = 0; i < 2; i++) {
      const run = spawnSync(process.execPath, ['desktop/configure-ime.mjs', '--user-dir', dir], { encoding: 'utf8' });
      assert.equal(run.status, 0, run.stderr);
    }
    const defaults = YAML.parse(readFileSync(join(dir, 'default.custom.yaml'), 'utf8'));
    assert.deepEqual(defaults.patch.schema_list, [{ schema: 'winintel' }, { schema: 'luna_pinyin' }]);
    assert.equal(defaults.patch['menu/page_size'], 5);
    assert.equal(YAML.parse(readFileSync(join(dir, 'weasel.custom.yaml'), 'utf8')).patch['style/font_point'], 18);
    assert.ok(readFileSync(join(dir, 'winintel.schema.yaml'), 'utf8').includes('lua_filter@*winintel_desktop'));
    assert.equal(readdirSync(dir).filter(name => name.startsWith('winintel_backup_')).length, 2);
  } finally { rmSync(dir, { recursive: true, force: true }); }
});
test('desktop setup keeps malformed configuration intact', () => {
  const dir = mkdtempSync(join(tmpdir(), 'winintel-invalid-config-'));
  try {
    const source = 'patch: [invalid';
    writeFileSync(join(dir, 'default.custom.yaml'), source);
    const run = spawnSync(process.execPath, ['desktop/configure-ime.mjs', '--user-dir', dir], { encoding: 'utf8' });
    assert.notEqual(run.status, 0);
    assert.equal(readFileSync(join(dir, 'default.custom.yaml'), 'utf8'), source);
    assert.ok(!readdirSync(dir).includes('winintel.schema.yaml'));
  } finally { rmSync(dir, { recursive: true, force: true }); }
});
