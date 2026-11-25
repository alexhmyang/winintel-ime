import { existsSync, mkdirSync, readFileSync } from 'node:fs';
import { join, resolve, dirname } from 'node:path';
import { homedir } from 'node:os';
import { fileURLToPath } from 'node:url';

export const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
if (existsSync(join(ROOT, '.env'))) process.loadEnvFile(join(ROOT, '.env'));
export const DATA_DIR = process.env.WININTEL_DATA_DIR ||
  join(process.env.LOCALAPPDATA || join(homedir(), '.local', 'share'), 'winintel-ime');
export function ensureDataDir() { mkdirSync(DATA_DIR, { recursive: true }); return DATA_DIR; }
export function readJson(path, fallback) {
  try { return JSON.parse(readFileSync(path, 'utf8')); } catch { return fallback; }
}
export const config = {
  port: Number(process.env.PORT || 8787),
  aiEnabled: process.env.AI_ENABLED === '1',
  ollamaUrl: process.env.OLLAMA_URL || 'http://127.0.0.1:11434',
  ollamaModel: process.env.OLLAMA_MODEL || 'qwen3:0.6b',
  aiTimeout: Math.min(3000, Math.max(100, Number(process.env.AI_TIMEOUT_MS || 650))),
  asrDir: resolve(ROOT, process.env.ASR_MODEL_DIR || 'models/voice'),
  asrThreads: Math.min(8, Math.max(1, Number(process.env.ASR_THREADS || 2))),
  asrProvider: process.env.ASR_PROVIDER || 'cpu'
};
