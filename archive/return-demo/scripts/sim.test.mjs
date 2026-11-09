import test from 'node:test';
import assert from 'node:assert/strict';
import {createSimulation} from '../dist/sim.js';

test('visible observation is detached and hides people who have not arrived',()=>{
 const s=createSimulation();const v=s.observe();assert.deepEqual(v.people.map(p=>p.id),['you','mira']);
 v.people[0].trust.you=100;v.locations.camp.x=99;assert.equal(s.person('you').trust.you,0);assert.equal(s.observe().locations.camp.x,11);
});
test('a real meal fulfills a recruitment promise and changes remembered trust',()=>{
 const s=createSimulation(8291);assert.equal(s.command({type:'invite',actorId:'mira'}).ok,true);
 s.command({type:'intent',actorId:'you',task:'forage'});s.command({type:'step'});
 assert.equal(s.state.promises.find(p=>p.to==='mira').status,'kept');assert.ok(s.person('mira').trust.you>8);assert.ok(s.person('mira').memories.some(m=>m.text.includes('kept')));
});
test('vampires refuse unsafe daytime work through the same public commands',()=>{
 const s=createSimulation();s.command({type:'step'});s.command({type:'invite',actorId:'ilex'});
 const r=s.command({type:'request',actorId:'ilex',task:'wood'});assert.equal(r.ok,false);assert.match(r.message,/sun/i);assert.equal(s.person('ilex').intent,null);
});
test('blood donation costs the consenting donor health and creates a concrete debt',()=>{
 const s=createSimulation();s.command({type:'step'});s.command({type:'invite',actorId:'ilex'});
 const before=s.person('you').health;assert.equal(s.command({type:'donate',actorId:'you',target:'ilex'}).ok,true);s.command({type:'step'});
 assert.ok(s.person('you').health<before);assert.ok(s.person('ilex').debtTo.includes('you'));assert.ok(s.state.events.some(e=>e.type==='aid'&&e.actors.includes('ilex')));
});
test('a badly wounded NPC cannot be forced to donate by the text interface',()=>{
 const s=createSimulation();s.command({type:'invite',actorId:'mira'});s.command({type:'step'});s.command({type:'invite',actorId:'ilex'});s.person('mira').health=30;
 assert.equal(s.command({type:'donate',actorId:'mira',target:'ilex'}).ok,false);
});
test('broken promises affect later cooperation instead of only flavor text',()=>{
 const s=createSimulation();s.command({type:'invite',actorId:'mira'});s.command({type:'step'});s.command({type:'step'});s.command({type:'invite',actorId:'oren'});
 s.state.food=0;s.command({type:'request',actorId:'oren',task:'rest',offer:'meal'});s.person('you').intent={task:'rest'};s.person('mira').intent={task:'rest'};
 s.command({type:'step'});s.state.food=0;s.person('you').intent={task:'rest'};s.person('mira').intent={task:'rest'};s.person('oren').intent={task:'rest'};s.command({type:'step'});
 assert.ok(s.state.promises.some(p=>p.to==='oren'&&p.status==='broken'));assert.ok(s.person('oren').trust.you<0);assert.ok(s.state.events.some(e=>e.type==='broken'&&e.cause));
});
test('identical seeds and commands reproduce the same causal history',()=>{
 const run=()=>{const s=createSimulation(45);s.command({type:'invite',actorId:'mira'});for(let i=0;i<12&&!s.state.ended;i++)s.command({type:'step'});return s.observe();};assert.deepEqual(run(),run());
});
test('the scenario ends after twelve shifts and new camp resets all obligations',()=>{
 const s=createSimulation();s.command({type:'invite',actorId:'mira'});for(let i=0;i<12&&!s.state.ended;i++)s.command({type:'step'});assert.equal(s.state.ended,true);assert.equal(s.state.paused,true);assert.ok(s.state.result);
 s.command({type:'restart',seed:988});assert.equal(s.state.shift,0);assert.equal(s.state.promises.length,0);assert.equal(s.person('mira').joined,false);assert.equal(s.state.ended,false);
});
test('pause prevents elapsed time from consuming simulation events',()=>{
 const s=createSimulation();s.elapse(50);assert.equal(s.state.shift,0);s.command({type:'pause',value:false});s.elapse(1);assert.equal(s.state.shift,1);s.command({type:'pause',value:true});const phase=s.state.phase;s.elapse(30);assert.equal(s.state.phase,phase);
});
