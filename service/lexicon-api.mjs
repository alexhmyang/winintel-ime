// SPDX-License-Identifier: GPL-3.0-or-later
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { resolve, join } from 'node:path';
import { pathToFileURL } from 'node:url';
import { createHash, timingSafeEqual } from 'node:crypto';
const builtin = join(import.meta.dirname,'lexicons');
const hash = s => createHash('sha256').update(s).digest();
export function createLexiconServer({ root=builtin, tokens=[] }={}) {
  const credentials=tokens.filter(Boolean).map(hash), limits=new Map();
  return createServer(async(req,res)=>{
    res.setHeader('Content-Type','application/json; charset=utf-8');
    res.setHeader('Cache-Control','no-store');
    res.setHeader('X-Content-Type-Options','nosniff');
    const reply=(status,value)=>{res.writeHead(status);res.end(JSON.stringify(value));};
    const address=req.socket.remoteAddress, now=Date.now();
    if(limits.size>10000)limits.clear();
    let quota=limits.get(address);if(!quota||now-quota.time>60000)limits.set(address,quota={time:now,count:0});
    if(++quota.count>60)return reply(429,{error:'rate_limit'});
    if(req.method!=='GET')return reply(405,{error:'get_only'});
    const path=(req.url||'').split('?')[0];
    const match=path.match(/^\/v1\/lexicons\/([a-z0-9-]{1,64})$/);
    try{
      if(path==='/v1/hotwords')return reply(200,JSON.parse(await readFile(join(import.meta.dirname,'hotwords.json'),'utf8')));
      const catalog=JSON.parse(await readFile(join(root,'catalog.json'),'utf8'));
      if(path==='/v1/catalog')return reply(200,{schema:1,plans:['community','managed'],lexicons:catalog});
      if(!match)return reply(404,{error:'not_found'});
      const descriptor=catalog.find(item=>item.id===match[1]);
      if(!descriptor)return reply(404,{error:'not_found'});
      if(descriptor.access==='managed'){
        const token=(req.headers.authorization||'').match(/^Bearer ([^\s]{1,256})$/);
        if(!token||!credentials.some(value=>timingSafeEqual(value,hash(token[1]))))return reply(401,{error:'access_key_required'});
      }
      // Filename comes from a validated identifier, never from arbitrary request paths.
      const pack=JSON.parse(await readFile(join(root,match[1]+'.json'),'utf8'));
      if(pack.schema!==1||pack.id!==match[1]||!Array.isArray(pack.entries)||pack.entries.length>10000)throw new Error('Invalid pack');
      return reply(200,pack);
    }catch{return reply(503,{error:'pack_unavailable'});}
  });
}
if(process.argv[1]&&import.meta.url===pathToFileURL(resolve(process.argv[1])).href){
  const port=Number(process.env.WININTEL_LEXICON_PORT||8787);
  const server=createLexiconServer({root:process.env.WININTEL_LEXICON_ROOT||builtin,tokens:(process.env.WININTEL_LEXICON_TOKENS||'').split(',')});
  server.listen(port,process.env.WININTEL_LEXICON_HOST||'127.0.0.1',()=>console.log(`winintel AI lexicon API listening on ${port}`));
}
