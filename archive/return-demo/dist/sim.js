export const TASKS={forage:'Find food',wood:'Gather timber',build:'Repair shelter',rest:'Rest',care:'Care for someone',guard:'Keep watch'};
export const PLACES={camp:{x:11,y:9,name:'The old camp'},forage:{x:4,y:12,name:'Berry ground'},wood:{x:6,y:4,name:'Fallen timber'},build:{x:11,y:7,name:'Ruined shelter'},rest:{x:13,y:8,name:'Covered ruins'},care:{x:12,y:9,name:'Camp hearth'},guard:{x:15,y:12,name:'Southern path'}};
const clamp=(v,a=0,b=100)=>Math.min(b,Math.max(a,v));
const rng=seed=>{let x=seed>>>0;return ()=>{x+=0x6D2B79F5;let t=x;t=Math.imul(t^t>>>15,t|1);t^=t+Math.imul(t^t>>>7,t|61);return ((t^t>>>14)>>>0)/4294967296;};};
const descriptions={Protective:'Puts an injured companion before ordinary work.',Oathbound:'Tries to finish an accepted promise, even when tired.',Proud:'Refuses an unsafe bargain and values being asked.',Practical:'Prefers a workable shelter and a reserve of food.',Generous:'May volunteer blood or care when someone is in danger.',Independent:'Needs a better reason to accept someone else’s plan.',Desperate:'May take a reserved meal when critically hungry.',Anxious:'Avoids risky work when wounded or exhausted.'};
const roster=[
 ['you','You','human','forager','Practical',0,'#d9b76d'],
 ['mira','Mira','human','forager','Protective',0,'#a8b99c'],
 ['ilex','Ilex','vampire','carer','Proud',1,'#b5a3c8'],
 ['oren','Oren','human','builder','Oathbound',2,'#cb9c78'],
 ['bram','Bram','human','carer','Generous',3,'#93b7bb'],
 ['nia','Nia','human','forager','Desperate',4,'#d0ad88'],
 ['sera','Sera','human','builder','Independent',6,'#b1b972'],
 ['vale','Vale','vampire','watcher','Anxious',7,'#9d9fca']
];
function world(seed){
 const truth=rng(seed);const extra=['Practical','Generous','Anxious','Independent'];
 const people=roster.map(([id,name,species,role,trait,arrival,color],i)=>({id,name,species,role,traits:[trait,extra.filter(t=>t!==trait)[Math.floor(truth()*extra.filter(t=>t!==trait).length)]],arrival,color,arrived:arrival===0,joined:id==='you',alive:true,left:false,hunger:id==='ilex'?65:id==='nia'?74:36+Math.floor(truth()*18),fatigue:8+Math.floor(truth()*12),health:100,trust:{you:0},memories:[],debtTo:[],intent:null,job:null,speech:id==='mira'?'I can gather food. I need somewhere safe to stay.':'',home:{x:10+(i%4)*.9,y:9+Math.floor(i/4)*1.2},position:{x:10+(i%4)*.9,y:9+Math.floor(i/4)*1.2}}));
 return {seed,shift:0,phase:0,running:false,paused:true,speed:1,food:0,wood:0,roof:0,policy:'need',people,promises:[],events:[],serial:0,ended:false,result:null,weather:'Clear',weatherPenalty:0,guarded:false,selected:'you',messages:[],lastIncident:null,donations:[],storyCount:0};
}
export function createSimulation(seed=8291){
 const state=world(seed);let dice=rng(seed^0x9175af3);
 const person=id=>state.people.find(p=>p.id===id);
 const present=()=>state.people.filter(p=>p.arrived&&p.alive&&!p.left);
 const members=()=>present().filter(p=>p.joined);
 const trust=(p,id='you')=>p.trust[id]??0;
 const log=(type,text,actors=[],cause=null)=>{const event={id:'e'+(++state.serial),shift:state.shift,type,text,actors,cause};state.events.push(event);return event.id;};
 const remember=(p,text,eventId,affect=0)=>{p.memories.unshift({text,eventId,shift:state.shift,affect});p.memories=p.memories.slice(0,30);};
 const mood=p=>clamp(77-p.hunger*.34-p.fatigue*.29-(100-p.health)*.24+trust(p)*.16+p.memories.filter(m=>state.shift-m.shift<3).reduce((s,m)=>s+m.affect,0));
 const night=()=>((Math.max(1,state.shift)-1)%6)>=3;
 const desire=p=>p.health<55?'Recover from injury':p.hunger>66?(p.species==='vampire'?'Find a willing blood donor':'Get a meal'):p.fatigue>64?'Get undisturbed rest':!p.joined?'Find a safe place to belong':state.roof<3?'Make the shelter weatherproof':p.role==='carer'?'Keep the others well':'Keep this camp supplied';
 const addDebt=(beneficiary,helper,eventId)=>{if(beneficiary.id===helper.id)return;beneficiary.trust[helper.id]=clamp(trust(beneficiary,helper.id)+12,-100,100);if(!beneficiary.debtTo.includes(helper.id))beneficiary.debtTo.push(helper.id);remember(beneficiary,helper.name+' helped me when I needed it.',eventId,5);};
 function promise(p,type){
  const old=state.promises.find(o=>o.to===p.id&&o.type===type&&o.status==='open');if(old)return old;
  const entry={id:'p'+(state.promises.length+1),to:p.id,type,due:state.shift+2,status:'open',created:state.shift};state.promises.push(entry);
  entry.eventId=log('promise','You promised '+p.name+(type==='meal'?' a meal':' time to rest')+' by the end of shift '+entry.due+'.',['you',p.id]);
  return entry;
 }
 function fulfill(p,type,cause){for(const o of state.promises.filter(o=>o.to===p.id&&o.type===type&&o.status==='open')){
  o.status='kept';p.trust.you=clamp(trust(p)+14,-100,100);const id=log('kept',p.name+' remembers: you kept your '+(type==='meal'?'meal':'rest')+' promise.',['you',p.id],cause??o.eventId);remember(p,'You kept your '+type+' promise.',id,9);state.storyCount++;
 }}
 function unsafe(p,task){
  if(p.species==='vampire'&&!night()&&['forage','wood','guard'].includes(task))return 'The sun is still up. I need covered work.';
  if(p.health<38&&['forage','wood','build','guard'].includes(task))return 'I am too badly hurt for that.';
  if(p.fatigue>84&&task!=='rest'&&!p.traits.includes('Oathbound'))return 'I need sleep before I can do that.';
  return null;
 }
 function acceptance(p,task,offered){
  const red=unsafe(p,task);if(red)return {ok:false,reason:red};
  const favorite=(p.role==='forager'&&task==='forage')||(p.role==='builder'&&['wood','build'].includes(task))||(p.role==='carer'&&task==='care')||(p.role==='watcher'&&task==='guard');
  const score=trust(p)*.45+mood(p)*.48+(favorite?12:0)+(offered?20:0)+(p.debtTo.includes('you')?12:0)-(p.traits.includes('Independent')?10:0);
  return score>=18?{ok:true,reason:offered?'I will help. Please keep that promise.':favorite?'That is work I know. I will do it.':'All right. I can help with that.'}:{ok:false,reason:trust(p)<-20?'You broke your word. I need a reason to trust you.':'I need something in return. Offer a meal or let me rest.'};
 }
 function choose(p){
  if(!p.joined)return {task:'rest',reason:'Waiting to see whether this camp is safe.'};
  if(p.health<38)return {task:'rest',reason:'Too badly hurt to work.'};
  if(p.fatigue>82&&!p.traits.includes('Oathbound'))return {task:'rest',reason:'Exhaustion takes priority.'};
  if(p.intent){const red=unsafe(p,p.intent.task);if(!red){const j={...p.intent,reason:p.id==='you'?'Your chosen intention.':'Accepted your request.'};p.intent=null;return j;}p.speech=red;log('refusal',p.name+' stopped: '+red,[p.id]);p.intent=null;}
  if(p.fatigue>73)return {task:'rest',reason:'Needs rest before another job.'};
  const hurt=members().filter(t=>t.id!==p.id&&t.health<72).sort((a,b)=>a.health-b.health)[0];
  if(hurt&&state.food>0&&(p.traits.includes('Protective')||p.role==='carer'))return {task:'care',target:hurt.id,reason:hurt.name+' needs care.'};
  if(p.species==='human'&&p.traits.includes('Generous')&&p.health>80){const v=members().find(v=>v.species==='vampire'&&v.hunger>83&&trust(p,v.id)>=0);if(v)return {task:'donate',target:v.id,reason:v.name+' needs a willing donor.'};}
  if(p.species==='vampire'&&!night())return {task:'rest',reason:'Waiting under cover until nightfall.'};
  if(p.species==='human'&&p.hunger>72)return {task:'forage',reason:'Hunger takes priority.'};
  if(p.role==='builder'&&state.roof<3)return {task:state.wood>=3?'build':'wood',reason:'The camp needs a sound roof.'};
  if(state.food<members().filter(p=>p.species==='human').length*1.6)return {task:'forage',reason:'The shared food reserve is low.'};
  if(state.roof<3&&state.wood<3)return {task:'wood',reason:'Shelter repairs need timber.'};
  if(p.species==='vampire'||p.role==='watcher')return {task:'guard',reason:'Keeping the camp safe.'};
  return {task:'rest',reason:'Taking a breather while supplies last.'};
 }
 function incident(){
  state.weather='Clear';state.weatherPenalty=0;const roll=dice();let text='',type='incident';
  if(roll<.13){state.weather='Rain';if(state.roof<3){for(const p of members())p.fatigue=clamp(p.fatigue+10);text='Rain finds the gaps in the roof. Everyone in camp loses some rest.';}else text='Rain drums on the repaired roof. For once, everyone stays dry.';}
  else if(roll<.24&&state.food>1){const lost=Math.min(state.food,2);state.food-=lost;text=lost+' meals have spoiled in the damp.';}
  else if(roll<.35&&state.shift>2){const workers=members().filter(p=>['wood','build','forage'].includes(p.job?.task));if(workers.length){const p=workers[Math.floor(dice()*workers.length)];p.health=clamp(p.health-19);text=p.name+' was cut by loose debris while working. Someone may need to care for them.';remember(p,'I was injured while working.',null,-5);}}
  else if(roll<.45){state.food+=2;text='A patch of late berries is found beside the camp. Two meals, without an expedition.';}
  else if(roll<.54){state.wood+=2;text='The river leaves two bundles of useful driftwood.';}
  else if(roll<.64){state.weather='Fog';state.weatherPenalty=1;text='A thick fog settles in. Food will be harder to find this shift.';}
  else if(roll<.73&&state.shift>3){if(state.guarded)text='A stranger tests the camp boundary. The watch sends them away.';else{state.food=Math.max(0,state.food-2);text='With nobody on watch, a stranger takes up to two meals from the stores.';}}
  else text='A quiet shift. The camp has time to live with its own decisions.';
  const id=log(type,text||'Nothing new troubles the camp.');state.lastIncident={id,text:text||'Nothing new troubles the camp.',shift:state.shift};
 }
 function begin(){
  if(state.ended)return;state.shift++;state.running=true;state.phase=0;
  for(const p of state.people.filter(p=>!p.arrived&&p.arrival<=state.shift)){p.arrived=true;const text=p.species==='vampire'?p.name+' reaches the covered ruins. They need blood, shade, and somewhere to stay.':p.name+' reaches the camp, looking for food and a place to stay.';p.speech=p.species==='vampire'?'I cannot work in sunlight. I can help after dark.':'I can help, if there is a place for me.';log('arrival',text,[p.id]);}
  for(const p of present()){p.hunger=clamp(p.hunger+(p.species==='vampire'?16:17));p.job=choose(p);}
  incident();
 }
 function finish(){
  const busy=new Set();const ppl=present();
  for(const p of ppl.filter(p=>p.joined&&p.job?.task==='donate')){
   const v=person(p.job.target);if(!v?.alive||!v.joined||v.species!=='vampire'||busy.has(v.id)||p.health<45){p.job={task:'rest',reason:'The feeding arrangement is not safe right now.'};continue;}
   p.health=clamp(p.health-13);p.fatigue=clamp(p.fatigue+17);v.hunger=clamp(v.hunger-70);v.fatigue=clamp(v.fatigue+7);busy.add(p.id);busy.add(v.id);v.job={task:'receive',reason:p.name+' offered blood freely.'};
   const id=log('aid',p.name+' gave blood to '+v.name+'. '+p.name+' is weaker; '+v.name+' owes them help.',[p.id,v.id]);addDebt(v,p,id);p.speech='I agreed to this. I will need rest afterward.';v.speech='You helped me. I will remember that.';state.storyCount++;
  }
  for(const p of ppl.filter(p=>p.joined&&p.job?.task==='care'&&!busy.has(p.id))){
   if(busy.has(p.id))continue;
   const target=person(p.job.target)||members().filter(t=>t.health<90).sort((a,b)=>a.health-b.health)[0];
   if(target&&target.health<95&&state.food>0&&!busy.has(target.id)){
    state.food--;target.health=clamp(target.health+26);target.fatigue=clamp(target.fatigue-15);target.job={task:'rest',reason:p.name+' is tending their wounds.'};busy.add(target.id);busy.add(p.id);p.fatigue=clamp(p.fatigue+12);
    const id=log('aid',p.name+' used a meal to tend '+target.name+' instead of ordinary work.',[p.id,target.id]);addDebt(target,p,id);fulfill(target,'rest',id);state.storyCount++;
   }else p.job={task:'rest',reason:state.food===0?'Care needs one meal for recovery.':'Nobody needs treatment right now.'};
  }
  let guards=0;
  for(const p of ppl){
   if(busy.has(p.id))continue;
   if(!p.joined){p.hunger=clamp(p.hunger-13);p.fatigue=clamp(p.fatigue-10);continue;}
   const j=p.job||{task:'rest'};let eventId=null;
   if(j.task==='rest'){p.fatigue=clamp(p.fatigue-39);p.health=clamp(p.health+3);fulfill(p,'rest');}
   else if(j.task==='forage'){const n=Math.max(0,2+(dice()>.5?1:0)-state.weatherPenalty);state.food+=n;p.fatigue=clamp(p.fatigue+16);eventId=log('work',p.name+' brought back '+n+' meals.',[p.id]);if(dice()<.065){p.health=clamp(p.health-12);log('injury',p.name+' was scratched in the brambles.',[p.id],eventId);}}
   else if(j.task==='wood'){state.wood+=3;p.fatigue=clamp(p.fatigue+19);log('work',p.name+' brought back three timber.',[p.id]);}
   else if(j.task==='build'){if(state.wood>=3&&state.roof<3){state.wood-=3;state.roof++;p.fatigue=clamp(p.fatigue+18);log('shelter',p.name+' repaired the shelter: '+state.roof+' of 3 sections are sound.',[p.id]);}else{p.fatigue=clamp(p.fatigue-10);p.speech=state.roof>=3?'The roof is already sound.':'I cannot repair the roof without three timber.';log('refusal',p.name+': '+p.speech,[p.id]);}}
   else if(j.task==='guard'){guards++;p.fatigue=clamp(p.fatigue+15);}
  }
  state.guarded=guards>0;
  let eaters=members().filter(p=>p.species==='human'&&p.hunger>=38);
  eaters.sort((a,b)=>state.policy==='promises'?((state.promises.some(o=>o.to===b.id&&o.status==='open'&&o.type==='meal')?1:0)-(state.promises.some(o=>o.to===a.id&&o.status==='open'&&o.type==='meal')?1:0)||b.hunger-a.hunger):state.policy==='you'?((a.id==='you'?-1:0)-(b.id==='you'?-1:0)||b.hunger-a.hunger):b.hunger-a.hunger);
  const nia=person('nia');if(nia.arrived&&nia.joined&&nia.alive&&!nia.left&&nia.hunger>=78&&state.food>0&&eaters[0]?.id!=='nia'){
   const victim=eaters[0];state.food--;nia.hunger=clamp(nia.hunger-50);eaters=eaters.filter(p=>p.id!=='nia');victim.trust.nia=clamp(trust(victim,'nia')-24,-100,100);const id=log('conflict','Nia took the meal set aside for '+victim.name+'. '+victim.name+' will remember the theft.',['nia',victim.id]);remember(victim,'Nia took my reserved meal.',id,-12);state.storyCount++;
  }
  for(const p of eaters){if(state.food<=0)break;state.food--;p.hunger=clamp(p.hunger-50);fulfill(p,'meal');}
  for(const p of members()){
   if(p.hunger>=90){p.health=clamp(p.health-13);const id=log('need',p.name+' is weakening from '+(p.species==='vampire'?'blood hunger':'hunger')+'.',[p.id]);remember(p,'My need went unmet.',id,-5);}
   if(p.fatigue>=95)p.health=clamp(p.health-5);
   if(p.health<=0){p.alive=false;const id=log('death',p.name+' died. The camp has lost someone it cannot replace.',[p.id]);for(const w of members().filter(w=>w.id!==p.id))remember(w,p.name+' died in our camp.',id,-13);}
  }
  for(const o of state.promises.filter(o=>o.status==='open'&&o.due<=state.shift)){
   const p=person(o.to);if(!p.alive||p.left){o.status='ended';continue;}o.status='broken';p.trust.you=clamp(trust(p)-30,-100,100);const id=log('broken',p.name+': “You promised '+(o.type==='meal'?'me a meal':'me time to rest')+'. I remember.”',['you',p.id],o.eventId);remember(p,'You broke your '+o.type+' promise.',id,-17);p.speech='You broke your '+o.type+' promise. I need a reason to trust you.';state.storyCount++;
  }
  for(const p of members().filter(p=>p.id!=='you'))if(trust(p)<=-45&&(p.hunger>70||p.memories.some(m=>m.text.startsWith('You broke')))){p.left=true;log('departure',p.name+' left because your broken promises made this camp feel unsafe.',['you',p.id]);}
  state.running=false;state.phase=0;
  if(!person('you').alive||state.shift>=12){state.ended=true;state.paused=true;const friends=members().filter(p=>p.id!=='you').length;state.result={won:person('you').alive&&state.roof===3&&friends>=3&&state.food>=3,alive:person('you').alive,friends,food:state.food,roof:state.roof};log('ending',!person('you').alive?'Your first life in this camp has ended.':state.result.won?'Two days survived. A camp has become a place people choose to stay.':'Two days survived. The camp’s future is still unsettled.');}
 }
 function command(cmd){
  if(!cmd||typeof cmd.type!=='string')return {ok:false,message:'A command needs a type.'};
  if(cmd.type==='restart'){Object.assign(state,world(Number(cmd.seed)||8291));dice=rng(state.seed^0x9175af3);welcome();return {ok:true,message:'A new camp begins.'};}
  if(cmd.type==='pause'){state.paused=cmd.value??!state.paused;return {ok:true,message:state.paused?'Paused.':'Time is running.'};}
  if(cmd.type==='speed'){if(![1,3,8].includes(cmd.value))return {ok:false,message:'Choose 1, 3, or 8.'};state.speed=cmd.value;return {ok:true,message:'Speed changed.'};}
  if(cmd.type==='policy'){if(!['need','promises','you'].includes(cmd.value))return {ok:false,message:'Unknown ration policy.'};state.policy=cmd.value;return {ok:true,message:'Ration plan updated.'};}
  if(cmd.type==='step'){if(state.ended)return {ok:false,message:'This camp’s story is finished.'};if(!state.running)begin();finish();state.paused=true;return {ok:true,message:'One shift completed.'};}
  if(state.ended)return {ok:false,message:'Begin a new camp to play again.'};
  const p=person(cmd.actorId);if(!p||!p.arrived||!p.alive||p.left)return {ok:false,message:'That person is not available.'};
  if(cmd.type==='invite'){
   if(p.joined)return {ok:false,message:p.name+' already lives here.'};
   p.joined=true;p.trust.you+=8;promise(p,p.species==='vampire'?'rest':'meal');const id=log('join',p.name+' agreed to stay. You owe them '+(p.species==='vampire'?'protected rest':'a meal')+'.',['you',p.id]);remember(p,'You offered me a place here.',id,4);p.speech='I will stay. Let us see whether this works.';return {ok:true,message:p.name+' joined the camp.'};
  }
  if(!p.joined)return {ok:false,message:'Invite '+p.name+' to stay before requesting camp work.'};
  if(cmd.type==='promise'){
   if(p.id==='you')return {ok:false,message:'Choose another person to make a promise.'};if(!['meal','rest'].includes(cmd.offer))return {ok:false,message:'Offer a meal or rest.'};if(cmd.offer==='meal'&&p.species==='vampire')return {ok:false,message:'Ordinary food cannot satisfy blood hunger. Offer blood or protected rest.'};promise(p,cmd.offer);return {ok:true,message:'Promise recorded. It is due within two shifts.'};
  }
  if(cmd.type==='request'||cmd.type==='intent'){
   if(!TASKS[cmd.task])return {ok:false,message:'Unknown activity.'};
   if(cmd.task==='care'&&cmd.target){const t=person(cmd.target);if(!t||!t.arrived||!t.joined||!t.alive||t.left||t.id===p.id)return {ok:false,message:'Care needs another living member of this camp.'};}
   const red=unsafe(p,cmd.task);if(red){p.speech=red;log('refusal',p.name+': '+red,[p.id]);return {ok:false,message:red};}
   if(cmd.offer&&!['meal','rest'].includes(cmd.offer))return {ok:false,message:'Unknown offer.'};
   if(cmd.offer==='meal'&&p.species==='vampire')return {ok:false,message:'A vampire needs blood, not an ordinary meal.'};
   if(p.id!=='you'){const a=acceptance(p,cmd.task,cmd.offer);p.speech=a.reason;log(a.ok?'agreement':'refusal',p.name+': '+a.reason,['you',p.id]);if(!a.ok)return {ok:false,message:a.reason};if(cmd.offer)promise(p,cmd.offer);}
   p.intent={task:cmd.task,target:cmd.target};return {ok:true,message:(p.id==='you'?'Your intention':p.name+'’s accepted request')+': '+TASKS[cmd.task]+'. It begins next shift.'};
  }
  if(cmd.type==='donate'){
   const v=person(cmd.target);if(!v||!v.arrived||!v.alive||v.left||!v.joined||v.species!=='vampire'||p.species!=='human')return {ok:false,message:'Choose a human donor and a vampire who lives here.'};
   if(p.health<60||p.fatigue>78)return {ok:false,message:p.name+' is too weak or tired to give blood safely.'};
   if(p.id!=='you'&&(mood(p)<35||trust(p)<-10||trust(p,v.id)<-20)){p.speech='I do not consent to that.';log('refusal',p.name+' refused to give blood.',['you',p.id,v.id]);return {ok:false,message:p.name+' does not consent.'};}
   p.intent={task:'donate',target:v.id};p.speech=p.id==='you'?'I will give blood, then I will need rest.':'I agree to give blood.';return {ok:true,message:'Consensual feeding arranged for next shift. Donor loses 13 health.'};
  }
  return {ok:false,message:'Unknown command.'};
 }
 function welcome(){log('begin','You woke beside a ruined shelter with nothing. Mira is waiting nearby. A first meal could become a first friend.',['you','mira']);}
 function observe(){return structuredClone({version:1,seed:state.seed,shift:state.shift,phase:state.phase,paused:state.paused,speed:state.speed,night:night(),weather:state.weather,resources:{food:state.food,wood:state.wood,shelter:state.roof},policy:state.policy,ended:state.ended,result:state.result,people:state.people.filter(p=>p.arrived).map(p=>({...p,mood:Math.round(mood(p)),desire:desire(p),traitDescriptions:p.traits.map(t=>descriptions[t]),trustLabel:trust(p)>25?'Trusting':trust(p)<-20?'Distrustful':'Getting to know you'})),promises:state.promises,events:state.events,locations:PLACES});}
 function elapse(seconds){if(state.paused||state.ended)return;if(!state.running)begin();state.phase+=seconds*state.speed/18;if(state.phase>=1)finish();}
 welcome();return {state,command,observe,elapse,person,night,mood};
}
