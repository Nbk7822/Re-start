// Flat overhead ink drawings. All world coordinates share the same scale.
const INK='#171b16';
const CLOTH={you:'#d1a45b',mira:'#91ab8b',ilex:'#92788d',oren:'#b77d58',bram:'#75a0a0',nia:'#c19b6c',sera:'#9da96f',vale:'#73728f'};
const hair={you:'#403d32',mira:'#875b3e',ilex:'#302b37',oren:'#5a4533',bram:'#8d8060',nia:'#503e31',sera:'#705446',vale:'#353341'};
const noise=n=>{let t=Math.imul(n^0x45d9f3b,0x45d9f3b);return ((t^t>>>16)>>>0)/4294967295;};
let width=0,height=0,paper=null,paperSeed=null;

function cloakPath(vampire){return vampire?'M-11-4Q0-11 11-4L14 15 6 11 0 16-6 11-14 15Z':'M-9-3Q0-9 9-3L10 12 3 15-3 15-10 12Z';}
export function pawnIcon(p,portrait=false){
 const vampire=p.species==='vampire',cloth=CLOTH[p.id]||p.color;
 const mark=p.id==='you'?'<path d="M4 8H-3V3M-3 3-6 6M-3 3 0 6"/>':vampire?'<path d="M0 3 4 8 0 12-4 8Z"/>':p.role==='builder'?'<path d="M-4 8H4M0 4V12"/>':p.role==='carer'?'<path d="M-4 8H4M0 4V12"/>':'<path d="M-4 10Q0 1 4 5Q4 11-4 10ZM-3 10 2 6"/>';
 return `<svg class="mini-pawn ${portrait?'portrait-pawn':''} ${p.species}" viewBox="-17 -18 34 38" aria-hidden="true"><g fill="${cloth}" stroke="${INK}" stroke-width="1.7" stroke-linejoin="round" stroke-linecap="round"><path d="${cloakPath(vampire)}"/><path d="M-6-2-3 5M6-2 3 5" fill="none"/>${vampire?'<path d="M-10-4-5-9 0-3 5-9 10-4" fill="#423b49"/>':''}<g fill="none" stroke="${vampire?'#dfd1bb':'#3e4b3c'}" stroke-width="1.5">${mark}</g><circle cy="-7" r="6.1" fill="${vampire?'#d9d4cb':'#d3b58e'}"/><path d="M-6-8Q-4-16 3-13Q7-11 6-6Q3-7 1-10Q-2-7-6-8Z" fill="${hair[p.id]||'#514234'}"/>${p.id==='you'?'<path d="M-5-10Q0-12 5-10" fill="none" stroke="#e8c878"/>':''}</g></svg>`;
}

export function drawWorld(canvas,ctx,sim,selected){
 const box=canvas.getBoundingClientRect(),dpr=Math.min(devicePixelRatio||1,2);
 if(width!==box.width||height!==box.height){width=box.width;height=box.height;canvas.width=width*dpr;canvas.height=height*dpr;paper=null;}
 ctx.setTransform(dpr,0,0,dpr,0,0);ctx.imageSmoothingEnabled=true;ctx.lineJoin='round';ctx.lineCap='round';
 const u=Math.min(width/25.5,height/17.8),ox=(width-u*24)/2,oy=(height-u*18)/2;
 const X=x=>ox+x*u,Y=y=>oy+y*u;
 const finish=(fill,line=INK,w=1.7)=>{if(fill){ctx.fillStyle=fill;ctx.fill();}if(line){ctx.strokeStyle=line;ctx.lineWidth=w;ctx.stroke();}};
 const polygon=(points,fill,line=INK,w=1.7)=>{ctx.beginPath();points.forEach(([x,y],i)=>i?ctx.lineTo(X(x),Y(y)):ctx.moveTo(X(x),Y(y)));ctx.closePath();finish(fill,line,w);};
 const line=(x1,y1,x2,y2,color=INK,w=1)=>{ctx.beginPath();ctx.moveTo(X(x1),Y(y1));ctx.lineTo(X(x2),Y(y2));finish(null,color,w);};
 const circle=(x,y,r,fill,lineColor=INK,w=1.5)=>{ctx.beginPath();ctx.arc(X(x),Y(y),r*u,0,Math.PI*2);finish(fill,lineColor,w);};
 const rect=(x,y,w,h,fill,r=1.5,lineColor=INK,lw=1.7)=>{ctx.beginPath();ctx.roundRect(X(x),Y(y),w*u,h*u,r);finish(fill,lineColor,lw);};
 const strokeCurve=(points,color,w)=>{ctx.beginPath();ctx.moveTo(X(points[0][0]),Y(points[0][1]));for(let i=1;i<points.length;i+=3)ctx.bezierCurveTo(X(points[i][0]),Y(points[i][1]),X(points[i+1][0]),Y(points[i+1][1]),X(points[i+2][0]),Y(points[i+2][1]));finish(null,color,w);};

 ctx.fillStyle='#82947b';ctx.fillRect(0,0,width,height);
 // Interlocking, flat washes of grass and moss; no raised terrain or shadows.
 polygon([[-5,-1],[7,-1],[9,2],[7,5],[3,6],[2,11],[-5,12]],'#6c8575',null);
 polygon([[17,-2],[28,-2],[28,20],[15,20],[17,15],[16,11],[19,7]],'#6c8679',null);
 polygon([[5,5],[9,4.5],[13,5],[16,7],[17,11],[15,14],[10,14.5],[6,12],[5.5,8]],'#a5a184',null);
 polygon([[1,12],[5,11],[7,14],[10,16],[8,20],[-3,20]],'#8a9778',null);
 // A subtle planning grid uses square cells, without pixel-art tiles.
 ctx.strokeStyle='#283d3020';ctx.lineWidth=.6;ctx.beginPath();
 for(let x=-6;x<31;x++){ctx.moveTo(X(x),0);ctx.lineTo(X(x),height);}for(let y=-2;y<21;y++){ctx.moveTo(0,Y(y));ctx.lineTo(width,Y(y));}ctx.stroke();
 for(let i=0;i<120;i++){
  const x=-4+noise(i*13+2)*31,y=noise(i*17+3)*19;
  if(x>8&&x<18&&y>5&&y<14)continue;
  const c=i%3===0?'#b7b18e':'#435e4b';
  line(x,y,x+.08,y-.2,c,.8);line(x+.11,y+.02,x+.2,y-.13,c,.8);
 }
 // The river is a map shape with ink marks on its surface, never a beveled bank.
 ctx.beginPath();ctx.moveTo(X(21.2),-15);ctx.bezierCurveTo(X(19.4),Y(2),X(22.5),Y(4),X(21),Y(7));ctx.bezierCurveTo(X(19.6),Y(10),X(22),Y(13),X(20.7),height+25);ctx.lineTo(width+20,height+20);ctx.lineTo(width+20,-15);ctx.closePath();finish('#467b80',INK,2.2);
 for(let row=0;row<9;row++)for(let col=0;col<3;col++){
  const x=21.8+col*.9+(row%2)*.2,y=row*2.15+.4;
  strokeCurve([[x,y],[x+.12,y-.1],[x+.28,y-.1],[x+.4,y]],'#b5c4b0',.8);
  if(col===1)line(x+.04,y+.2,x+.37,y+.2,'#294f53',.7);
 }
 // A worn tile path makes the work sites legible at a glance.
 const road=[[14,19],[14,16],[13,15],[13,13],[12,12],[11,11],[11,9],[10,8],[9,7],[8,6],[7,5],[6,5]];
 for(const [x,y]of road){rect(x,y,1.02,1.02,'#b8a37b',0,'#554f3c80',.6);line(x+.15,y+.74,x+.45,y+.8,'#70694b',.8);}

 function tree(x,y,r=1,type=0,seed=1){
  const px=X(x),py=Y(y),s=r*u;
  if(type===2){
   for(let j=0;j<5;j++){
    const a=j*Math.PI*.4+seed*.4,dx=Math.cos(a),dy=Math.sin(a);
    line(x,y,x+dx*r*.85,y+dy*r*.85,INK,2.2);
    line(x+dx*r*.5,y+dy*r*.5,x+dx*r*.55-dy*r*.27,y+dy*r*.55+dx*r*.27,INK,1.4);
   }circle(x,y,r*.15,'#b7ad8e');return;
  }
  const count=type===1?14:18;
  ctx.beginPath();
  for(let j=0;j<count;j++){
   const a=j/count*Math.PI*2-.4,spike=type===1?(j%2===0?1:.58):(.77+noise(seed*11+j)*.29);
   const xx=px+Math.cos(a)*s*spike,yy=py+Math.sin(a)*s*spike;
   j?ctx.lineTo(xx,yy):ctx.moveTo(xx,yy);
  }ctx.closePath();finish(type===1?'#3e655b':seed%3===0?'#667b5d':'#557b64',INK,2);
  // A carved, radial branch motif makes each canopy an ink stamp.
  const branches=type===1?5:4;
  for(let j=0;j<branches;j++){
   const a=j*Math.PI*2/branches+.5;
   const dx=Math.cos(a),dy=Math.sin(a),ex=x+dx*r*.67,ey=y+dy*r*.67;
   line(x+dx*r*.08,y+dy*r*.08,ex,ey,'#233e32',1);
   line(x+dx*r*.4,y+dy*r*.4,x+dx*r*.47-dy*r*.17,y+dy*r*.47+dx*r*.17,'#233e32',.9);
  }
  for(let j=0;j<4;j++){
   const a=j*.65+seed*.2;line(x+Math.cos(a)*r*.67,y+Math.sin(a)*r*.67,x+Math.cos(a+.08)*r*.82,y+Math.sin(a+.08)*r*.82,'#acb18b',.9);
  }
 }
 const trees=[[-3,2,1.25,1],[-1,5,1.25,0],[-2,11,1.3,1],[0,1,1.1,1],[1.5,3,1.12,0],[3.7,2.1,1.15,0],[5.8,1.1,1.1,1],[8.2,2.2,1.2,0],[2.8,5.4,.94,1],[.5,7.6,1.3,0],[3.7,8.2,1.05,0],[1,10,.75,2],[1.7,14.7,1.2,0],[4.8,16.5,1.28,1],[7.8,15.5,.88,0],[9.8,18.4,1.24,0],[16.4,2.5,1.2,1],[18.4,4.5,.98,0],[18.8,8.9,.75,2],[17.6,13.6,1.1,0],[18,16.4,1.18,1],[.2,17.8,1.1,1]];
 trees.forEach((t,i)=>tree(...t,i+1));

 function stone(x,y,r=.35,seed=1){
  const points=Array.from({length:6},(_,i)=>{const a=i*Math.PI/3;return [x+Math.cos(a)*r*(.8+noise(seed+i)*.3),y+Math.sin(a)*r*(.8+noise(seed+7+i)*.3)];});
  polygon(points,'#a3a38c',INK,1.5);line(x-r*.45,y+r*.2,x+r*.25,y-r*.32,'#606b59',.8);
 }
 stone(17.7,5.9,.42,3);stone(18.5,10.5,.32,9);stone(7.2,11.8,.3,13);stone(19,15,.37,18);
 // Shelter: floor squares and walls seen directly from above, doors cut into plan.
 for(let y=6;y<9;y++)for(let x=9;x<14;x++){
  rect(x,y,1,1,'#b2a78a',0,'#746c55',.6);
  if((x+y)%2===0){line(x+.13,y+.2,x+.76,y+.2,'#8b8164',.7);line(x+.2,y+.64,x+.64,y+.64,'#8b8164',.7);}
 }
 const wall=(x,y,w,h,fill='#b3b09b')=>{
  rect(x,y,w,h,fill,0,INK,2.1);
  if(w>h)for(let i=0;i<Math.floor(w);i++)line(x+i+.32,y+.04,x+i+.11,y+h-.04,'#595f51',.8);
  else for(let i=0;i<Math.floor(h);i++)line(x+.04,y+i+.3,x+w-.04,y+i+.1,'#595f51',.8);
 };
 wall(8.8,5.8,5.4,.38);wall(8.8,6.18,.38,3.05);wall(13.82,6.18,.38,3.05);wall(8.8,8.85,1.65,.38);wall(12.6,8.85,1.6,.38);
 rect(10.5,8.87,2,.33,'#867754',0,INK,1.3);line(11.4,8.94,11.6,9.08,'#d0bd8e',1);
 for(let i=0;i<sim.state.roof;i++){
  const x=9.35+i*1.43;
  // Roof coverage is an overhead cross-hatched panel, with no visible front face.
  rect(x,6.42,1.2,1.95,'#9c7954',0,INK,1.5);
  for(let j=0;j<5;j++)line(x+.07,6.65+j*.32,x+1.13,6.65+j*.32,'#423d30',.85);
  line(x+.15,6.5,x+.15,8.25,'#d4b580',.75);
 }
 // The covered ruin has an old seal inscribed in a flat stone floor.
 rect(14.65,6.3,2.6,2.5,'#888f7a',0,'#535f50',1);
 wall(14.45,6.1,3,.36);wall(14.45,6.46,.35,2.55);wall(17.1,6.46,.35,1.1);wall(14.45,8.8,.8,.35);wall(16.7,8.8,.75,.35);
 polygon([[15.93,6.8],[16.6,7.47],[15.93,8.14],[15.26,7.47]],null,'#35483e',1.4);
 circle(15.93,7.47,.35,null,'#35483e',1.2);line(15.93,6.9,15.93,8.04,'#35483e',.9);
 // Bedrolls and supplies read as icons on the same plane as the floor.
 rect(14.9,8,.57,.7,'#b6af8b',1);rect(14.96,8.03,.45,.15,'#d6ccb1',1);
 function timber(x,y,len=1.6,angle=0){
  ctx.save();ctx.translate(X(x),Y(y));ctx.rotate(angle);ctx.beginPath();ctx.roundRect(-len*u/2,-u*.16,len*u,u*.32,2);finish('#ad8b5e',INK,1.7);
  ctx.beginPath();ctx.moveTo(-len*u*.37,-u*.03);ctx.lineTo(len*u*.36,-u*.03);finish(null,'#5c4b34',.8);
  for(const end of [-1,1]){ctx.beginPath();ctx.moveTo(end*len*u*.41,-u*.12);ctx.lineTo(end*len*u*.41,u*.12);finish(null,INK,1);}
  ctx.restore();
 }
 timber(5.7,4.1,1.8,.13);timber(6.3,4.55,1.8,-.08);timber(5.5,4.9,1.5,.06);
 for(const [x,y]of [[3.5,11.8],[4.7,12.3],[3.9,12.85],[5.25,11.45]]){
  polygon([[x-.5,y],[x-.3,y-.3],[x+.1,y-.45],[x+.5,y-.13],[x+.4,y+.28],[x-.1,y+.37]],'#60795a',INK,1.6);
  for(const [dx,dy]of [[-.17,-.1],[.17,-.13],[.05,.16]])circle(x+dx,y+dy,.085,'#9f5756',INK,.8);
  line(x-.32,y+.2,x+.27,y-.19,'#293f30',.9);
 }
 rect(10,10.45,.75,.62,'#a78759',0,INK,1.7);line(10.05,10.5,10.69,11.01,INK,.9);line(10.7,10.5,10.05,11.01,INK,.9);
 circle(10.65,10.15,.22,'#b19b6f');circle(10.65,10.15,.13,'#d2b775',INK,.8);
 // The hearth is a ring of stones and coals, viewed from above.
 for(let i=0;i<7;i++){const a=i*Math.PI*2/7;stone(12.15+Math.cos(a)*.43,11.4+Math.sin(a)*.43,.12,i+27);}
 circle(12.15,11.4,.31,'#594a37',INK,1.2);timber(12.15,11.4,.55,.6);timber(12.15,11.4,.55,-.6);
 for(const [dx,dy]of [[-.12,-.07],[.1,.04],[-.05,.12]])circle(12.15+dx,11.4+dy,.065,'#ce8050',INK,.7);
 // Dried reeds, a boundary post, and hanging-free map marks add local character.
 for(const [x,y]of [[20.3,3.5],[20.2,11.5],[20.2,15.5]])for(let j=0;j<3;j++){
  line(x+j*.13,y,x+j*.13-.12,y-.5,'#333f32',1);circle(x+j*.13-.12,y-.53,.035,'#aa8656',INK,.65);
 }
 rect(15.6,14.9,.18,.48,'#87734e',1);line(15.39,14.93,15.99,14.93,INK,1.5);
 // A restrained, static paper grain unifies vector assets without pixel blocks.
 if(!paper||paperSeed!==sim.state.seed){
  paper=document.createElement('canvas');paper.width=Math.ceil(width);paper.height=Math.ceil(height);paperSeed=sim.state.seed;
  const pc=paper.getContext('2d');
  for(let i=0;i<2500;i++){
   const x=noise(i*7+sim.state.seed)*width,y=noise(i*11+53)*height;
   pc.strokeStyle=i%2?'#e5d6ac10':'#182b2210';pc.lineWidth=.65;pc.beginPath();pc.moveTo(x,y);pc.lineTo(x+.5+noise(i+83)*1.9,y+.3);pc.stroke();
  }
 }ctx.drawImage(paper,0,0,width,height);
 if(sim.night()){ctx.fillStyle='#18283c6b';ctx.fillRect(0,0,width,height);}
 if(sim.state.weather==='Fog'){ctx.fillStyle='#e1dbc52d';ctx.fillRect(0,0,width,height);}

 function label(text,x,y,isSelected=false,small=false){
  ctx.font=(isSelected?'600 ':'')+(small?'9':'11')+'px -apple-system,sans-serif';ctx.textAlign='center';
  const tw=ctx.measureText(text).width;ctx.beginPath();ctx.rect(x-tw/2-5,y-11,tw+10,16);finish(isSelected?'#d8b979':'#ddd4b8ed',INK,1.1);
  ctx.fillStyle=INK;ctx.fillText(text,x,y+1);
 }
 for(const [text,x,y]of [['BERRY GROUND',4.3,14],['FALLEN TIMBER',5.9,5.95],['SHELTER',11.5,5.35],[width<500?'RUINS':'COVERED RUINS',15.95,5.65],['SOUTHERN PATH',15.2,16.5]])label(text,X(x),Y(y),false,true);

 const boxes=[];
 for(const p of sim.state.people.filter(p=>p.arrived&&p.alive&&!p.left)){
  const i=sim.state.people.indexOf(p),home={x:9.7+(i%4)*1.8,y:10+Math.floor(i/4)*2.1};
  const places={forage:{x:4,y:12},wood:{x:6,y:4},build:{x:11.5,y:7.5},rest:{x:15.7,y:7.5},care:{x:12,y:10},guard:{x:15.5,y:14},donate:{x:12,y:10},receive:{x:12,y:10}};
  const dest=!p.joined?{x:17.5,y:10}:places[p.job?.task]||home;
  const goal={x:dest.x+(i%3-1)*.75,y:dest.y+Math.floor(i/3)*.6};
  let t=sim.state.running?(sim.state.phase<.4?sim.state.phase/.4:sim.state.phase>.78?(1-sim.state.phase)/.22:1):0;t=Math.max(0,Math.min(1,t));
  const x=X(home.x+(goal.x-home.x)*t),y=Y(home.y+(goal.y-home.y)*t),size=Math.max(23,Math.min(36,u*1.25));
  if(p.id===selected){ctx.beginPath();ctx.arc(x,y+size*.05,size*.62,0,Math.PI*2);finish(null,'#e1bd70',2);ctx.beginPath();ctx.arc(x,y+size*.05,size*.71,0,Math.PI*2);finish(null,INK,1);}
  ctx.save();ctx.translate(x,y);ctx.scale(size/32,size/32);
  ctx.beginPath();const shape=new Path2D(cloakPath(p.species==='vampire'));ctx.fillStyle=CLOTH[p.id]||p.color;ctx.strokeStyle=INK;ctx.lineWidth=1.9;ctx.fill(shape);ctx.stroke(shape);
  ctx.beginPath();ctx.moveTo(-6,-2);ctx.lineTo(-3,5);ctx.moveTo(6,-2);ctx.lineTo(3,5);finish(null,INK,1.25);
  if(p.species==='vampire'){
   ctx.beginPath();ctx.moveTo(-10,-4);ctx.lineTo(-5,-9);ctx.lineTo(0,-3);ctx.lineTo(5,-9);ctx.lineTo(10,-4);finish('#423b49',INK,1.4);
   ctx.beginPath();ctx.moveTo(0,3);ctx.lineTo(4,8);ctx.lineTo(0,12);ctx.lineTo(-4,8);ctx.closePath();finish(null,'#ddd0b8',1.4);
  }else{
   ctx.strokeStyle='#354737';ctx.lineWidth=1.4;ctx.beginPath();
   if(p.id==='you'){ctx.moveTo(4,8);ctx.lineTo(-3,8);ctx.lineTo(-3,3);ctx.moveTo(-6,6);ctx.lineTo(-3,3);ctx.lineTo(0,6);}
   else if(p.role==='carer'||p.role==='builder'){ctx.moveTo(-4,8);ctx.lineTo(4,8);ctx.moveTo(0,4);ctx.lineTo(0,12);}
   else{ctx.moveTo(-4,10);ctx.quadraticCurveTo(0,1,4,5);ctx.quadraticCurveTo(4,11,-4,10);ctx.moveTo(-3,10);ctx.lineTo(2,6);}
   ctx.stroke();
  }
  ctx.beginPath();ctx.arc(0,-7,6.1,0,Math.PI*2);finish(p.species==='vampire'?'#d9d4cb':'#d3b58e',INK,1.9);
  ctx.beginPath();ctx.moveTo(-6,-8);ctx.quadraticCurveTo(-4,-16,3,-13);ctx.quadraticCurveTo(7,-11,6,-6);ctx.quadraticCurveTo(3,-7,1,-10);ctx.quadraticCurveTo(-2,-7,-6,-8);finish(hair[p.id]||'#514234',INK,1.1);
  if(p.id==='you'){ctx.beginPath();ctx.moveTo(-5,-10);ctx.quadraticCurveTo(0,-12,5,-10);finish(null,'#e8c878',1.4);}ctx.restore();
  label(p.name+(p.id==='you'?' ↰':''),x,y-size*.62,p.id===selected);
  if(p.health<55||p.hunger>82){ctx.beginPath();ctx.moveTo(x+size*.4,y-size*.28);ctx.lineTo(x+size*.65,y-size*.28);ctx.lineTo(x+size*.52,y-size*.52);ctx.closePath();finish('#cb825d',INK,1);}
  boxes.push({id:p.id,x,y,r:Math.max(20,size*.66)});
 }
 return boxes;
}
