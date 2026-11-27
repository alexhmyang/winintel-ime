// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
// Private stdio; never opens a network socket.
import {createInterface} from 'node:readline';
import {translate} from './translation-engine.mjs';
let chain=Promise.resolve();
const input=createInterface({input:process.stdin,crlfDelay:Infinity});
input.on('line',line=>{
  chain=chain.then(async()=>{
    let request;
    try{
      request=JSON.parse(line.replace(/^\uFEFF/,''));
      const result=await translate(request.text,request.direction);
      process.stdout.write(JSON.stringify({id:request.id,...result})+'\n');
    }catch(error){process.stdout.write(JSON.stringify({id:request?.id,error:error.message})+'\n');}
  });
});
