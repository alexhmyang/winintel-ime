import http from 'node:http';
import { readFileSync, existsSync, writeFileSync } from 'node:fs';
import { join, extname } from 'node:path';
import { randomBytes, timingSafeEqual, randomUUID } from 'node:crypto';
import { pathToFileURL } from 'node:url';
import { Worker } from 'node:worker_threads';
import { WebSocketServer } from 'ws';
import { Engine } from './engine.js';
import { LocalAI } from './ai.js';
import { config, ROOT, ensureDataDir } from './config.js';

export function createApp({ engine = new Engine(), ai = new LocalAI(), speech = true } = {}) {
  const token = randomBytes(32).toString('hex');
  const connections = new Map();
  let speechStatus = { ready: false, error: 'Speech model is loading' };
  const worker = speech ? new Worker(new URL('./speech-worker.js', import.meta.url)) : null;
  const emit = (ws, data) => { if (ws.readyState === 1) ws.send(JSON.stringify(data)); };
  worker?.on('message', message => {
    if (message.type === 'status') { speechStatus = message; return; }
    const state = connections.get(message.id);
    if (!state) return;
    if (message.type === 'ack') { state.pending--; return; }
    emit(state.ws, message);
    if (message.type === 'error' || message.finished) state.active = false;
  });
  worker?.on('error', error => {
    speechStatus = { ready: false, error: error.message };
    for (const state of connections.values()) emit(state.ws, { type: 'error', error: 'Speech worker stopped' });
  });
  const authenticated = req => {
    const supplied = req.headers['x-winintel-token'] || '';
    return typeof supplied === 'string' && supplied.length === token.length &&
      timingSafeEqual(Buffer.from(supplied), Buffer.from(token));
  };
  const sameSite = req => {
    const expected = `http://${req.headers.host}`;
    return (!req.headers.origin || req.headers.origin === expected) &&
      !['cross-site', 'same-site'].includes(req.headers['sec-fetch-site']);
  };
  const send = (res, status, data) => {
    res.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
    res.end(JSON.stringify(data));
  };
  const readBody = async req => {
    let size = 0, chunks = [];
    for await (const chunk of req) {
      size += chunk.length;
      if (size > 4096) throw new Error('Request exceeds 4 KB');
      chunks.push(chunk);
    }
    return JSON.parse(Buffer.concat(chunks).toString('utf8') || '{}');
  };
  const server = http.createServer(async (req, res) => {
    const address = server.address();
    const allowedHosts = [`127.0.0.1:${address?.port}`, `localhost:${address?.port}`];
    if (!allowedHosts.includes(req.headers.host) || !sameSite(req)) return send(res, 403, { error: 'Loopback same-origin requests only' });
    res.setHeader('x-content-type-options', 'nosniff');
    res.setHeader('content-security-policy', "default-src 'self'; connect-src 'self'; style-src 'self'; script-src 'self'; worker-src 'self'; frame-ancestors 'none'");
    res.setHeader('referrer-policy', 'no-referrer');
    const url = new URL(req.url, `http://${req.headers.host}`);
    try {
      if (url.pathname === '/api/session' && req.method === 'GET') return send(res, 200, { token });
      if (url.pathname.startsWith('/api/')) {
        if (!authenticated(req)) return send(res, 401, { error: 'Local session token required' });
        if (url.pathname === '/api/status' && req.method === 'GET') return send(res, 200, {
          dictionaryEntries: engine.count, ai: ai.status, aiEnabled: config.aiEnabled, speech: speechStatus, localOnly: true
        });
        if (url.pathname === '/api/query' && req.method === 'POST') {
          const body = await readBody(req);
          return send(res, 200, engine.query(String(body.input || '').slice(0, 64), String(body.context || '').slice(-160)));
        }
        if (url.pathname === '/api/learn' && req.method === 'POST') {
          const body = await readBody(req);
          return send(res, 200, { learned: engine.learn(String(body.input || ''), String(body.text || '')) });
        }
        if (url.pathname === '/api/learning' && req.method === 'DELETE') { engine.clearLearning(); return send(res, 200, { cleared: true }); }
        return send(res, 404, { error: 'API endpoint not found' });
      }
      const files = { '/': 'index.html', '/app.js': 'app.js', '/style.css': 'style.css', '/audio-worklet.js': 'audio-worklet.js' };
      if (!files[url.pathname] || req.method !== 'GET') return send(res, 404, { error: 'Not found' });
      const file = join(ROOT, 'web', files[url.pathname]);
      res.writeHead(200, { 'content-type': { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8' }[extname(file)] });
      res.end(readFileSync(file));
    } catch (error) { send(res, 400, { error: error.message }); }
  });
  const wss = new WebSocketServer({ noServer: true, maxPayload: 65536 });
  server.on('upgrade', (req, socket, head) => {
    const address = server.address();
    const url = new URL(req.url, `http://127.0.0.1:${address.port}`);
    // Browser passes token in subprotocol; avoids credentials in URLs and access logs.
    const protocols = String(req.headers['sec-websocket-protocol'] || '').split(',').map(s => s.trim());
    if (![`127.0.0.1:${address.port}`, `localhost:${address.port}`].includes(req.headers.host) ||
        !sameSite(req) || !['/ws/ime', '/ws/voice'].includes(url.pathname) || !protocols.includes(`winintel.${token}`)) {
      socket.write('HTTP/1.1 403 Forbidden\r\nConnection: close\r\n\r\n'); socket.destroy(); return;
    }
    if (wss.clients.size >= 8) { socket.destroy(); return; }
    wss.handleUpgrade(req, socket, head, ws => wss.emit('connection', ws, req));
  });
  wss.on('connection', (ws, req) => {
    const id = randomUUID(), voice = req.url === '/ws/voice';
    const state = { ws, pending: 0, active: false, finishing: false };
    connections.set(id, state);
    let sequence = -1, controller, lastRequest = 0;
    ws.on('message', async (bytes, binary) => {
      try {
        if (binary) {
          if (!voice || !state.active || state.finishing || bytes.length % 4 || bytes.length > 32000) throw new Error('Expected <=500 ms mono float32 PCM');
          if (state.pending >= 12) throw new Error('Speech decoder is overloaded; stop and retry');
          const samples = bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
          const pcm = new Float32Array(samples);
          if (pcm.some(x => !Number.isFinite(x) || Math.abs(x) > 1.01)) throw new Error('Invalid PCM samples');
          state.pending++;
          worker.postMessage({ id, type: 'audio', samples }, [samples]); return;
        }
        if (bytes.length > 4096) throw new Error('Control message too large');
        const message = JSON.parse(bytes.toString());
        if (voice) {
          if (!speechStatus.ready) throw new Error(speechStatus.error || 'Speech model not ready');
          if (message.type === 'start') {
            if (state.active) throw new Error('Session already active');
            state.active = true; state.finishing = false; worker.postMessage({ id, type: 'start' });
          } else if (message.type === 'finish' && state.active && !state.finishing) {
            state.finishing = true; worker.postMessage({ id, type: 'finish' });
          } else throw new Error('Expected start or finish');
        } else {
          if (message.type !== 'query' || !Number.isSafeInteger(message.seq) || message.seq <= sequence) return;
          if (Date.now() - lastRequest < 8) return;
          lastRequest = Date.now(); sequence = message.seq; controller?.abort(); controller = new AbortController();
          const input = String(message.input || '').slice(0, 64), context = String(message.context || '').slice(-160);
          const local = engine.query(input, context);
          emit(ws, { type: 'candidates', seq: sequence, phase: 'local', ...local });
          const current = sequence, signal = controller.signal;
          const ranked = await ai.rank(input, context, local.candidates, signal);
          if (current === sequence && !signal.aborted && ranked.source.startsWith('ai')) {
            emit(ws, { type: 'candidates', seq: current, phase: 'ai', ...local, ...ranked });
          }
        }
      } catch (error) { emit(ws, { type: 'error', error: error.message }); }
    });
    ws.on('close', () => { controller?.abort(); worker?.postMessage({ id, type: 'close' }); connections.delete(id); });
    ws.on('error', () => ws.close());
  });
  return { server, token, engine, wss, close: async () => {
    for (const ws of wss.clients) ws.terminate();
    wss.close(); await worker?.terminate();
    await new Promise(resolve => server.close(resolve));
  } };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const app = createApp();
  app.server.listen(config.port, '127.0.0.1', () => {
    const dir = ensureDataDir();
    writeFileSync(join(dir, 'session.json'), JSON.stringify({ token: app.token, port: config.port }), { mode: 0o600 });
    console.log(`winintel AI input method: http://127.0.0.1:${config.port} (${app.engine.count} dictionary entries)`);
  });
  const shutdown = () => app.close().then(() => process.exit());
  process.on('SIGINT', shutdown); process.on('SIGTERM', shutdown);
}
