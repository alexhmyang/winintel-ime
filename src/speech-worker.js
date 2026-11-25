import { parentPort } from 'node:worker_threads';
import { createRequire } from 'node:module';
import { existsSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { performance } from 'node:perf_hooks';
import { config } from './config.js';

let recognizer;
const sessions = new Map();
try {
  if (!existsSync(config.asrDir)) throw new Error('Run npm run download:voice first.');
  const files = readdirSync(config.asrDir);
  const find = prefix => {
    const file = files.find(f => f.startsWith(prefix) && f.endsWith('.onnx') && (prefix === 'decoder' || f.includes('int8')));
    if (!file) throw new Error(`Missing ${prefix} ONNX model in ${config.asrDir}`);
    return join(config.asrDir, file);
  };
  if (!existsSync(join(config.asrDir, 'tokens.txt'))) throw new Error('Missing tokens.txt');
  const sherpa = createRequire(import.meta.url)('sherpa-onnx-node');
  recognizer = new sherpa.OnlineRecognizer({
    featConfig: { sampleRate: 16000, featureDim: 80 },
    modelConfig: {
      transducer: { encoder: find('encoder'), decoder: find('decoder'), joiner: find('joiner') },
      tokens: join(config.asrDir, 'tokens.txt'), numThreads: config.asrThreads,
      provider: config.asrProvider, modelType: 'zipformer', debug: 0
    },
    decodingMethod: 'greedy_search',
    enableEndpoint: true,
    rule1MinTrailingSilence: 2.4, rule2MinTrailingSilence: 0.6, rule3MinUtteranceLength: 20
  });
  parentPort.postMessage({ type: 'status', ready: true, backend: 'sherpa-onnx', sampleRate: 16000 });
} catch (error) {
  parentPort.postMessage({ type: 'status', ready: false, error: error.message });
}
parentPort.on('message', message => {
  const { id, type } = message;
  try {
    if (!recognizer) throw new Error('Speech model is not ready');
    if (type === 'start') {
      if (sessions.size >= 2 || sessions.has(id)) throw new Error('Only two concurrent speech sessions are supported');
      sessions.set(id, { stream: recognizer.createStream(), text: '', audioSeconds: 0 });
      parentPort.postMessage({ id, type: 'started', sampleRate: 16000 }); return;
    }
    if (type === 'close') { sessions.delete(id); return; }
    const session = sessions.get(id);
    if (!session) throw new Error('Start a speech session first');
    if (type === 'audio' || type === 'finish') {
      const start = performance.now();
      const samples = type === 'finish' ? new Float32Array(12800) : new Float32Array(message.samples);
      session.stream.acceptWaveform({ samples, sampleRate: 16000 });
      if (type === 'finish') session.stream.inputFinished();
      else session.audioSeconds += samples.length / 16000;
      while (recognizer.isReady(session.stream)) recognizer.decode(session.stream);
      const text = recognizer.getResult(session.stream).text.trim();
      const endpoint = type === 'finish' || recognizer.isEndpoint(session.stream);
      if (text !== session.text || endpoint) {
        parentPort.postMessage({ id, type: endpoint ? 'final' : 'partial', finished: type === 'finish', text, decodeMs: performance.now() - start, audioSeconds: session.audioSeconds });
        session.text = text;
      }
      if (type === 'audio') parentPort.postMessage({ id, type: 'ack' });
      if (endpoint) { recognizer.reset(session.stream); session.text = ''; }
      if (type === 'finish') sessions.delete(id);
    }
  } catch (error) { parentPort.postMessage({ id, type: 'error', error: error.message }); }
});
