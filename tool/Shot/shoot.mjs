// Takes the picture of each release that the Gallery's pages show: the screen its "Open it" link
// opens, in a Rulpane served from this machine with this Gallery's releases beside it.
//
//   node tool/Shot/shoot.mjs <list> <viewer> <folder>
//
// <list> is what `tool/Site --shots-to-take` writes: a line per picture, its path under <folder>
// and the fragment that opens it, tab-separated. <viewer> is where Rulpane is served, such as
// http://127.0.0.1:5080/. Each picture is the first 1280 × 800 of the page, once the screen is
// up, at half that size. A link whose screen does not come up within 30 seconds gets no picture,
// and the page shows none; that is said here and does not stop the run.
//
// It drives Chrome (or $CHROME) over the DevTools protocol, with Node's own WebSocket, so it
// needs Node 22 or later and nothing installed from npm.

import { spawn } from "node:child_process";
import { mkdtemp, mkdir, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";

const [list, viewer, folder] = process.argv.slice(2);
if (!list || !viewer || !folder) {
  console.error("usage: node tool/Shot/shoot.mjs <list> <viewer> <folder>");
  process.exit(2);
}

const width = 1280, height = 800, wait = 30_000;
const wanted = (await readFile(list, "utf8")).split("\n").filter(l => l.trim()).map(l => l.split("\t"));

const profile = await mkdtemp(join(tmpdir(), "rulpane-shot-"));
const chrome = spawn(process.env.CHROME ?? "google-chrome", [
  "--headless=new", "--remote-debugging-port=0", `--user-data-dir=${profile}`,
  "--no-first-run", "--no-default-browser-check", "--hide-scrollbars", "--force-color-profile=srgb",
  // The pages are in English, and Rulpane speaks the language the browser asks for first.
  "--lang=en-US", "--accept-lang=en-US",
  "about:blank",
], { stdio: ["ignore", "ignore", "pipe"] });

// Chrome says where its DevTools listen on stderr, once it is up.
const browser = await new Promise((resolve, reject) => {
  let said = "";
  chrome.stderr.on("data", chunk => {
    said += chunk;
    const found = said.match(/DevTools listening on (ws:\/\/\S+)/);
    if (found) resolve(found[1]);
  });
  chrome.on("exit", code => reject(new Error(`Chrome exited (${code}) before it listened:\n${said}`)));
});

const socket = new WebSocket(browser);
await new Promise((resolve, reject) => { socket.onopen = resolve; socket.onerror = reject; });

let next = 0;
const pending = new Map();
socket.onmessage = event => {
  const message = JSON.parse(event.data);
  if (message.id && pending.has(message.id)) {
    const { resolve, reject } = pending.get(message.id);
    pending.delete(message.id);
    message.error ? reject(new Error(message.error.message)) : resolve(message.result);
  }
};
const send = (method, params = {}, sessionId) => new Promise((resolve, reject) => {
  const id = ++next;
  pending.set(id, { resolve, reject });
  socket.send(JSON.stringify({ id, method, params, sessionId }));
});

const { targetId } = await send("Target.createTarget", { url: "about:blank" });
const { sessionId } = await send("Target.attachToTarget", { targetId, flatten: true });
const page = (method, params) => send(method, params, sessionId);
await page("Emulation.setDeviceMetricsOverride", { width, height, deviceScaleFactor: 1, mobile: false });
await page("Emulation.setEmulatedMedia", { features: [{ name: "prefers-color-scheme", value: "light" }] });

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const state = async () => (await page("Runtime.evaluate", {
  expression: "document.querySelector('main .play') ? 'up' : document.querySelector('main .failure')?.innerText ?? ''",
  returnByValue: true,
})).result.value;

let taken = 0;
for (const [path, fragment] of wanted) {
  // A page of its own each time: a change of fragment alone is not a new load.
  await page("Page.navigate", { url: "about:blank" });
  await page("Page.navigate", { url: new URL(fragment, viewer).href });
  let seen = "";
  for (const until = Date.now() + wait; Date.now() < until && !seen; ) {
    await sleep(250);
    seen = await state().catch(() => "");
  }

  if (seen !== "up") {
    console.log(`✕ ${path}: ${seen ? seen.replace(/\s+/g, " ").trim() : "the screen did not come up"}`);
    continue;
  }

  // Let the fonts and the first layout settle before the picture.
  await page("Runtime.evaluate", { expression: "document.fonts.ready", awaitPromise: true });
  await sleep(300);
  const { data } = await page("Page.captureScreenshot", {
    format: "webp", quality: 80, clip: { x: 0, y: 0, width, height, scale: 0.5 },
  });
  await mkdir(dirname(join(folder, path)), { recursive: true });
  await writeFile(join(folder, path), Buffer.from(data, "base64"));
  console.log(`✓ ${path}`);
  taken++;
}

console.log(`${taken} of ${wanted.length} picture(s) → ${folder}`);
socket.close();
chrome.kill();
await new Promise(resolve => chrome.once("exit", resolve));
await rm(profile, { recursive: true, force: true }).catch(() => {});
