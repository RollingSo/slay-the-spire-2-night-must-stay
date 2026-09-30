// Mechanical alpha-bound normalization; no artwork recoloring or repainting.
const sharp = require('C:/Users/wenti/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
async function bounds(path) {
 const {data,info}=await sharp(path).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let left=info.width,top=info.height,right=0,bottom=0;
 for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>16){left=Math.min(left,x);right=Math.max(right,x);top=Math.min(top,y);bottom=Math.max(bottom,y);}
 let total=0,count=0;
 for(let y=Math.max(top,bottom-30);y<=bottom;y++)for(let x=left;x<=right;x++)if(data[(y*info.width+x)*4+3]>16){total+=x;count++;}
 return {left,top,width:right-left+1,height:bottom-top+1,bottom,ground:total/count};
}
(async()=>{
 const original=await bounds('duchess_assets/combat_rig/duchess_combat_character.png');
 const source=process.argv[2], next=await bounds(source), scale=original.height/next.height;
 const image=await sharp(source).extract({left:next.left,top:next.top,width:next.width,height:next.height}).resize({height:original.height}).png().toBuffer();
 // Wider transparent canvas accommodates the raised cape; centered Sprite2D
 // cancels the extra 300 px on each side, preserving the original ground anchor.
 const left=300+Math.round(original.ground-(next.ground-next.left)*scale),top=original.bottom-original.height+1;
 await sharp({create:{width:2200,height:1280,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:image,left,top}]).png().toFile('duchess_assets/combat_rig/duchess_block.png');
 console.log({original,newBounds:await bounds('duchess_assets/combat_rig/duchess_block.png')});
})().catch(error=>{console.error(error);process.exit(1);});
