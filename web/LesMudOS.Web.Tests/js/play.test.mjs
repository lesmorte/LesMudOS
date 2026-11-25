// Tests of wwwroot/js/play.js with Node's own test runner: node --test web/LesMudOS.Web.Tests/js
// The pure parts run against a tiny strict fake DOM that throws if anything assigns markup. The page tests run only where jsdom can be
// found (npm install jsdom; NODE_PATH=.../node_modules): they load the rendered /play page (HM_PLAY_HTML) and drive it with a fake socket.
import test from 'node:test';
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const here = path.dirname(fileURLToPath(import.meta.url));
const playPath = path.resolve(here, '../../LesMudOS.Web/wwwroot/js/play.js');
const play = require(playPath);

// ---------------------------------------------------------------------------------------------------------------- a strict fake DOM
class FakeText { constructor(s) { this.nodeType = 3; this.data = String(s); this.parentNode = null; } get textContent() { return this.data; } }
class FakeEl {
  constructor(tag) { this.nodeType = 1; this.tagName = tag; this.className = ''; this.children = []; this.attrs = {}; this.parentNode = null; }
  appendChild(n) { n.parentNode = this; this.children.push(n); return n; }
  removeChild(n) { this.children.splice(this.children.indexOf(n), 1); n.parentNode = null; return n; }
  get firstChild() { return this.children[0] || null; }
  setAttribute(k, v) { this.attrs[k] = String(v); }
  getAttribute(k) { return k in this.attrs ? this.attrs[k] : null; }
  get textContent() { return this.children.map(c => c.textContent).join(''); }
}
for (const bad of ['innerHTML', 'outerHTML']) Object.defineProperty(FakeEl.prototype, bad, { get() { throw new Error('read of ' + bad); }, set() { throw new Error('assignment to ' + bad); } });
FakeEl.prototype.insertAdjacentHTML = () => { throw new Error('insertAdjacentHTML'); };
const fakeDoc = { createElement: t => new FakeEl(t), createTextNode: s => new FakeText(s) };

function all(el, out = []) { for (const c of el.children) { out.push(c); if (c.children) all(c, out); } return out; }
function render(text, opts = {}) {
  const el = new FakeEl('div');
  const screen = play.createScreen(fakeDoc, el, { maxLines: opts.maxLines || 3000, hideEcho: true });
  const ansi = play.createAnsi();
  screen.write(ansi.feed(text));
  return { el, screen, ansi };
}
const lineTexts = el => el.children.map(l => l.textContent);

// ---------------------------------------------------------------------------------------------------------------- ANSI
test('the colours the game sends become fixed class names', () => {
  const t = play.createAnsi().feed('Obvious exits: \x1b[1;96mnorth\x1b[0m, \x1b[31mred\x1b[0m \x1b[4munder\x1b[0m \x1b[1mbold\x1b[0m\n');
  const classes = t.filter(x => x.k === 't').map(x => [x.s, x.c]);
  assert.deepEqual(classes, [['Obvious exits: ', ''], ['north', 'b fg96 cmd'], [', ', ''], ['red', 'fg31'], [' ', ''], ['under', 'u'], [' ', ''], ['bold', 'b']]);
  assert.equal(t[t.length - 1].k, 'n');
});

test('bold is not cmd unless it is exactly bold bright cyan', () => {
  const c = s => play.createAnsi().feed(s).filter(x => x.k === 't')[0].c;
  assert.equal(c('\x1b[1;96mx'), 'b fg96 cmd');
  assert.equal(c('\x1b[1;4;96mx'), 'b u fg96');
  assert.equal(c('\x1b[96mx'), 'fg96');
  assert.equal(c('\x1b[1;96;41mx'), 'b fg96 bg41');
  assert.equal(c('\x1b[1;96;22mx'), 'fg96');
});

test('the style resets at every newline', () => {
  const t = play.createAnsi().feed('\x1b[31mred\nstill?');
  assert.deepEqual(t.filter(x => x.k === 't').map(x => x.c), ['fg31', '']);
});

test('extended colours consume their parameters and draw nothing', () => {
  const c = s => play.createAnsi().feed(s).filter(x => x.k === 't').map(x => x.c);
  assert.deepEqual(c('\x1b[38;5;196mx'), ['']);
  assert.deepEqual(c('\x1b[38;2;1;2;3;1mx'), ['b']);
  assert.deepEqual(c('\x1b[48;5;9;31mx'), ['fg31']);
  assert.deepEqual(c('\x1b[1;999;31mx'), ['b fg31']);
});

test('every other escape sequence is discarded and no escape character is ever drawn', () => {
  const hostile = ['\x1b[2J', '\x1b[999999999D', '\x1b[H\x1b[K', '\x1b]0;title\x07', '\x1b]0;title\x1b\\', '\x1bc', '\x1b(B', '\x1b', '\x1b[?25l', '\x1bPdata\x1b\\', '\x1b[31'];
  for (const h of hostile) {
    const out = play.createAnsi().feed('a' + h + 'b').filter(x => x.k === 't').map(x => x.s).join('');
    assert.ok(!out.includes('\x1b'), JSON.stringify(h));
    assert.ok(out.startsWith('a'), JSON.stringify(h));
  }
  assert.equal(play.createAnsi().feed('a\x1b]0;title\x07b').filter(x => x.k === 't').map(x => x.s).join(''), 'ab');
  assert.equal(play.createAnsi().feed('a\x1b[2Jb').filter(x => x.k === 't').map(x => x.s).join(''), 'ab');
});

test('a sequence cut by the end of a frame is continued by the next one', () => {
  const a = play.createAnsi();
  const out = [];
  for (const part of ['x\x1b', '[1;', '96mnor', 'th\x1b[0', 'mdone']) out.push(...a.feed(part).filter(x => x.k === 't'));
  assert.deepEqual(out.map(x => [x.s, x.c]), [['x', ''], ['nor', 'b fg96 cmd'], ['th', 'b fg96 cmd'], ['done', '']]);
});

test('a surrogate pair split across frames is not broken by the parser', () => {
  const a = play.createAnsi();
  const s = '\u{1F600}';
  const joined = a.feed(s[0]).concat(a.feed(s[1])).filter(x => x.k === 't').map(x => x.s).join('');
  assert.equal(joined, s);
});

test('control characters other than newline and tab are dropped', () => {
  assert.equal(play.createAnsi().feed('a\x00\x07\x08\r\x7f\u0085b\tc').filter(x => x.k === 't').map(x => x.s).join(''), 'ab\tc');
});

test('an over-long sequence is abandoned and 10,000 escapes in a row do not hang', () => {
  const long = '\x1b[' + '1;'.repeat(100) + 'm';
  const out = play.createAnsi().feed('a' + long + 'b').filter(x => x.k === 't').map(x => x.s).join('');
  assert.ok(!out.includes('\x1b'));
  const t0 = Date.now();
  const many = play.createAnsi().feed('\x1b[31m'.repeat(10000) + 'x' + '\x1b]0;t\x07'.repeat(10000));
  assert.equal(many.filter(x => x.k === 't').map(x => x.s).join(''), 'x');
  assert.ok(Date.now() - t0 < 2000);
});

// ---------------------------------------------------------------------------------------------------------------- the screen: text only
test('hostile game text is drawn as text, never as markup', () => {
  const lines = [
    '<script>alert(1)</script>', '<img src=x onerror=alert(1)>', '</div><script>', '"><svg onload=alert(1)>', '<b>bold</b> &amp; &lt; &#x3c;',
    'javascript:alert(1)', '<a href="http://evil.example">click</a>', '{{7*7}} ${7*7} `x`',
  ];
  const { el } = render(lines.join('\n') + '\n');
  const nodes = all(el);
  for (const n of nodes) if (n.nodeType === 1) assert.ok(['div', 'span'].includes(n.tagName), 'unexpected element ' + n.tagName);
  assert.deepEqual(lineTexts(el).slice(0, lines.length), lines);
  // the only attributes are the fixed ones the page sets
  for (const n of nodes) if (n.nodeType === 1) assert.deepEqual(Object.keys(n.attrs).filter(k => k !== 'aria-hidden'), []);
});

test('a hostile name in a colour sequence cannot become a class', () => {
  const { el } = render('\x1b[1;96;<script>m x\x1b[31m<img onerror=1>\x1b[0m\n');
  for (const n of all(el)) if (n.nodeType === 1) assert.match(n.className, /^(ln|ln cont|(b|u|fg\d+|bg\d+|cmd|echo|note)( (b|u|fg\d+|bg\d+|cmd))*)?$/);
});

test('a line of 100,000 characters is cut into continuation lines of at most 4096', () => {
  const { el } = render('x'.repeat(100000) + '\n');
  const lines = el.children;
  assert.ok(lines.length >= 25);
  for (const l of lines) assert.ok(l.textContent.length <= 4096);
  assert.equal(lines.map(l => l.textContent).join(''), 'x'.repeat(100000));
  assert.ok(lines[1].className.includes('cont'));
});

test('the unfinished last line is the prompt, and the echo continues it', () => {
  const { el, screen, ansi } = render('You are here.\nAlice (45/58) > ');
  assert.deepEqual(lineTexts(el), ['You are here.', 'Alice (45/58) > ']);
  assert.equal(screen.tail(), 'Alice (45/58) > ');
  screen.echo('look');
  assert.deepEqual(lineTexts(el), ['You are here.', 'Alice (45/58) > look', '']);
  const echoSpan = el.children[1].children[1];
  assert.equal(echoSpan.className, 'echo');
  assert.equal(echoSpan.attrs['aria-hidden'], 'true');
  screen.write(ansi.feed('A square.\nAlice (45/58) > '));
  assert.deepEqual(lineTexts(el), ['You are here.', 'Alice (45/58) > look', 'A square.', 'Alice (45/58) > ']);
});

test('text arriving in many frames continues the same line and merges same-style runs', () => {
  const { el, screen, ansi } = render('');
  for (const part of ['Pass', 'word: ']) screen.write(ansi.feed(part));
  assert.deepEqual(lineTexts(el), ['Password: ']);
  assert.equal(el.children[0].children.length, 1);
  assert.match(screen.tail(), /assword: $/);
});

test('a note is on a line of its own and the prompt line is not changed by it', () => {
  const { el, screen, ansi } = render('Alice > ');
  screen.note('*** Connection lost ***');
  assert.deepEqual(lineTexts(el), ['Alice > ', '*** Connection lost ***', '']);
  assert.equal(el.children[1].children[0].className, 'note');
});

test('scrollback is trimmed in batches and the unfinished line survives', () => {
  const { el, screen } = render('', { maxLines: 500 });
  const ansi = play.createAnsi();
  screen.write(ansi.feed(Array.from({ length: 800 }, (_, i) => 'line ' + i).join('\n') + '\nprompt > '));
  const removed = screen.trim();
  assert.ok(removed >= 100);
  assert.equal(el.children.length, 801 - removed);
  assert.ok(el.children.length <= 500 + 100 && el.children.length >= 500);
  assert.equal(el.children[el.children.length - 1].textContent, 'prompt > ');
  assert.equal(el.children[el.children.length - 2].textContent, 'line 799');
});

test('a thousand lines in one frame is one batch of tokens and counts its completed lines', () => {
  const { screen, ansi } = render('');
  screen.takeCompleted();
  screen.write(ansi.feed('a\n'.repeat(1000)));
  assert.equal(screen.takeCompleted(), 1000);
  assert.equal(screen.takeCompleted(), 0);
});

// ---------------------------------------------------------------------------------------------------------------- history and the rest
test('history walks up and down, keeps the line being typed and collapses repeats', () => {
  const h = play.createHistory(100);
  h.add('look'); h.add('look'); h.add('north'); h.add(''); h.add('say hi');
  assert.equal(h.size(), 3);
  assert.equal(h.prev('half typed'), 'say hi');
  assert.equal(h.prev('x'), 'north');
  assert.equal(h.prev('x'), 'look');
  assert.equal(h.prev('x'), 'look');
  assert.equal(h.next(), 'north');
  assert.equal(h.next(), 'say hi');
  assert.equal(h.next(), 'half typed');
  assert.equal(h.next(), null);
});

test('history keeps at most 100 and an empty one answers null', () => {
  const h = play.createHistory(100);
  assert.equal(h.prev(''), null);
  for (let i = 0; i < 250; i++) h.add('cmd ' + i);
  assert.equal(h.size(), 100);
  assert.equal(h.all()[0], 'cmd 150');
});

test('reconnect delays are 1, 2, 4, 8, 15, 30 and then 30, with up to 20 percent jitter', () => {
  const d = n => play.backoffDelay(n, () => 0);
  assert.deepEqual([0, 1, 2, 3, 4, 5, 6, 20].map(d), [1, 2, 4, 8, 15, 30, 30, 30]);
  assert.equal(play.backoffDelay(5, () => 1), 36);
});

test('settings: corrupt values give the defaults and good ones are clamped', () => {
  const mk = v => ({ getItem: () => v });
  const d = { scrollback: 3000 };
  assert.deepEqual(play.loadSettings(mk(null), d), { stripDismissed: false, scrollback: 3000 });
  for (const bad of ['{', 'null', '[]', '"x"', '5', 'x'.repeat(20000), '{"stripDismissed":"yes","scrollback":"lots"}'])
    assert.deepEqual(play.loadSettings(mk(bad), d), { stripDismissed: false, scrollback: 3000 }, bad.slice(0, 20));
  assert.deepEqual(play.loadSettings(mk('{"stripDismissed":true,"scrollback":99999}'), d), { stripDismissed: true, scrollback: 10000 });
  assert.equal(play.loadSettings(mk('{"scrollback":5}'), d).scrollback, 500);
  assert.deepEqual(play.loadSettings({ getItem() { throw new Error('blocked'); } }, d), { stripDismissed: false, scrollback: 3000 });
});

test('font size: the largest from 11 to 16 px at which 52 columns fit', () => {
  const cw = 0.6;   // a typical monospace column is 0.6 em
  assert.equal(play.fitFontSize(340, cw), 11);                  // 52 * 0.6 * 11 = 343: just under, floor at 11
  assert.equal(play.fitFontSize(360, cw), 11.5);
  assert.equal(play.fitFontSize(1000, cw), 16);
  assert.equal(play.fitFontSize(0, cw), 12);
  const size = play.fitFontSize(390, cw);
  assert.ok(size * cw * 52 <= 390 + 0.01 || size === 11);
});

// ---------------------------------------------------------------------------------------------------------------- the whole page (needs jsdom)
let JSDOM = null;
try { ({ JSDOM } = require('jsdom')); } catch { /* not installed: these tests are skipped */ }
const pageHtml = process.env.HM_PLAY_HTML && fs.existsSync(process.env.HM_PLAY_HTML) ? fs.readFileSync(process.env.HM_PLAY_HTML, 'utf8') : null;
const opened = [];
const withPage = (name, fn) => (JSDOM && pageHtml ? test : test.skip)(name, async () => {
  try { await fn(); } finally { while (opened.length) opened.pop().close(); }   // the page's timers would keep node alive
});

class FakeSocket {
  static all = [];
  constructor(url, protocol) { this.url = url; this.protocol = protocol; this.readyState = 0; this.sent = []; FakeSocket.all.push(this); }
  send(s) { this.sent.push(JSON.parse(s)); }
  close(code) { this.readyState = 3; this.onclose && this.onclose({ code: code || 1005 }); }
  open() { this.readyState = 1; this.onopen && this.onopen(); }
  recv(frame) { this.onmessage && this.onmessage({ data: typeof frame === 'string' ? frame : JSON.stringify(frame) }); }
  drop(code) { this.readyState = 3; this.onclose && this.onclose({ code }); }
}
const sleep = ms => new Promise(r => setTimeout(r, ms));

async function openPage(status = { v: 1, enabled: true, full: false }) {
  FakeSocket.all = [];
  const store = new Map();
  const dom = new JSDOM(pageHtml.replace(/<script\b[^>]*src=[^>]*><\/script>/g, ''), { url: 'https://example.test/play', runScripts: 'outside-only', pretendToBeVisual: true });
  const w = dom.window;
  opened.push(w);
  w.WebSocket = FakeSocket;
  w.fetch = async () => ({ ok: true, json: async () => status });
  Object.defineProperty(w, 'localStorage', { value: { getItem: k => store.get(k) ?? null, setItem: (k, v) => store.set(k, String(v)), removeItem: k => store.delete(k) } });
  w.eval(fs.readFileSync(playPath, 'utf8'));
  await sleep(50);
  const $ = id => w.document.getElementById(id);
  return { w, $, store, sock: () => FakeSocket.all[FakeSocket.all.length - 1] };
}
async function enter(p) {
  p.$('play-enter').click();
  const s = p.sock();
  s.open();
  s.recv({ t: 'hello', v: 1, max_in: 4096, ping: 25 });
  return s;
}

withPage('the page offers Enter, connects to the same origin with the subprotocol and shows game text as text', async () => {
  const p = await openPage();
  assert.equal(p.$('play-enter').hidden, false);
  assert.equal(p.$('play-log').hidden, true);
  const s = await enter(p);
  assert.equal(s.url, 'wss://example.test/play/ws');
  assert.equal(s.protocol, 'hollowmere.v1');
  const hostile = '<script>window.pwned=1</script><img src=x onerror="window.pwned=2">\x1b[31mred\x1b[0m';
  s.recv({ t: 'out', d: 'By what name are you known? ' });
  s.recv({ t: 'out', d: hostile + '\n' });
  await sleep(250);
  const log = p.$('play-log');
  assert.equal(log.querySelectorAll('script, img, svg, a, iframe').length, 0);
  assert.ok(log.textContent.includes('<script>window.pwned=1</script><img src=x onerror="window.pwned=2">red'));
  assert.equal(p.w.pwned, undefined);
  assert.ok(!log.textContent.includes('\x1b'));
  assert.equal(log.querySelector('.fg31').textContent, 'red');
  assert.equal(log.getAttribute('role'), 'log');
});

withPage('a typed command is sent, echoed on the prompt line and remembered; Up brings it back', async () => {
  const p = await openPage();
  const s = await enter(p);
  s.recv({ t: 'out', d: 'Alice (45/58) > ' });
  await sleep(200);
  const input = p.$('play-input');
  input.value = 'look';
  p.$('play-form').dispatchEvent(new p.w.Event('submit', { cancelable: true }));
  assert.deepEqual(s.sent.filter(f => f.t === 'in'), [{ t: 'in', d: 'look' }]);
  assert.equal(input.value, '');
  assert.ok(p.$('play-log').textContent.includes('Alice (45/58) > look'));
  input.dispatchEvent(new p.w.KeyboardEvent('keydown', { key: 'ArrowUp', cancelable: true }));
  assert.equal(input.value, 'look');
  input.dispatchEvent(new p.w.KeyboardEvent('keydown', { key: 'Escape' }));
  assert.equal(input.value, '');
});

withPage('a masked line: password field, no echo, no history, nothing stored, back to text afterwards', async () => {
  const p = await openPage();
  const s = await enter(p);
  s.recv({ t: 'out', d: 'Password: ' });
  s.recv({ t: 'mask', on: true });
  await sleep(200);
  const input = p.$('play-input');
  assert.equal(input.type, 'password');
  assert.equal(input.getAttribute('autocomplete'), 'off');
  input.value = 'hunter2-secret-marker';
  p.$('play-form').dispatchEvent(new p.w.Event('submit', { cancelable: true }));
  assert.deepEqual(s.sent.filter(f => f.t === 'in').map(f => f.d), ['hunter2-secret-marker']);
  assert.equal(input.value, '');
  s.recv({ t: 'mask', on: false });
  s.recv({ t: 'out', d: '\nWelcome.\nAlice > ' });
  await sleep(200);
  assert.equal(input.type, 'text');
  const everything = p.w.document.documentElement.outerHTML + JSON.stringify([...p.store.entries()]);
  assert.ok(!everything.includes('hunter2-secret-marker'));
  input.dispatchEvent(new p.w.KeyboardEvent('keydown', { key: 'ArrowUp', cancelable: true }));
  assert.equal(input.value, '');                          // the password is not in the history
  const lines = [...p.$('play-log').querySelectorAll('.ln')].map(l => l.textContent);
  assert.deepEqual(lines.slice(0, 3), ['Password: ', 'Welcome.', 'Alice > ']);   // no echo; the game's own newline ends the line
});

withPage('the safety net masks a password prompt even if the mask frame never comes', async () => {
  const p = await openPage();
  const s = await enter(p);
  s.recv({ t: 'out', d: 'Password: ' });
  await sleep(250);
  assert.equal(p.$('play-input').type, 'password');
});

withPage('a lost connection shows the banner and a note and retries; a clean close does not', async () => {
  const p = await openPage();
  const s = await enter(p);
  await sleep(50);
  s.drop(1006);
  await sleep(50);
  assert.equal(p.$('play-banner').hidden, false);
  assert.match(p.$('play-banner-text').textContent, /Connection lost\. Trying again in \d+ s\./);
  assert.ok(p.$('play-log').textContent.includes('*** Connection lost ***'));
  p.$('play-try').click();
  const s2 = p.sock();
  assert.notEqual(s2, s);
  s2.open();
  assert.ok(p.$('play-log').textContent.includes('*** Connected. Please sign in again. ***'));
  assert.equal(p.$('play-banner').hidden, true);
  s2.recv({ t: 'sys', code: 'game_closed', msg: 'The game closed your connection.' });
  s2.drop(1000);
  await sleep(50);
  assert.equal(p.$('play-closed').hidden, false);
  assert.equal(p.$('play-banner').hidden, true);
  const before = FakeSocket.all.length;
  await sleep(1300);
  assert.equal(FakeSocket.all.length, before);
});

withPage('a full game and a switched-off client are told on the page before any socket is opened', async () => {
  let p = await openPage({ v: 1, enabled: true, full: true });
  assert.equal(p.$('play-enter').hidden, true);
  assert.match(p.$('play-notice').textContent, /full right now/);
  assert.equal(FakeSocket.all.length, 0);
  p = await openPage({ v: 1, enabled: false, full: false });
  assert.equal(p.$('play-enter').hidden, true);
  assert.match(p.$('play-notice').textContent, /switched off/);
});

withPage('the pause button toggles the live region and the strip can be hidden for good', async () => {
  const p = await openPage();
  await enter(p);
  const log = p.$('play-log'), pause = p.$('play-pause');
  assert.equal(log.getAttribute('aria-live'), 'polite');
  pause.click();
  assert.equal(log.getAttribute('aria-live'), 'off');
  assert.equal(pause.getAttribute('aria-pressed'), 'true');
  pause.click();
  assert.equal(log.getAttribute('aria-live'), 'polite');
  p.$('play-strip-hide').click();
  assert.equal(p.$('play-strip').hidden, true);
  assert.equal(JSON.parse(p.store.get('hm.settings.v1')).stripDismissed, true);
});
