// SPDX-License-Identifier: GPL-3.0-or-later
import {createLexiconServer} from '../../service/lexicon-api.mjs';
import {once} from 'node:events';
const server=createLexiconServer();
server.listen(0,'127.0.0.1');await once(server,'listening');
const url=`http://127.0.0.1:${server.address().port}`,rows=['| Check | Result |','|---|---|'];let failures=0;
function check(ok,label){rows.push(`| ${label} | ${ok?'PASS':'FAIL'} |`);if(!ok)failures++;}
try{
  const response=await fetch(url+'/v1/hotwords'),pack=await response.json();
  check(response.status===200&&pack.schema===1,'Self-hosted content endpoint');
  check(pack.version==='2026-11-26'&&pack.chinese.length>=17&&pack.english.length>=30,'Bilingual dated pack');
  check((await fetch(url+'/v1/hotwords',{method:'POST'})).status===405,'Read-only endpoint');
  check((await fetch(url+'/v1/catalog')).status===200,'Existing dictionary catalog preserved');
  check((await fetch(url+'/v1/lexicons/team-demo')).status===401,'Managed dictionary still requires key');
}finally{server.closeAllConnections();server.close();}
console.log(rows.join('\n'));process.exitCode=failures?1:0;
