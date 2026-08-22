const fs = require("fs");
const path = require("path");

const projectRoot = path.resolve(__dirname, "../..");
const includeExtensions = new Set([".cs", ".json", ".asset", ".md", ".html"]);
const excludeParts = [
  `${path.sep}.git${path.sep}`,
  `${path.sep}Library${path.sep}`,
  `${path.sep}Logs${path.sep}`,
  `${path.sep}Temp${path.sep}`,
  `${path.sep}UserSettings${path.sep}`,
  `${path.sep}node_modules${path.sep}`,
  `${path.sep}Docs${path.sep}game${path.sep}Build${path.sep}`
];
const maxTextFileBytes = 2 * 1024 * 1024;

function walk(dir, result = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (excludeParts.some(part => full.includes(part))) continue;
    if (entry.isDirectory()) {
      walk(full, result);
  } else if (includeExtensions.has(path.extname(entry.name))) {
      const stat = fs.statSync(full);
      if (stat.size <= maxTextFileBytes && !entry.name.includes(" SDF.asset")) {
        result.push(full);
      }
    }
  }
  return result;
}

function isUsefulGlyph(char) {
  if (/\s/.test(char)) return false;
  const code = char.codePointAt(0);
  return (
    (code >= 0x20 && code <= 0x7e) ||
    (code >= 0x3040 && code <= 0x30ff) ||
    (code >= 0x3400 && code <= 0x9fff) ||
    (code >= 0xff00 && code <= 0xffef) ||
    "、。・ー「」『』（）！？…：／×→←↑↓".includes(char)
  );
}

const glyphs = new Set();
const files = walk(projectRoot);

for (const file of files) {
  let text;
  try {
    text = fs.readFileSync(file, "utf8");
  } catch {
    continue;
  }
  for (const char of text) {
    if (isUsefulGlyph(char)) glyphs.add(char);
  }
}

const sorted = Array.from(glyphs).sort((a, b) => a.codePointAt(0) - b.codePointAt(0));
const outDir = path.join(projectRoot, "tmp", "font");
fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, "glyphs.txt"), sorted.join(""));
fs.writeFileSync(path.join(outDir, "glyphs-report.json"), JSON.stringify({
  count: sorted.length,
  filesScanned: files.length,
  output: "tmp/font/glyphs.txt"
}, null, 2));

console.log(`Collected ${sorted.length} glyphs from ${files.length} files.`);
console.log("Wrote tmp/font/glyphs.txt");
