import { existsSync, readFileSync, writeFileSync, renameSync, mkdirSync, rmSync } from 'node:fs';
import { join } from 'node:path';
import { homedir } from 'node:os';
import { LocalAI } from './ai.js';
import { config } from './config.js';

const defaultDir = process.platform === 'win32' ? join(process.env.APPDATA, 'Rime') :
  process.platform === 'darwin' ? join(homedir(), 'Library', 'Rime') : join(homedir(), '.config', 'ibus', 'rime');
const directory = join(process.env.RIME_USER_DIR || defaultDir, 'winintel_bridge');
mkdirSync(directory, { recursive: true });
const ai = new LocalAI();
let previous = '', busy = false;
console.log(`Rime AI bridge: ${directory}. AI ${config.aiEnabled ? 'enabled' : 'disabled; enable AI_ENABLED=1 in .env'}`);
setInterval(async () => {
  const request = join(directory, 'request.tsv');
  if (!existsSync(request) || busy) return;
  let snapshot;
  try { snapshot = readFileSync(request, 'utf8'); } catch { return; }
  if (snapshot === previous || snapshot.length > 8192) return;
  previous = snapshot; busy = true;
  try {
    const [input, context, ...texts] = snapshot.trimEnd().split(/\r?\n/);
    const candidates = texts.slice(0, 9).map(text => ({ text }));
    const ranked = await ai.rank(input, context, candidates);
    if (readFileSync(request, 'utf8') !== snapshot) return;
    writeFileSync(join(directory, 'response.tmp'), [input, context, ...ranked.candidates.map(c => c.text)].join('\n') + '\n', { mode: 0o600 });
    renameSync(join(directory, 'response.tmp'), join(directory, 'response.tsv'));
  } catch (error) { console.error(`Bridge: ${error.message}`); } finally { busy = false; }
}, 30);
const clean = () => {
  for (const file of ['request.tsv', 'response.tsv', 'request.tmp', 'response.tmp']) rmSync(join(directory, file), { force: true });
  process.exit();
};
process.on('SIGINT', clean); process.on('SIGTERM', clean);
