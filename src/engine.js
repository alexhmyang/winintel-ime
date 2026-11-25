import { readFileSync, existsSync, readdirSync, writeFileSync, renameSync } from 'node:fs';
import { join } from 'node:path';
import { performance } from 'node:perf_hooks';
import { ROOT, DATA_DIR, ensureDataDir, readJson } from './config.js';

export function normalize(input) {
  return String(input || '').normalize('NFKC').toLowerCase().replace(/ü/g, 'v')
    .replace(/u:/g, 'v').replace(/[^a-z]/g, '').slice(0, 64);
}
const lowerBound = (arr, target) => {
  let lo = 0, hi = arr.length;
  while (lo < hi) { const mid = (lo + hi) >>> 1; if (arr[mid] < target) lo = mid + 1; else hi = mid; }
  return lo;
};
export function editVariants(code) {
  const result = new Set(), alphabet = 'abcdefghijklmnopqrstuvwxyz';
  for (let i = 0; i < code.length; i++) {
    result.add(code.slice(0, i) + code.slice(i + 1));
    if (i + 1 < code.length) result.add(code.slice(0, i) + code[i + 1] + code[i] + code.slice(i + 2));
    for (const ch of alphabet) {
      if (ch !== code[i]) result.add(code.slice(0, i) + ch + code.slice(i + 1));
      result.add(code.slice(0, i) + ch + code.slice(i));
    }
  }
  for (const ch of alphabet) result.add(code + ch);
  result.delete(code); result.delete('');
  return result;
}

export class Engine {
  constructor({ persist = true, extraPaths = [], fullDictionary = true } = {}) {
    this.exact = new Map(); this.abbr = new Map(); this.follow = new Map();
    this.persist = persist; this.learnFile = join(DATA_DIR, 'learned.json');
    this.learned = persist ? readJson(this.learnFile, {}) : {};
    this.count = 0;
    this.load(join(ROOT, 'data', 'starter.tsv'), true);
    const dictDir = join(ROOT, 'data', 'rime-ice');
    if (fullDictionary && existsSync(dictDir)) {
      for (const file of readdirSync(dictDir).filter(f => f.endsWith('.dict.yaml'))) this.load(join(dictDir, file));
    }
    for (const file of extraPaths) this.load(file, true);
    this.keys = [...this.exact.keys()].sort();
  }
  load(path, starter = false) {
    let body = readFileSync(path, 'utf8'), data = !path.endsWith('.yaml');
    for (const line of body.split(/\r?\n/)) {
      if (!data) { if (line.trim() === '...') data = true; continue; }
      if (!line || line.startsWith('#')) continue;
      const [text, spelling, frequency] = line.split('\t');
      if (!text || !spelling || text.length > 64) continue;
      const code = normalize(spelling);
      if (!code) continue;
      const row = { text, code, score: Math.log2(Math.max(1, Number(frequency) || 100)) + (starter ? 12 : 0) };
      this.add(this.exact, code, row, 12);
      const initials = spelling.trim().split(/\s+/).map(s => s[0]).join('');
      if (initials.length > 1 && initials !== code) this.add(this.abbr, initials, row, 8);
      // A bounded lexical continuation table; does not record surrounding application text.
      if (starter && /[\u3400-\u9fff]/.test(text)) {
        for (let i = 1; i < text.length; i++) this.add(this.follow, text.slice(0, i), { ...row, text: text.slice(i) }, 8);
      }
      this.count++;
    }
  }
  add(map, code, row, limit) {
    const rows = map.get(code) || [];
    const duplicate = rows.find(r => r.text === row.text);
    if (duplicate) { duplicate.score = Math.max(duplicate.score, row.score); return; }
    rows.push(row); rows.sort((a, b) => b.score - a.score);
    if (rows.length > limit) rows.pop();
    map.set(code, rows);
  }
  query(input, context = '', limit = 9) {
    const started = performance.now(), code = normalize(input);
    limit = Math.max(1, Math.min(9, Number(limit) || 9));
    if (!code) return { input, code, candidates: this.predict(context).slice(0, limit), latencyMs: performance.now() - started };
    const found = new Map();
    const collect = (rows, kind, penalty = 0) => {
      for (const row of rows || []) {
        let score = row.score - penalty + Math.min(20, 3 * Math.log2(1 + (this.learned[`${code}\t${row.text}`] || 0)));
        if (context && row.text.startsWith(context.slice(-4))) score += 1;
        if (/[A-Z]/.test(input[0]) && /^[a-z]/.test(row.text)) score += 2;
        const previous = found.get(row.text);
        if (!previous || score > previous.score) found.set(row.text, { text: row.text, code: row.code, kind, score });
      }
    };
    collect(this.exact.get(code), 'exact');
    collect(this.abbr.get(code), 'abbreviation', 3);
    // Binary search instead of a multi-million-node prefix trie.
    if (code.length >= 2) {
      let scanned = 0;
      for (let i = lowerBound(this.keys, code); i < this.keys.length && this.keys[i].startsWith(code) && scanned++ < 128; i++) {
        if (this.keys[i] !== code) collect(this.exact.get(this.keys[i]), 'completion', 8 + Math.min(6, this.keys[i].length - code.length));
      }
    }
    // Hard bounds prevent long pasted strings monopolizing the keystroke thread.
    if (code.length >= 3 && code.length <= 24) {
      for (const variant of editVariants(code)) collect(this.exact.get(variant), 'correction', 12);
      const fuzzy = code.replace(/zh/g, 'z').replace(/ch/g, 'c').replace(/sh/g, 's');
      if (fuzzy !== code) collect(this.exact.get(fuzzy), 'fuzzy', 14);
    }
    if (!found.size && code.length <= 48) {
      const sentence = this.segment(code);
      if (sentence) found.set(sentence.text, { ...sentence, kind: 'sentence' });
    }
    let candidates = [...found.values()].sort((a, b) => b.score - a.score).slice(0, limit);
    if (!candidates.length) candidates = [{ text: String(input).slice(0, 64), code, kind: 'raw', score: 0 }];
    // Preserve user's English capitalisation without altering branded dictionary entries.
    if (/^[A-Z]/.test(input)) candidates = candidates.map(c =>
      /^[a-z][a-z ]+$/.test(c.text) ? { ...c, text: c.text[0].toUpperCase() + c.text.slice(1) } : c);
    return { input, code, candidates, latencyMs: performance.now() - started };
  }
  segment(code) {
    const dp = Array(code.length + 1).fill(null);
    dp[0] = { text: '', code, score: 0 };
    for (let end = 1; end <= code.length; end++) {
      for (let start = Math.max(0, end - 24); start < end; start++) {
        if (!dp[start]) continue;
        const rows = this.exact.get(code.slice(start, end));
        const row = rows?.find(r => /[\u3400-\u9fff]/.test(r.text));
        if (!row) continue;
        const score = dp[start].score + row.score - 34;
        if (!dp[end] || score > dp[end].score) dp[end] = { text: dp[start].text + row.text, code, score };
      }
    }
    return dp[code.length];
  }
  predict(context) {
    for (let len = Math.min(12, context.length); len > 0; len--) {
      const rows = this.follow.get(context.slice(-len));
      if (rows) return rows.map(r => ({ ...r, kind: 'prediction' }));
    }
    const last = context.toLowerCase().trim().split(/\s+/).at(-1);
    const phrases = { hello: ['world'], artificial: ['intelligence'], language: ['model'], thank: ['you'], winintel: ['AI'] };
    return (phrases[last] || []).map(text => ({ text, code: text.toLowerCase(), kind: 'prediction', score: 1 }));
  }
  learn(input, text) {
    const code = normalize(input);
    // Only count an actual candidate choice. Never silently acquire arbitrary app text.
    if (!code || !this.query(input).candidates.some(c => c.text === text)) return false;
    const key = `${code}\t${text}`;
    this.learned[key] = Math.min(1000, (this.learned[key] || 0) + 1);
    const keys = Object.keys(this.learned);
    if (keys.length > 10000) delete this.learned[keys[0]];
    if (this.persist) {
      ensureDataDir();
      writeFileSync(`${this.learnFile}.tmp`, JSON.stringify(this.learned), { mode: 0o600 });
      renameSync(`${this.learnFile}.tmp`, this.learnFile);
    }
    return true;
  }
  clearLearning() {
    this.learned = {};
    if (this.persist) { ensureDataDir(); writeFileSync(this.learnFile, '{}', { mode: 0o600 }); }
  }
}
