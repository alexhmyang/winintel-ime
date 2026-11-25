// Mono PCM resampling with a continuous phase accumulator, independent of the device rate.
class Capture extends AudioWorkletProcessor {
  constructor() { super(); this.ratio = sampleRate / 16000; this.position = 0; this.previous = 0; this.buffer = new Float32Array(1600); this.used = 0; }
  process(inputs) {
    const channels = inputs[0];
    if (!channels?.length) return true;
    const mono = channels[0];
    for (let i = 0; i < mono.length; i++) {
      const current = mono[i];
      while (this.position < 1) {
        this.buffer[this.used++] = this.previous + (current - this.previous) * this.position;
        if (this.used === this.buffer.length) {
          this.port.postMessage(this.buffer.buffer, [this.buffer.buffer]);
          this.buffer = new Float32Array(1600); this.used = 0;
        }
        this.position += this.ratio;
      }
      this.position -= 1; this.previous = current;
    }
    return true;
  }
}
registerProcessor('winintel-capture', Capture);
