const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname,'../..');
function serve(port=8797){
  const server=http.createServer((request,response)=>{
    let name;try{name=decodeURIComponent(new URL(request.url,'http://localhost').pathname);}catch{response.writeHead(400).end();return;}
    if(name==='/')name='/Docs/Art/character-art-gallery.html';
    const documents=['/Docs/Art/character-art-gallery.html','/Docs/Art/character-art-status.html','/Docs/Art/CharacterArtStatus.json'];
    if(!documents.includes(name)&&!name.startsWith('/Assets/Resources/UI/')&&!name.startsWith('/Assets/Art/Generated/CharacterIcons/')){response.writeHead(403).end();return;}
    const file=path.resolve(root,'.'+name);if(!file.startsWith(root+path.sep)){response.writeHead(403).end();return;}
    fs.readFile(file,(error,bytes)=>{if(error){response.writeHead(404).end();return;}
      response.writeHead(200,{'Content-Type':({'.png':'image/png','.json':'application/json','.html':'text/html; charset=utf-8'})[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});response.end(bytes);});
  });
  return new Promise(resolve=>server.listen(port,'127.0.0.1',()=>resolve(server)));
}
module.exports={serve};
if(require.main===module)serve(Number(process.env.KANJI_ART_PORT||8797)).then(server=>console.log(`Art gallery: http://127.0.0.1:${server.address().port}/`));
