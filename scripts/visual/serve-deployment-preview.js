const http = require("http");
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "../../tmp/deployment-preview");
const port = Number(process.env.PORT || 8795);
const contentTypes = {
  ".html": "text/html; charset=utf-8",
  ".js": "application/javascript",
  ".css": "text/css",
  ".wasm": "application/wasm",
  ".png": "image/png",
};

http.createServer((req, res) => {
  let pathname;
  try { pathname = decodeURIComponent(new URL(req.url, "http://localhost").pathname); }
  catch { res.writeHead(400).end(); return; }
  if (pathname === "/") pathname = "/game/index.html";
  let filename = path.resolve(root, "." + pathname);
  if (!filename.startsWith(root + path.sep)) { res.writeHead(403).end(); return; }
  if (fs.existsSync(filename) && fs.statSync(filename).isDirectory()) filename = path.join(filename, "index.html");
  fs.readFile(filename, (error, data) => {
    if (error) { res.writeHead(404).end(); return; }
    res.writeHead(200, { "Content-Type": contentTypes[path.extname(filename)] || "application/octet-stream", "Cache-Control": "no-store" });
    res.end(data);
  });
}).listen(port, "127.0.0.1", () => console.log(`Deployment preview: http://127.0.0.1:${port}/game/index.html`));
