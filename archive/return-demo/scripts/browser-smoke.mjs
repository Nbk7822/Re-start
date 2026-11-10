import {spawn} from 'node:child_process';
import {mkdtemp,readFile,writeFile,mkdir} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import assert from 'node:assert/strict';
const profile=await mkdtemp(join(tmpdir(),'return-browser-'));
const chrome=spawn('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',['--headless=new','--no-first-run','--no-default-browser-check','--disable-background-networking','--disable-gpu','--hide-scrollbars','--remote-debugging-port=0','--user-data-dir='+profile,'--window-size=1440,1100','about:blank'],{stdio:['ignore','ignore','pipe']});
let diagnostic='';chrome.stderr.on('data',b=>diagnostic+=b.toString());
const wait=ms=>new Promise(r=>setTimeout(r,ms));let ws;
try{
 let port;for(let i=0;i<100;i++){try{port=Number((await readFile(join(profile,'DevToolsActivePort'),'utf8')).split('\n')[0]);break;}catch{if(chrome.exitCode!==null)throw new Error('Chrome exited: '+diagnostic.slice(-1800));await wait(100);}}
 if(!port)throw new Error('Chrome debugging did not start: '+diagnostic.slice(-1800));
 const tabs=await (await fetch('http://127.0.0.1:'+port+'/json/list')).json();
 ws=new WebSocket(tabs.find(t=>t.type==='page').webSocketDebuggerUrl);await new Promise((r,j)=>{ws.onopen=r;ws.onerror=j;});let serial=0;const pending=new Map(),errors=[];
 ws.onmessage=({data})=>{const m=JSON.parse(data);if(m.id){const p=pending.get(m.id);pending.delete(m.id);m.error?p?.reject(m.error):p?.resolve(m.result);}else if(m.method==='Runtime.exceptionThrown')errors.push(m.params.exceptionDetails.exception?.description||m.params.exceptionDetails.text);};
 const send=(method,params={})=>new Promise((resolve,reject)=>{const id=++serial;pending.set(id,{resolve,reject});ws.send(JSON.stringify({id,method,params}));});
 const evaluate=async expression=>{const r=await send('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)throw new Error(r.exceptionDetails.text+' '+r.exceptionDetails.exception?.description);return r.result.value;};
 await send('Page.enable');await send('Runtime.enable');await send('Emulation.setDeviceMetricsOverride',{width:1440,height:1100,deviceScaleFactor:1,mobile:false});await send('Page.navigate',{url:'http://127.0.0.1:4173/'});
 for(let i=0;i<100;i++){if(await evaluate('Boolean(window.Return && document.querySelector("#welcome[open]"))'))break;await wait(50);}
 assert.equal(await evaluate('Boolean(window.Return)'),true);
 await evaluate('document.getElementById("begin-paused").click()');
 assert.equal(await evaluate('window.Return.observe().paused'),true);
 await mkdir('qa',{recursive:true});
 await writeFile('qa/desktop.png',Buffer.from((await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:true})).data,'base64'));
 await evaluate('document.querySelector("[data-task=forage]").click();document.getElementById("step").click();document.querySelector("[data-person=mira]").click();document.getElementById("invite-person").click()');
 assert.equal(await evaluate('window.Return.observe().people.find(p=>p.id==="mira").joined'),true);
 await evaluate('document.getElementById("step").click();document.querySelector("[data-person=ilex]").click();document.getElementById("invite-person").click()');
 const refused=await evaluate('window.Return.command({type:"request",actorId:"ilex",task:"wood"})');assert.equal(refused.ok,false);
 await evaluate('document.querySelector("[data-person=you]").click();document.getElementById("blood-target").value="ilex";document.getElementById("donate-selected").click();document.getElementById("step").click()');
 assert.ok(await evaluate('window.Return.observe().people.find(p=>p.id==="ilex").debtTo.includes("you")'));
 await evaluate('document.getElementById("console-toggle").click();document.getElementById("observe").click()');assert.ok((await evaluate('document.getElementById("console-output").textContent')).includes('"version": 1'));
 await evaluate('document.getElementById("console-toggle").click();for(let i=0;i<12&&!window.Return.observe().ended;i++)window.Return.command({type:"step"})');
 assert.equal(await evaluate('Boolean(document.querySelector("#ending[open]"))'),true);
 await writeFile('qa/ending.png',Buffer.from((await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:true})).data,'base64'));
 await evaluate('document.getElementById("ending-restart").click();document.getElementById("seed-input").value="991";document.getElementById("confirm-restart").click()');assert.equal(await evaluate('window.Return.observe().shift'),0);
 await send('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});await wait(200);
 assert.ok(await evaluate('document.documentElement.scrollWidth <= 390'), 'mobile layout overflows');
 await writeFile('qa/mobile.png',Buffer.from((await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:true})).data,'base64'));
 assert.deepEqual(errors,[]);
 console.log(JSON.stringify({passed:true,checks:['launch','pause','work intent','recruitment','daylight refusal','consensual blood and debt','text observation','ending','restart','mobile width','no browser exceptions'],screenshots:['qa/desktop.png','qa/ending.png','qa/mobile.png']}));
}finally{ws?.close();chrome.kill();}
