import { execFileSync } from 'node:child_process';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from '../src/config.js';

const ref = '927e1cf1d3eeb9bf4be85e3f684dba4dd485b6f6';
const destination = join(ROOT, 'data', 'rime-ice');
mkdirSync(destination, { recursive: true });
const files = ['cn_dicts/8105.dict.yaml', 'cn_dicts/base.dict.yaml', 'en_dicts/en.dict.yaml', 'en_dicts/en_ext.dict.yaml', 'LICENSE'];
const manifest = { repository: 'https://github.com/iDvel/rime-ice', revision: ref, files: [] };
for (const path of files) {
  const meta = JSON.parse(execFileSync('gh', ['api', `repos/iDvel/rime-ice/contents/${path}?ref=${ref}`], { maxBuffer: 40 * 1024 * 1024 }));
  const blob = meta.content ? meta : JSON.parse(execFileSync('gh', ['api', `repos/iDvel/rime-ice/git/blobs/${meta.sha}`], { maxBuffer: 40 * 1024 * 1024 }));
  const bytes = Buffer.from(blob.content, 'base64');
  if (bytes.length !== meta.size) throw new Error(`Size mismatch: ${path}`);
  const output = path === 'LICENSE' ? join(ROOT, 'LICENSE') : join(destination, path.split('/').at(-1));
  writeFileSync(output, bytes);
  manifest.files.push({ path, sha: meta.sha, size: meta.size });
  console.log(`Saved ${path}: ${bytes.length} bytes`);
}
writeFileSync(join(destination, 'provenance.json'), JSON.stringify(manifest, null, 2) + '\n');
