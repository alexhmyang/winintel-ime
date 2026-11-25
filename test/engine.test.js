import test from 'node:test';
import assert from 'node:assert/strict';
import { Engine, normalize } from '../src/engine.js';

const engine = new Engine({ persist: false });
test('large bilingual dictionary is shipped and parsed', () => { assert.ok(engine.count > 500000); });
for (const [input, expected] of [
  ['nihao', '你好'], ['zhongguo', '中国'], ['rgzn', '人工智能'], ['srf', '输入法'],
  ['perfromance', 'performance'], ['nihaoo', '你好'], ['sevrer', 'server'],
  ['github', 'GitHub'], ['HELLO', 'Hello'], ["xi'an", '西安'], ['nv', '女']
]) {
  test(`candidate recall: ${input}`, () => assert.ok(engine.query(input).candidates.some(c => c.text === expected)));
}
test('long unseen pinyin is segmented into a sentence', () => {
  assert.ok(engine.query('woaizhongguo').candidates.some(c => c.text === '我爱中国'));
});
test('learning accepts candidate choices and rejects arbitrary text', () => {
  assert.equal(engine.learn('nihao', '<script>secret</script>'), false);
  assert.equal(engine.learn('nihao', '你好'), true);
  assert.equal(engine.learned['nihao\t你好'], 1);
  engine.clearLearning(); assert.deepEqual(engine.learned, {});
});
test('normalization handles umlaut and bounds long pasted input', () => {
  assert.equal(normalize('NÜ'), 'nv'); assert.equal(normalize('nu:'), 'nv');
  assert.equal(normalize('a'.repeat(100000)).length, 64);
  assert.ok(engine.query('a'.repeat(100000)).candidates.length <= 9);
});
test('literal unknown input is preserved and predictions work', () => {
  assert.equal(engine.query('zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz').candidates[0].kind, 'raw');
  assert.equal(engine.query('', 'artificial').candidates[0].text, 'intelligence');
});
