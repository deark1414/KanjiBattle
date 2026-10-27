const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const http = require("node:http");
const { spawn } = require("node:child_process");
const { chromium } = require("playwright");
const { GameHarness } = require("./harness");
const scenarios = require("./scenarios");

const root = path.resolve(__dirname, "../..");
const args = process.argv.slice(2);
const option = name => args.includes(name) ? args[args.indexOf(name) + 1] : undefined;
const buildRoot = path.resolve(root, option("--build-root") || "tmp/e2e");
const unity = process.env.UNITY_EDITOR || "/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity";

async function build() {
  fs.mkdirSync(buildRoot, { recursive: true });
  assert(fs.existsSync(unity), "Unity editor not found; set UNITY_EDITOR to its executable.");
  const log = path.join(buildRoot, "build.log");
  console.log("E2E専用WebGL版をビルドします。");
  await new Promise((resolve, reject) => {
    const child = spawn(unity, ["-batchmode", "-nographics", "-noaudio", "-quit", "-projectPath", root,
      "-executeMethod", "KanjiBattle.Editor.WebGLBuild.BuildLocalE2E", "-e2eBuildRoot", buildRoot, "-logFile", log], { cwd: root, stdio: "ignore" });
    child.on("error", reject);
    child.on("exit", code => code === 0 ? resolve() : reject(new Error(`Unity build failed (${code}). See ${log}`)));
  });
}

async function serve() {
  const marker = JSON.parse(fs.readFileSync(path.join(buildRoot, "build.json"), "utf8"));
  assert.equal(marker.define, "KANJI_BATTLE_E2E", "E2E build marker is missing or invalid.");
  const server = http.createServer((request, response) => {
    const pathname = decodeURIComponent(new URL(request.url, "http://localhost").pathname);
    const filename = path.resolve(buildRoot, `.${pathname}`);
    if (!filename.startsWith(buildRoot + path.sep)) { response.writeHead(403).end(); return; }
    fs.readFile(filename, (error, data) => {
      if (error) { response.writeHead(404).end(); return; }
      const mime = { ".html": "text/html", ".css": "text/css", ".js": "application/javascript", ".wasm": "application/wasm",
        ".png": "image/png", ".ico": "image/x-icon", ".json": "application/json" }[path.extname(filename)];
      response.writeHead(200, { "Content-Type": mime || "application/octet-stream", "Cache-Control": "no-store" }); response.end(data);
    });
  });
  await new Promise(resolve => server.listen(0, "127.0.0.1", resolve));
  return { server, url: `http://127.0.0.1:${server.address().port}/game/index.html` };
}

function html(report) {
  const escape = value => String(value).replace(/[&<>"']/g, character => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[character]);
  return `<!doctype html><html lang="ja"><meta charset="utf-8"><title>融合 E2E テスト結果</title>
  <style>body{font:16px sans-serif;max-width:1000px;margin:32px auto;padding:0 16px;color:#202922}table{border-collapse:collapse;width:100%}td,th{padding:12px;border-bottom:1px solid #ccc;text-align:left}pre{white-space:pre-wrap;overflow-wrap:anywhere}.passed{color:#176333}.failed{color:#a12121}</style>
  <h1>融合 E2E テスト結果</h1><p>${escape(report.startedAt)} / ${report.results.filter(result => result.status === "passed").length} 成功 / ${report.results.filter(result => result.status === "failed").length} 失敗</p>
  <table><tr><th>シナリオ</th><th>結果</th><th>記録</th></tr>${report.results.map(result => `<tr><td>${escape(result.name)}</td><td class="${result.status}">${result.status === "passed" ? "成功" : "失敗"}</td><td><a href="${result.id}/trace.zip">操作記録</a> / <a href="${result.id}/state.json">状態</a>${result.status === "failed" ? ` / <a href="${result.id}/failure.png">画面</a><details><summary>詳細</summary><pre>${escape(result.error)}</pre></details>` : ""}</td></tr>`).join("")}</table></html>`;
}

async function main() {
  if (args.includes("--list")) { scenarios.forEach(scenario => console.log(`${scenario.id}\t${scenario.name}`)); return; }
  if (args.includes("--doctor")) {
    assert(fs.existsSync(unity), "Unity editor is missing.");
    assert(fs.existsSync(chromium.executablePath()), "Chromium is missing. Run npm run install:browsers.");
    console.log(JSON.stringify({ unity, chromium: chromium.executablePath(), playwright: require("playwright/package.json").version, scenarios: scenarios.length }, null, 2)); return;
  }
  if (args.includes("--build")) await build();
  let server, browser;
  const output = path.resolve(option("--output") || path.join(buildRoot, "results"));
  fs.mkdirSync(output, { recursive: true });
  const report = { startedAt: new Date().toISOString(), results: [] };
  try {
    let url = option("--url");
    if (!url) ({ server, url } = await serve());
    browser = await chromium.launch({ headless: !args.includes("--headed"), args: ["--mute-audio"] });
    const only = option("--only")?.split(",");
    const selected = only ? scenarios.filter(scenario => only.includes(scenario.id)) : scenarios;
    assert(selected.length > 0, "No matching scenario.");
    for (const scenario of selected) {
      const directory = path.join(output, scenario.id); fs.mkdirSync(directory, { recursive: true });
      const context = await browser.newContext({ viewport: scenario.viewport || { width: 1280, height: 800 },deviceScaleFactor:scenario.deviceScaleFactor||1 });
      await context.tracing.start({ screenshots: true, snapshots: true, sources: true });
      const page = await context.newPage();
      const game = new GameHarness(page, directory);
      const result = { id: scenario.id, name: scenario.name, status: "failed" };
      const started = Date.now();
      console.log(`実行: ${scenario.name}`);
      try {
        await game.boot(url);
        await game.request("seed", scenario.fixture);
        await scenario.run(game);
        await game.request();
        assert.deepEqual(game.errors, [], "Browser or Unity runtime errors were detected.");
        await game.screenshot("final");
        result.status = "passed";
      } catch (error) {
        result.error = error.stack || String(error);
        await game.screenshot("failure").catch(() => {});
        console.error(result.error);
      } finally {
        result.durationMs = Date.now() - started;
        game.writeArtifacts();
        try { await context.tracing.stop({ path: path.join(directory, "trace.zip") }); }
        catch(error) {
          result.status="failed";
          result.error=[result.error,"Trace export failed: "+(error.stack||String(error))].filter(Boolean).join("\n");
          console.error(result.error);
        }
        await context.close();
        report.results.push(result);
        console.log(`${result.status}: ${scenario.id} (${(result.durationMs / 1000).toFixed(1)}秒)`);
        fs.writeFileSync(path.join(output, "report.json"), JSON.stringify(report, null, 2));
        fs.writeFileSync(path.join(output, "index.html"), html(report));
      }
    }
    if (report.results.some(result => result.status === "failed")) process.exitCode = 1;
    console.log(`結果: ${output}/index.html`);
  } finally {
    await browser?.close();
    if (server) await new Promise(resolve => server.close(resolve));
  }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
