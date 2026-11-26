import { createServer } from 'node:http';
import { Worker } from 'node:worker_threads';
import { createRequire } from 'node:module';
import { randomBytes } from 'node:crypto';
import { mkdirSync,writeFileSync,renameSync,readFileSync } from 'node:fs';
import { resolve,join } from 'node:path';
const worker=new Worker(new URL('./voice-worker.mjs',import.meta.url));
let ready=false,error='';
const sockets=new Map();
const token=randomBytes(24).toString('hex');
const {WebSocketServer}=createRequire(import.meta.url)('ws');
if(process.argv.includes('--self-test')){
  let recognized='';
  worker.on('message',m=>{
    if(m.type==='status'){
      if(!m.ready){console.error(m.error);worker.terminate();process.exitCode=1;return;}
      worker.postMessage({id:'test',type:'start'});
    }else if(m.type==='started'){
      const wav=readFileSync(resolve(import.meta.dirname,'..','models','voice','test_wavs','0.wav'));
      let offset=12,pcm;
      while(offset+8<=wav.length){const size=wav.readUInt32LE(offset+4);if(wav.toString('ascii',offset,offset+4)==='data'){pcm=wav.subarray(offset+8,offset+8+size);break;}offset+=8+size+(size%2);}
      if(!pcm)throw Error('Invalid WAV');
      const samples=new Float32Array(pcm.length/2);for(let i=0;i<samples.length;i++)samples[i]=pcm.readInt16LE(i*2)/32768;
      for(let i=0;i<samples.length;i+=1600)worker.postMessage({id:'test',type:'audio',samples:samples.slice(i,i+1600).buffer});
      worker.postMessage({id:'test',type:'finish'});
    }else if(m.type==='final'){recognized+=m.text;console.log(m.text);if(m.finished){if(!recognized)process.exitCode=1;worker.terminate();}}
    else if(m.type==='error'){console.error(m.error);process.exitCode=1;worker.terminate();}
  });
}else{
  const server=createServer((req,res)=>{res.writeHead(404);res.end();});
  const ws=new WebSocketServer({noServer:true,maxPayload:65536,perMessageDeflate:false});
  server.on('upgrade',(req,socket,head)=>{
    if(req.url!='/ws/voice'||req.headers['sec-websocket-protocol']!=='winintel.'+token||req.headers.origin){socket.destroy();return;}
    ws.handleUpgrade(req,socket,head,s=>ws.emit('connection',s,req));
  });
  ws.on('connection',s=>{
    const id=randomBytes(8).toString('hex');sockets.set(id,s);
    s.on('message',(data,binary)=>{
      if(!ready){s.send(JSON.stringify({type:'error',error:error||'离线语音模型正在加载'}));return;}
      if(binary){if(data.length%4)return s.close();const copy=new Uint8Array(data).buffer;worker.postMessage({id,type:'audio',samples:copy},[copy]);}
      else{try{const m=JSON.parse(data);if(['start','finish'].includes(m.type))worker.postMessage({id,type:m.type});else s.close();}catch{s.close();}}
    });
    s.on('close',()=>{sockets.delete(id);worker.postMessage({id,type:'close'});});
  });
  const folder=join(process.env.LOCALAPPDATA,'winintel-ime-native');mkdirSync(folder,{recursive:true});
  function publish(){if(!server.listening)return;const file=join(folder,'voice-session.json');writeFileSync(file+'.tmp',JSON.stringify({port:server.address().port,token,ready,error,pid:process.pid}));renameSync(file+'.tmp',file);}
  worker.on('message',m=>{if(m.type==='status'){ready=m.ready;error=m.error||'';publish();}else{const s=sockets.get(m.id);if(s&&s.readyState===1)s.send(JSON.stringify(m));}});
  worker.on('error',e=>{ready=false;error=e.message;publish();});
  server.listen(0,'127.0.0.1',publish);
}
