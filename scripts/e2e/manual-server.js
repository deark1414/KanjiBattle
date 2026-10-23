const fs = require("node:fs");
const path = require("node:path");
const http = require("node:http");

const buildRoot = path.resolve(__dirname, "../../tmp/e2e");
const marker = JSON.parse(fs.readFileSync(path.join(buildRoot, "build.json"), "utf8"));
if (marker.define !== "KANJI_BATTLE_E2E") throw new Error("Build the local E2E player first.");
const port = Number(process.env.KANJI_DEBUG_PORT || 8796);
const bridge = `<script>
const originalLog = console.log;
console.log = function(...items) {
  originalLog.apply(console, items);
  for (const value of items) if (typeof value === 'string' && value.startsWith('KANJI_E2E_PART:'))
    parent.postMessage({type:'kanji-debug-state',value},location.origin);
};
</script>`;

http.createServer((request, response) => {
  let pathname;
  try { pathname = decodeURIComponent(new URL(request.url, "http://localhost").pathname); }
  catch { response.writeHead(400).end(); return; }
  const filename = pathname === "/" ? path.join(__dirname, "manual.html") : path.resolve(buildRoot, `.${pathname}`);
  if (pathname !== "/" && !filename.startsWith(buildRoot + path.sep)) { response.writeHead(403).end(); return; }
  fs.readFile(filename, (error, bytes) => {
    if (error) { response.writeHead(404).end(); return; }
    if (pathname === "/game/index.html") {
      const source = bytes.toString();
      const hook = ".then((unityInstance) => {";
      if (!source.includes(hook)) { response.writeHead(500).end("Unity loader hook changed."); return; }
      bytes = Buffer.from(source.replace("<head>", "<head>" + bridge)
        .replace(hook, hook + " window.unityInstance = unityInstance;"));
    }
    const type = { ".html": "text/html; charset=utf-8", ".css": "text/css", ".js": "application/javascript", ".png": "image/png",
      ".ico": "image/x-icon", ".json": "application/json", ".wasm": "application/wasm" }[path.extname(filename)];
    response.writeHead(200, { "Content-Type": type || "application/octet-stream", "Cache-Control": "no-store" });
    response.end(bytes);
  });
}).listen(port, "127.0.0.1", () => console.log(`Manual debug: http://127.0.0.1:${port}/`));
