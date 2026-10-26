const { test } = require("node:test");
const assert = require("node:assert/strict");
const { EventEmitter } = require("node:events");
const fs = require("node:fs");
const path = require("node:path");
const { GameHarness } = require("./harness");
const scenarios = require("./scenarios");

test("failure artifacts survive browser closure before the first snapshot", () => {
  const directory = fs.mkdtempSync(path.join(require("node:os").tmpdir(), "kanji-no-state-"));
  try {
    const game = new GameHarness(new EventEmitter(), directory);
    game.errors.push("browser closed before startup");
    game.writeArtifacts();
    assert.equal(JSON.parse(fs.readFileSync(path.join(directory,"state.json"),"utf8")), null);
    assert.deepEqual(JSON.parse(fs.readFileSync(path.join(directory,"errors.json"),"utf8")),game.errors);
  } finally { fs.rmSync(directory,{recursive:true,force:true}); }
});

test("Japanese state responses survive chunk boundaries and out-of-order console delivery", () => {
  const page = new EventEmitter();
  const game = new GameHarness(page, "/unused");
  const response = { requestId: "42", protocol: 1, name: "双剣・散弾・麒麟" };
  const base64 = Buffer.from(JSON.stringify(response)).toString("base64");
  const chunks = base64.match(/.{1,9}/g);
  for (let index = chunks.length - 1; index >= 0; index--)
    page.emit("console", { text: () => `KANJI_E2E_PART:42:${index}:${chunks.length}:${chunks[index]}\nUnity stack`, type: () => "log" });
  assert.deepEqual(game.responses.get("42"), response);
  assert.equal(game.chunks.size, 0);
  assert.deepEqual(game.errors, []);
});

test("incomplete responses never count as a successful snapshot", () => {
  const page = new EventEmitter(); const game = new GameHarness(page, "/unused");
  page.emit("console", { text: () => "KANJI_E2E_PART:1:0:2:YWJj", type: () => "log" });
  assert.equal(game.responses.size, 0);
});

test("browser errors and Unity exceptions are captured", () => {
  const page = new EventEmitter(); const game = new GameHarness(page, "/unused");
  page.emit("console", { text: () => "NullReferenceException: example", type: () => "log" });
  page.emit("pageerror", new Error("WebGL failure"));
  assert.equal(game.errors.length, 2);
});

test("normalized Unity bounds map to the actual canvas rather than fixed window coordinates", async () => {
  const page = new EventEmitter(); let clicked;
  page.locator = () => ({ boundingBox: async () => ({ x: 20, y: 30, width: 400, height: 800 }) });
  page.mouse = { click: async (x, y) => { clicked = { x, y }; } };
  const game = new GameHarness(page, "/unused");
  await game.clickBox({ x: 0.2, y: 0.4, w: 0.2, h: 0.1 });
  assert(Math.abs(clicked.x - 140) < 0.001); assert(Math.abs(clicked.y - 390) < 0.001);
});

test("scenario identifiers are unique and every listed scenario is executable", () => {
  assert.equal(new Set(scenarios.map(scenario => scenario.id)).size, scenarios.length);
  for (const scenario of scenarios) {
    assert.equal(typeof scenario.run, "function"); assert(scenario.fixture); assert(scenario.name);
  }
});

test("covered controls fail before a misleading click is sent", async () => {
  const page = new EventEmitter(); let clicked = false;
  page.mouse = { click: async () => { clicked = true; } };
  const game = new GameHarness(page, "/unused");
  game.request = async () => ({ controls: [{ path: "/Covered", visible: true, reachable: false, enabled: true }] });
  await assert.rejects(() => game.named("Covered"), /covered by another UI element/);
  assert.equal(clicked, false);
});

test("offscreen tree branches are reached with real horizontal dragging", async () => {
  const page=new EventEmitter();let dragged=false,pressed=false;
  page.locator=()=>({boundingBox:async()=>({x:0,y:0,width:390,height:844})});
  page.mouse={move:async(x)=>{if(pressed&&x>195)dragged=true;},down:async()=>{pressed=true;},up:async()=>{pressed=false;},click:async()=>{}};
  page.waitForTimeout=async()=>{};
  const game=new GameHarness(page,"/unused");
  game.request=async()=>({controls:[{path:'/TreeNode_s1',enabled:true,visible:dragged,reachable:dragged,rect:{x:dragged?.2:-.3,y:.5,w:.4,h:.1}}]});
  await game.named('TreeNode_s1');assert(dragged);
});

test("radial map panning uses the actual viewport for vertical offscreen nodes", async () => {
  const page=new EventEmitter();let dragged=false,pressed=false;
  page.locator=()=>({boundingBox:async()=>({x:0,y:0,width:390,height:844})});
  page.mouse={move:async(x,y)=>{if(pressed&&y>400)dragged=true;},down:async()=>{pressed=true;},up:async()=>{pressed=false;},click:async()=>{}};
  page.waitForTimeout=async()=>{};
  const game=new GameHarness(page,"/unused");
  game.request=async()=>({treeViewport:{x:.05,y:.3,w:.9,h:.5},controls:[{
    path:'/TreeViewport/TreeContent/TreeNode_u10_0',enabled:true,visible:dragged,reachable:dragged,
    rect:{x:.4,y:dragged?.45:.05,w:.1,h:.1}
  }]});
  await game.named('TreeNode_u10_0');assert(dragged);
});

test("test-only C# entry points are excluded from normal players", () => {
  const root = path.resolve(__dirname, "../..");
  for (const file of ["BattleE2EBridge.cs", "BattleManager.E2E.cs", "BattleManager.DefenseE2E.cs"]) {
    const source = fs.readFileSync(path.join(root, "Assets/Scripts/Game", file), "utf8");
    assert(source.startsWith("#if KANJI_BATTLE_E2E\n")); assert(source.trimEnd().endsWith("#endif"));
  }
});

test("an incomplete build is reported as a test failure instead of an unhandled route exception", async () => {
  const page = new EventEmitter(); let handler, fulfilled;
  page.route = async (_, callback) => { handler = callback; };
  page.goto = async () => handler({
    fetch: async () => ({ text: async () => "build temporarily unavailable" }),
    fulfill: async value => { fulfilled = value; }
  });
  const game = new GameHarness(page, "/unused");
  await assert.rejects(() => game.boot("http://local/game/index.html"), /Unity loader could not be prepared/);
  assert.equal(fulfilled.status, 503);
  assert.match(game.errors[0], /avoid rebuilding/);
});
