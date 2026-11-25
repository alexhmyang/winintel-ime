// GPL-3.0-or-later. Download pinned official model files with resumable ranges.
import { existsSync, mkdirSync, statSync, readFileSync, appendFileSync, renameSync, rmSync, writeFileSync, createReadStream } from 'node:fs';
import { join } from 'node:path';
import { createHash } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { tmpdir } from 'node:os';
import { config } from '../src/config.js';
const repository = 'csukuangfj/sherpa-onnx-streaming-zipformer-bilingual-zh-en-2023-02-20';
const revision = '98590b7ed6443e77b714204da2757d75e1a642f4';
const host = process.env.HF_ENDPOINT || 'https://huggingface.co';
if (!['https://huggingface.co', 'https://hf-mirror.com'].includes(host)) throw new Error('Unsupported model host');
mkdirSync(config.asrDir, { recursive: true });
async function digest(file) {
  const hash = createHash('sha256'); for await (const chunk of createReadStream(file)) hash.update(chunk); return hash.digest('hex');
}
async function tree(path = '') {
  const response = await fetch(`${host}/api/models/${repository}/tree/${revision}${path ? `/${path}` : ''}`, { signal: AbortSignal.timeout(30000) });
  if (!response.ok) throw new Error(`Model metadata: HTTP ${response.status}`); return response.json();
}
const names = ['encoder-epoch-99-avg-1.int8.onnx', 'decoder-epoch-99-avg-1.int8.onnx', 'joiner-epoch-99-avg-1.int8.onnx', 'tokens.txt', 'README.md'];
const manifestPath = join(config.asrDir, 'model-manifest.json');
if (existsSync(manifestPath)) {
  const cached = JSON.parse(readFileSync(manifestPath, 'utf8'));
  if (cached.repository === repository && cached.revision === revision &&
      names.every(name => cached.files.some(row => row.path === name)) &&
      existsSync(join(config.asrDir, 'test_wavs', '0.wav'))) {
    let verified = true;
    for (const row of cached.files) {
      const file = join(config.asrDir, row.path);
      if (!existsSync(file) || statSync(file).size !== row.size || (row.sha256 && await digest(file) !== row.sha256)) { verified = false; break; }
    }
    if (verified) { console.log('Pinned model cache verified; no download needed.'); process.exit(0); }
  }
}
const models = (await tree()).filter(row => names.includes(row.path));
if (models.length !== names.length) throw new Error('Pinned model layout mismatch');
for (const file of [...models, ...(await tree('test_wavs')).filter(row => row.path === 'test_wavs/0.wav')]) {
  const target = join(config.asrDir, file.path);
  mkdirSync(join(target, '..'), { recursive: true });
  if (existsSync(target) && statSync(target).size === file.size && (!file.lfs || await digest(target) === file.lfs.oid)) { console.log(`Verified ${file.path}`); continue; }
  const partial = `${target}.part`;
  let offset = existsSync(partial) ? statSync(partial).size : 0;
  while (offset < file.size) {
    const end = Math.min(file.size - 1, offset + 4 * 1024 * 1024 - 1);
    const chunk = join(tmpdir(), `winintel-asr-${process.pid}.chunk`);
    const download = spawnSync('curl.exe', ['--silent', '--show-error', '--location', '--fail', '--max-time', '120', '--range', `${offset}-${end}`, '--output', chunk, `${host}/${repository}/resolve/${revision}/${file.path}`], { stdio: 'inherit' });
    if (download.status !== 0 || statSync(chunk).size !== end - offset + 1) throw new Error('Model range download failed; rerun to resume');
    appendFileSync(partial, readFileSync(chunk)); rmSync(chunk); offset = statSync(partial).size;
    console.log(`${file.path}: ${Math.round(offset / file.size * 100)}%`);
  }
  if (statSync(partial).size !== file.size || (file.lfs && await digest(partial) !== file.lfs.oid)) throw new Error(`Model checksum mismatch; remove ${partial}`);
  renameSync(partial, target);
}
writeFileSync(join(config.asrDir, 'model-manifest.json'), JSON.stringify({ repository, revision, source: host, files: models.map(row => ({ path: row.path, size: row.size, sha256: row.lfs?.oid })) }, null, 2));
