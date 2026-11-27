// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
// Self-hosted interface: publish your own dated headlines from a local JSON file.
import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {resolve} from 'node:path';
import {pathToFileURL} from 'node:url';
export function validateNews(pack){
  const link=value=>{try{const u=new URL(value);return u.protocol==='https:'&&!u.username&&!u.password;}catch{return false;}};
  if(pack?.schema!==1||!Array.isArray(pack.items)||pack.items.length>200)throw new Error('Invalid news response');
  for(const item of pack.items)if(!item||typeof item.title!=='string'||!item.title.trim()||item.title.length>300||!link(item.url)||!link(item.source)||!Number.isFinite(Date.parse(item.published_at))||Date.parse(item.published_at)>Date.now()+600000)throw new Error('Invalid news item');
  return pack;
}
export function createNewsServer({file=process.env.WININTEL_NEWS_FILE||resolve(import.meta.dirname,'news.json')}={}){
  return createServer(async(req,res)=>{
    res.setHeader('Content-Type','application/json; charset=utf-8');res.setHeader('Cache-Control','no-store');res.setHeader('X-Content-Type-Options','nosniff');
    const reply=(code,data)=>{res.writeHead(code);res.end(JSON.stringify(data));};
    if(req.method!=='GET')return reply(405,{error:'get_only'});
    if((req.url||'').split('?')[0]!=='/v1/news')return reply(404,{error:'not_found'});
    try{
      const text=await readFile(file,'utf8');if(Buffer.byteLength(text)>2097152)throw new Error('Too large');
      return reply(200,{...validateNews(JSON.parse(text)),served_at:new Date().toISOString()});
    }catch{return reply(503,{error:'news_unavailable'});}
  });
}
if(process.argv[1]&&import.meta.url===pathToFileURL(resolve(process.argv[1])).href){
  createNewsServer().listen(Number(process.env.WININTEL_NEWS_PORT||8788),process.env.WININTEL_NEWS_HOST||'127.0.0.1',()=>console.log('winintel AI news API ready'));
}
