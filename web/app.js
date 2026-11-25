const $ = id => document.getElementById(id);
let token, ime, seq = 0, candidates = [], shownInput = '', timer, keyboardFrozen = false, selected = 0;
let voice, stream, audio, recording = false;
const labels = { correction: '纠错', abbreviation: '简拼', completion: '补全', fuzzy: '模糊音', prediction: '联想', sentence: '组句' };
const api = async (path, body, method = 'POST') => {
  const result = await fetch(path, { method, headers: { 'x-winintel-token': token, 'content-type': 'application/json' }, ...(body === undefined ? {} : { body: JSON.stringify(body) }) });
  if (!result.ok) throw new Error((await result.json()).error);
  return result.json();
};
const socket = path => new WebSocket(`${location.protocol === 'https:' ? 'wss' : 'ws'}://${location.host}${path}`, `winintel.${token}`);
function query() {
  const current = ++seq; keyboardFrozen = false; selected = 0; candidates = []; shownInput = '';
  $('candidates').replaceChildren();
  clearTimeout(timer);
  timer = setTimeout(() => {
    if (ime?.readyState === WebSocket.OPEN) ime.send(JSON.stringify({ type: 'query', seq: current, input: $('compose').value, context: $('output').value.slice(0, $('output').selectionStart).slice(-160) }));
  }, 12);
}
function draw(message) {
  if (message.seq !== seq || (message.phase === 'ai' && keyboardFrozen)) return;
  candidates = message.candidates; shownInput = message.input; selected = 0;
  $('candidates').replaceChildren();
  candidates.forEach((candidate, index) => {
    const button = document.createElement('button'), number = document.createElement('small');
    number.textContent = `${index + 1}`; button.append(number, document.createTextNode(candidate.text));
    if (labels[candidate.kind] || candidate.ai) { const tag = document.createElement('em'); tag.textContent = candidate.ai ? 'AI' : labels[candidate.kind]; button.append(tag); }
    button.addEventListener('pointerenter', () => { keyboardFrozen = true; });
    button.addEventListener('click', () => commit(index));
    $('candidates').append(button);
  });
  $('latency').textContent = `引擎 ${message.latencyMs.toFixed(2)} ms`;
  $('phase').textContent = message.phase === 'ai' ? '本地 AI 排序' : '本地词库 · 即时候选';
}
function insert(text) {
  const out = $('output'), start = out.selectionStart, end = out.selectionEnd;
  const before = out.value.slice(0, start);
  const separator = /[a-z0-9]$/i.test(before) && /^[a-z0-9]/i.test(text) ? ' ' : '';
  out.setRangeText(separator + text, start, end, 'end');
}
function commit(index = selected) {
  if (!$('compose').value && !candidates[index]) return;
  if (shownInput !== $('compose').value && $('compose').value) { $('hint').textContent = '候选正在更新，请稍后选词'; return; }
  const candidate = candidates[index];
  if (!candidate) return;
  const input = $('compose').value;
  insert(candidate.text);
  if (input) api('/api/learn', { input, text: candidate.text }).catch(() => {});
  $('compose').value = ''; keyboardFrozen = false; $('compose').focus(); query();
}
$('compose').addEventListener('input', event => { if (!event.isComposing) query(); });
$('compose').addEventListener('compositionend', query);
$('compose').addEventListener('keydown', event => {
  if (event.isComposing) return;
  if (event.key === ' ' && candidates.length) { event.preventDefault(); commit(); }
  else if (/^[1-9]$/.test(event.key) && candidates.length) { event.preventDefault(); commit(Number(event.key) - 1); }
  else if (event.key === 'Enter' && $('compose').value) { event.preventDefault(); insert($('compose').value); $('compose').value = ''; query(); }
  else if (event.key === 'Escape') { $('compose').value = ''; query(); }
  else if (['ArrowDown', 'ArrowUp'].includes(event.key) && candidates.length) {
    event.preventDefault(); keyboardFrozen = true;
    selected = (selected + (event.key === 'ArrowDown' ? 1 : candidates.length - 1)) % candidates.length;
    [...$('candidates').children].forEach((button, i) => button.classList.toggle('selected', i === selected));
  }
});
document.querySelectorAll('[data-input]').forEach(button => button.addEventListener('click', () => { $('compose').value = button.dataset.input; $('compose').focus(); query(); }));
$('copy').addEventListener('click', async () => {
  try { await navigator.clipboard.writeText($('output').value); $('hint').textContent = '已复制'; }
  catch { $('hint').textContent = '复制失败，请选中文字后按 Ctrl+C'; }
});
$('clear').addEventListener('click', () => { $('output').value = ''; $('compose').value = ''; query(); });
$('forget').addEventListener('click', async () => { await api('/api/learning', undefined, 'DELETE'); $('hint').textContent = '已清除本机选词学习'; });
$('mode').addEventListener('click', () => { $('compose').focus(); $('hint').textContent = '中英文自动混输；回车直接输入原文'; });

async function refreshStatus() {
  const status = await api('/api/status', undefined, 'GET');
  $('status').textContent = `${status.dictionaryEntries.toLocaleString()} 词条 · AI ${status.aiEnabled ? status.ai : '未启用'} · 语音 ${status.speech.ready ? '已就绪' : '模型未就绪'}`;
  $('mic').disabled = !status.speech.ready;
  if (!status.speech.ready) $('voice').textContent = status.speech.error || '语音模型加载中…';
  else if (!recording) $('voice').textContent = '本地流式语音已就绪';
}
async function cleanupRecording() {
  recording = false; stream?.getTracks().forEach(track => track.stop()); stream = null;
  if (audio && audio.state !== 'closed') await audio.close(); audio = null;
  $('mic').textContent = '开始语音'; $('mic').classList.remove('recording');
}
async function toggleVoice() {
  if (recording) {
    if (voice?.readyState === 1) voice.send(JSON.stringify({ type: 'finish' }));
    await cleanupRecording(); return;
  }
  try {
    stream = await navigator.mediaDevices.getUserMedia({ audio: { channelCount: 1, echoCancellation: true, noiseSuppression: true, autoGainControl: true }, video: false });
    voice = socket('/ws/voice');
    await new Promise((resolve, reject) => { voice.onopen = resolve; voice.onerror = () => reject(new Error('语音连接失败')); });
    voice.onmessage = event => {
      const message = JSON.parse(event.data);
      if (message.type === 'partial') $('voice').textContent = `正在识别：${message.text}`;
      if (message.type === 'final') {
        if (message.text) insert(message.text);
        $('voice').textContent = `已识别：${message.text || '没有检测到语音'}`;
        if (message.finished) voice.close();
      }
      if (message.type === 'error') { $('voice').textContent = message.error; cleanupRecording(); voice.close(); }
    };
    voice.onclose = () => { if (recording) { $('voice').textContent = '语音连接已断开'; cleanupRecording(); } };
    voice.send(JSON.stringify({ type: 'start' }));
    audio = new AudioContext();
    await audio.audioWorklet.addModule('/audio-worklet.js');
    const source = audio.createMediaStreamSource(stream), capture = new AudioWorkletNode(audio, 'winintel-capture');
    capture.port.onmessage = event => {
      if (voice.readyState === 1 && recording) {
        if (voice.bufferedAmount > 128000) { $('voice').textContent = '识别积压，请停止后重试'; toggleVoice(); return; }
        voice.send(event.data);
      }
    };
    const muted = audio.createGain(); muted.gain.value = 0;
    source.connect(capture); capture.connect(muted); muted.connect(audio.destination);
    recording = true; $('mic').textContent = '结束语音'; $('mic').classList.add('recording'); $('voice').textContent = '正在聆听…';
  } catch (error) { $('voice').textContent = `无法启动语音：${error.message}`; await cleanupRecording(); voice?.close(); }
}
$('mic').addEventListener('click', toggleVoice);
document.addEventListener('keydown', event => { if (event.altKey && event.code === 'KeyV') { event.preventDefault(); if (!$('mic').disabled) toggleVoice(); } });
async function connect() {
  try {
    token = (await (await fetch('/api/session')).json()).token;
    ime = socket('/ws/ime'); ime.onmessage = event => { const message = JSON.parse(event.data); if (message.type === 'candidates') draw(message); };
    ime.onopen = () => { query(); refreshStatus().catch(() => {}); };
    ime.onclose = () => { $('status').textContent = '连接断开，正在重连…'; setTimeout(connect, 1500); };
    await refreshStatus();
  } catch { $('status').textContent = '本地服务未连接'; setTimeout(connect, 1500); }
}
connect();
setInterval(() => refreshStatus().catch(() => {}), 10000);
