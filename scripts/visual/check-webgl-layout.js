const { chromium } = require("playwright");
const http = require("http");
const path = require("path");
const fs = require("fs");

const projectRoot = path.resolve(__dirname, "../..");
const docsDir = path.join(projectRoot, "Docs");
const outDir = path.join(projectRoot, "tmp", "playwright-screenshots");
const port = Number(process.env.KANJI_BATTLE_QA_PORT || 8787);
const baseUrl = `http://127.0.0.1:${port}`;

const viewports = [
  {
    name: "mobile-game",
    url: "/game/index.html",
    viewport: { width: 390, height: 844 },
    userAgent:
      "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
    flow: "top"
  },
  {
    name: "desktop-game",
    url: "/game/index.html",
    viewport: { width: 1280, height: 800 },
    userAgent:
      "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36",
    flow: "battle"
  },
  {
    name: "mobile-embedded",
    url: "/index.html",
    viewport: { width: 390, height: 844 },
    userAgent:
      "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
    flow: "top"
  },
  {
    name: "desktop-embedded",
    url: "/index.html",
    viewport: { width: 1280, height: 900 },
    userAgent:
      "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36",
    flow: "top"
  }
];

function serveStatic(root) {
  const server = http.createServer((req, res) => {
    const requestUrl = new URL(req.url, baseUrl);
    const decodedPath = decodeURIComponent(requestUrl.pathname);
    const safePath = path.normalize(decodedPath).replace(/^(\.\.[/\\])+/, "");
    let filePath = path.join(root, safePath);

    if (!filePath.startsWith(root)) {
      res.writeHead(403);
      res.end("Forbidden");
      return;
    }

    if (fs.existsSync(filePath) && fs.statSync(filePath).isDirectory()) {
      filePath = path.join(filePath, "index.html");
    }

    fs.readFile(filePath, (err, data) => {
      if (err) {
        res.writeHead(404);
        res.end("Not Found");
        return;
      }

      const ext = path.extname(filePath).toLowerCase();
      const types = {
        ".html": "text/html; charset=utf-8",
        ".js": "application/javascript; charset=utf-8",
        ".wasm": "application/wasm",
        ".data": "application/octet-stream",
        ".css": "text/css; charset=utf-8",
        ".png": "image/png",
        ".ico": "image/x-icon"
      };
      res.writeHead(200, { "Content-Type": types[ext] || "application/octet-stream" });
      res.end(data);
    });
  });

  return new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(port, "127.0.0.1", () => resolve(server));
  });
}

async function waitForUnityCanvas(page) {
  const canvas = page.locator("#unity-canvas");
  await canvas.waitFor({ state: "visible", timeout: 90000 });
  await page.waitForFunction(() => {
    const loading = document.querySelector("#unity-loading-bar");
    const canvasEl = document.querySelector("#unity-canvas");
    const rect = canvasEl && canvasEl.getBoundingClientRect();
    return loading && loading.style.display === "none" && rect && rect.width > 100 && rect.height > 100;
  }, { timeout: 120000 });
  return canvas;
}

async function clickCanvasRatio(page, xRatio, yRatio) {
  const rect = await page.locator("#unity-canvas").boundingBox();
  if (!rect) throw new Error("Unity canvas is not visible");
  const x = rect.x + rect.width * xRatio;
  const y = rect.y + rect.height * yRatio;
  const useTouch = await page.evaluate(() => "ontouchstart" in window || navigator.maxTouchPoints > 0);
  if (useTouch) {
    await page.touchscreen.tap(x, y);
  } else {
    await page.mouse.click(x, y);
  }
}

async function runBattleSmokeFlow(page) {
  await waitForUnityCanvas(page);

  // Bottom battle tab, first stage, first three owned characters, then start battle.
  await clickCanvasRatio(page, 0.50, 0.985);
  await page.waitForTimeout(800);
  await clickCanvasRatio(page, 0.50, 0.20);
  await page.waitForTimeout(800);
  await clickCanvasRatio(page, 0.16, 0.39);
  await page.waitForTimeout(250);
  await clickCanvasRatio(page, 0.16, 0.47);
  await page.waitForTimeout(250);
  await clickCanvasRatio(page, 0.16, 0.55);
  await page.waitForTimeout(250);
  await clickCanvasRatio(page, 0.50, 0.92);
  await page.waitForTimeout(2500);
}

async function inspectCanvas(page) {
  return page.evaluate(() => {
    const canvas = document.querySelector("#unity-canvas");
    const rect = canvas.getBoundingClientRect();
    const loading = document.querySelector("#unity-loading-bar");
    return {
      canvas: {
        x: Math.round(rect.x),
        y: Math.round(rect.y),
        width: Math.round(rect.width),
        height: Math.round(rect.height)
      },
      loadingVisible: loading ? loading.style.display !== "none" : null,
      body: {
        width: document.documentElement.clientWidth,
        height: document.documentElement.clientHeight,
        scrollWidth: document.documentElement.scrollWidth,
        scrollHeight: document.documentElement.scrollHeight
      }
    };
  });
}

async function main() {
  if (!fs.existsSync(path.join(docsDir, "game", "Build", "game.loader.js"))) {
    throw new Error("Docs/game build is missing. Run the Unity WebGL build first.");
  }
  fs.mkdirSync(outDir, { recursive: true });

  const server = await serveStatic(docsDir);
  const browser = await chromium.launch({ headless: true });
  const report = [];

  try {
    for (const target of viewports) {
      const context = await browser.newContext({
        viewport: target.viewport,
        userAgent: target.userAgent,
        deviceScaleFactor: target.name.startsWith("mobile") ? 2 : 1,
        isMobile: target.name.startsWith("mobile"),
        hasTouch: target.name.startsWith("mobile")
      });
      const page = await context.newPage();
      page.on("console", msg => {
        const text = msg.text();
        if (/error|exception|abort/i.test(text)) {
          console.log(`[${target.name}] console ${msg.type()}: ${text}`);
        }
      });
      page.on("pageerror", err => console.log(`[${target.name}] pageerror: ${err.message}`));

      await page.goto(`${baseUrl}${target.url}`, { waitUntil: "domcontentloaded", timeout: 30000 });

      if (target.url.includes("/game/")) {
        await waitForUnityCanvas(page);
        if (target.flow === "battle") {
          await runBattleSmokeFlow(page);
        } else {
          await page.waitForTimeout(target.name.startsWith("mobile") ? 12000 : 3000);
        }
      } else {
        await page.waitForSelector("iframe", { timeout: 30000 });
        await page.waitForTimeout(5000);
      }

      const screenshotPath = path.join(outDir, `${target.name}.png`);
      await page.screenshot({ path: screenshotPath, fullPage: true });
      const metrics = target.url.includes("/game/") ? await inspectCanvas(page) : null;
      report.push({ name: target.name, url: target.url, screenshotPath, metrics });
      await context.close();
    }
  } finally {
    await browser.close();
    await new Promise(resolve => server.close(resolve));
  }

  const reportPath = path.join(outDir, "report.json");
  fs.writeFileSync(reportPath, JSON.stringify(report, null, 2));
  console.log(`Visual QA complete: ${reportPath}`);
  for (const item of report) {
    console.log(`- ${item.name}: ${item.screenshotPath}`);
  }
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
