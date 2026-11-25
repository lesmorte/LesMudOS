/* Legends of Hollowmere: the browser client, stage 1 (a plain text client).
   Vanilla JavaScript, no framework, no build step. Everything the game sends is untrusted text: it only ever reaches the page through
   text nodes (document.createTextNode) on elements made with document.createElement, and class names come from a fixed table of
   numbers. Nothing here builds markup from text. docs/specs/web-client.md sections 2.5, 2.8, 2.10, 3.5, 5.2, 8.1. */
(function () {
  'use strict';

  var MAX_SEQ = 64;            // an escape sequence longer than this is abandoned
  var MAX_LINE_CHARS = 4096;   // longer lines are cut into continuation lines
  var STORAGE_KEY = 'hm.settings.v1';
  var DEFAULT_SCROLLBACK_DESKTOP = 3000;
  var DEFAULT_SCROLLBACK_PHONE = 1500;
  var TRIM_BATCH = 100;

  // ---------------------------------------------------------------------------------------------------------------------
  // ANSI colour (SGR) parser: text in, tokens out. Anything but SGR is consumed and thrown away.
  // ---------------------------------------------------------------------------------------------------------------------

  function createAnsi() {
    var b = false, u = false, fg = 0, bg = 0;
    var mode = 0;     // 0 text, 1 after ESC, 2 CSI parameters, 3 string (OSC, DCS...), 4 ESC inside a string, 5 intermediates
    var seq = '';     // what has been collected of the current sequence

    function style() {
      var parts = [];
      if (b) parts.push('b');
      if (u) parts.push('u');
      if (fg) parts.push('fg' + fg);
      if (bg) parts.push('bg' + bg);
      if (b && fg === 96 && !bg && !u) parts.push('cmd'); // the game's {cmd} words
      return parts.join(' ');
    }

    function sgr(text) {
      var parts = text.split(/[;:]/);
      for (var i = 0; i < parts.length; i++) {
        var p = parts[i];
        var n = p === '' ? 0 : (/^[0-9]{1,4}$/.test(p) ? parseInt(p, 10) : -1);
        if (n < 0) continue;
        if (n === 38 || n === 48) {            // extended colour: consume its parameters, draw nothing
          var kind = parts[i + 1];
          if (kind === '5') i += 2; else if (kind === '2') i += 4; else i += 1;
          continue;
        }
        if (n === 0) { b = false; u = false; fg = 0; bg = 0; }
        else if (n === 1) b = true;
        else if (n === 4) u = true;
        else if (n === 22) b = false;
        else if (n === 24) u = false;
        else if ((n >= 30 && n <= 37) || (n >= 90 && n <= 97)) fg = n;
        else if (n === 39) fg = 0;
        else if ((n >= 40 && n <= 47) || (n >= 100 && n <= 107)) bg = n;
        else if (n === 49) bg = 0;
      }
    }

    /** Text in, tokens out: {k:'t', s:text, c:classes} runs of one style and {k:'n'} for a line end (the style resets there). */
    function feed(text) {
      var tokens = [];
      var run = '';
      var runStyle = style();
      function flush() {
        if (run) { tokens.push({ k: 't', s: run, c: runStyle }); run = ''; }
      }
      for (var i = 0; i < text.length; i++) {
        var ch = text.charAt(i);
        var code = text.charCodeAt(i);
        if (mode === 0) {
          if (code === 27) { flush(); mode = 1; seq = ''; }
          else if (ch === '\n') { flush(); tokens.push({ k: 'n' }); b = false; u = false; fg = 0; bg = 0; runStyle = style(); }
          else if (ch === '\t' || (code >= 32 && code !== 127 && !(code >= 128 && code <= 159))) {
            if (run === '') runStyle = style();
            run += ch;
          }
          // every other control character is dropped
          continue;
        }
        if (code === 27) {                       // ESC inside a sequence
          if (mode === 3 || mode === 4) { mode = 4; continue; }
          mode = 1; seq = ''; continue;           // restarts: the unfinished sequence is dropped
        }
        if (mode === 1) {
          if (ch === '[') { mode = 2; seq = ''; }
          else if (ch === ']' || ch === 'P' || ch === 'X' || ch === '^' || ch === '_') { mode = 3; seq = ''; }
          else if (code >= 0x20 && code <= 0x2f) { mode = 5; seq = ch; }
          else mode = 0;                          // a lone ESC x: both consumed
        } else if (mode === 2) {
          if (code >= 0x40 && code <= 0x7e) { if (ch === 'm') sgr(seq); mode = 0; seq = ''; }
          else if (code >= 0x20 && code <= 0x3f) { seq += ch; if (seq.length > MAX_SEQ) { mode = 0; seq = ''; } }
          else { mode = 0; seq = ''; i--; }       // not part of a sequence: abandon it and read this character again
        } else if (mode === 3) {
          if (code === 7) { mode = 0; seq = ''; }
          else { seq += ch; if (seq.length > MAX_SEQ) { mode = 0; seq = ''; } }
        } else if (mode === 4) {
          if (ch === '\\') { mode = 0; seq = ''; } else { mode = 0; seq = ''; i--; }
        } else if (mode === 5) {
          if (code >= 0x20 && code <= 0x2f) { seq += ch; if (seq.length > MAX_SEQ) { mode = 0; seq = ''; } }
          else mode = 0;                          // the final character of an ESC ( B style sequence
        }
      }
      flush();
      return tokens;
    }

    return { feed: feed, pending: function () { return mode !== 0; } };
  }

  // ---------------------------------------------------------------------------------------------------------------------
  // The output pane's line model. Text goes in through text nodes only.
  // ---------------------------------------------------------------------------------------------------------------------

  /** doc: a document; el: the pane element; opts: {maxLines, hideEcho}. The last line is the unfinished one (the prompt). */
  function createScreen(doc, el, opts) {
    var maxLines = opts.maxLines;
    var lineCount = 0, cur = null, curChars = 0, lastSpan = null, lastClass = null;
    var tail = '';
    var completed = 0;     // lines finished since the last call of takeCompleted()

    function newLine(continuation) {
      cur = doc.createElement('div');
      cur.className = continuation ? 'ln cont' : 'ln';
      el.appendChild(cur);
      lineCount++;
      curChars = 0; lastSpan = null; lastClass = null;
      if (!continuation) tail = '';
    }

    function put(s, cls, extraAttr) {
      if (s === '') return;
      if (!cur) newLine(false);
      while (s.length > 0) {
        var room = MAX_LINE_CHARS - curChars;
        if (room <= 0) { newLine(true); room = MAX_LINE_CHARS; }
        var piece = s.length > room ? s.slice(0, room) : s;
        if (piece.length < s.length && piece.charCodeAt(piece.length - 1) >= 0xd800 && piece.charCodeAt(piece.length - 1) <= 0xdbff) piece = piece.slice(0, -1);
        if (lastSpan && lastClass === cls && !extraAttr) {
          lastSpan.firstChild.data += piece;       // a Text node: this is text, never markup
        } else {
          var span = doc.createElement('span');
          if (cls) span.className = cls;
          if (extraAttr) span.setAttribute(extraAttr, 'true');
          span.appendChild(doc.createTextNode(piece));
          cur.appendChild(span);
          lastSpan = extraAttr ? null : span; lastClass = cls;
        }
        curChars += piece.length;
        tail = (tail + piece).slice(-160);
        s = s.slice(piece.length);
      }
    }

    function endLine() { completed++; newLine(false); }
    newLine(false);

    function trim() {
      if (lineCount <= maxLines + TRIM_BATCH) return 0;
      var remove = lineCount - maxLines;
      for (var i = 0; i < remove && el.firstChild !== cur; i++) { el.removeChild(el.firstChild); lineCount--; }
      return remove;
    }

    return {
      /** Draw tokens from the ANSI parser. */
      write: function (tokens) {
        for (var i = 0; i < tokens.length; i++) {
          var t = tokens[i];
          if (t.k === 'n') endLine(); else put(t.s, t.c);
        }
      },
      /** A line of the client's own ("*** Connection lost ***"), on a line of its own, never part of the prompt. */
      note: function (text) {
        if (curChars > 0) endLine();
        put(text, 'note');
        endLine();
      },
      /** What the player typed, appended to the prompt line, then the line ends. */
      echo: function (text) {
        put(text, 'echo', opts.hideEcho ? 'aria-hidden' : null);
        endLine();
      },
      newline: endLine,
      tail: function () { return tail; },
      lineCount: function () { return lineCount; },
      takeCompleted: function () { var c = completed; completed = 0; return c; },
      trim: trim,
      setMaxLines: function (n) { maxLines = n; }
    };
  }

  // ---------------------------------------------------------------------------------------------------------------------
  // Command history (in memory only), reconnect timing, settings, font size
  // ---------------------------------------------------------------------------------------------------------------------

  function createHistory(limit) {
    var items = [], pos = -1, draft = '';
    return {
      /** Add a typed command. Never call it for a masked line. Empty lines and repeats of the last line are not kept. */
      add: function (text) {
        pos = -1; draft = '';
        if (text === '' || items[items.length - 1] === text) return;
        items.push(text);
        if (items.length > limit) items.shift();
      },
      /** Older entry (Up). current: what the field holds now, kept while walking. */
      prev: function (current) {
        if (items.length === 0) return null;
        if (pos === -1) { draft = current; pos = items.length - 1; }
        else if (pos > 0) pos--;
        return items[pos];
      },
      next: function () {
        if (pos === -1) return null;
        if (pos < items.length - 1) { pos++; return items[pos]; }
        pos = -1;
        return draft;
      },
      reset: function () { pos = -1; draft = ''; },
      size: function () { return items.length; },
      all: function () { return items.slice(); }
    };
  }

  var BACKOFF = [1, 2, 4, 8, 15, 30];
  /** Seconds to wait before attempt number n (0 based): 1, 2, 4, 8, 15, then 30, plus up to 20 percent jitter. rand: a function giving 0 to 1. */
  function backoffDelay(n, rand) {
    var base = BACKOFF[Math.min(n, BACKOFF.length - 1)];
    return base * (1 + 0.2 * (rand ? rand() : Math.random()));
  }

  function clamp(n, lo, hi) { return Math.min(hi, Math.max(lo, n)); }

  /** Settings from storage, checked defensively: anything wrong becomes the default and never stops the page. */
  function loadSettings(storage, defaults) {
    var out = { stripDismissed: false, scrollback: defaults.scrollback };
    try {
      var raw = storage.getItem(STORAGE_KEY);
      if (typeof raw !== 'string' || raw.length > 16384) return out;
      var obj = JSON.parse(raw);
      if (!obj || typeof obj !== 'object' || Array.isArray(obj)) return out;
      if (obj.stripDismissed === true) out.stripDismissed = true;
      if (typeof obj.scrollback === 'number' && isFinite(obj.scrollback)) out.scrollback = clamp(Math.round(obj.scrollback), 500, 10000);
    } catch (e) { /* corrupt or storage blocked: defaults */ }
    return out;
  }

  function saveSettings(storage, s) {
    try { storage.setItem(STORAGE_KEY, JSON.stringify({ stripDismissed: s.stripDismissed === true, scrollback: s.scrollback })); } catch (e) { /* storage full or blocked */ }
  }

  /** Decision 10: the largest size from 11 to 16 px at which 52 columns fit; charWidthPerPx is the width of one column per pixel of font size. */
  function fitFontSize(paneWidth, charWidthPerPx) {
    if (!(paneWidth > 0) || !(charWidthPerPx > 0)) return 12;
    var size = Math.floor((paneWidth / (52 * charWidthPerPx)) * 2) / 2;
    return clamp(size, 11, 16);
  }

  // ---------------------------------------------------------------------------------------------------------------------
  // The page
  // ---------------------------------------------------------------------------------------------------------------------

  function boot(win, doc) {
    var root = doc.getElementById('play');
    if (!root) return null;
    var $ = function (id) { return doc.getElementById(id); };
    var logEl = $('play-log'), formEl = $('play-form'), inputEl = $('play-input'), stateEl = $('play-state');
    var startEl = $('play-start'), noticeEl = $('play-notice'), enterEl = $('play-enter'), stripEl = $('play-strip');
    var newEl = $('play-newlines'), bannerEl = $('play-banner'), bannerText = $('play-banner-text');
    var closedEl = $('play-closed'), closedText = $('play-closed-text'), pauseEl = $('play-pause');
    var wsPath = root.getAttribute('data-ws') || '/play/ws';
    var statusPath = root.getAttribute('data-status') || '/play/status';
    var protocol = root.getAttribute('data-protocol') || 'hollowmere.v1';

    var phone = !!(win.matchMedia && win.matchMedia('(max-width: 899px)').matches);
    var storage = null;
    try { storage = win.localStorage; } catch (e) { storage = null; }
    var defaults = { scrollback: phone ? DEFAULT_SCROLLBACK_PHONE : DEFAULT_SCROLLBACK_DESKTOP };
    var settings = storage ? loadSettings(storage, defaults) : { stripDismissed: false, scrollback: defaults.scrollback };

    var ansi = createAnsi();
    var screen = createScreen(doc, logEl, { maxLines: settings.scrollback, hideEcho: true });
    var history = createHistory(100);

    var state = 'idle';      // idle, connecting, open, reconnecting, closed
    var ws = null, attempt = 0, retryTimer = null, countdownTimer = null, pingTimer = null, watchTimer = null, stableTimer = null;
    var pingN = 0, lastRecv = 0, probeTimer = null, pingSeconds = 25, lost = false, retrying = false;
    var userStopped = false, deadClose = false, lastSys = null, retryAt = 0;
    var maskFrame = false, netMask = false;
    var stuck = true, unseen = 0;
    var pending = [], flushScheduled = false;

    // ---- small helpers ----
    function setText(el, text) { el.textContent = text; }
    function show(el, on) { el.hidden = !on; }
    function setState(text) { setText(stateEl, text); }

    // ---- masking (docs/specs/web-client.md 2.10) ----
    function applyMask() {
      var on = maskFrame || netMask;
      if (on) {
        if (inputEl.type !== 'password') inputEl.value = '';
        inputEl.type = 'password';
        inputEl.setAttribute('autocomplete', 'off');
        inputEl.setAttribute('enterkeyhint', 'go');
      } else {
        if (inputEl.type !== 'text') inputEl.value = '';
        inputEl.type = 'text';
        inputEl.setAttribute('enterkeyhint', 'send');
      }
    }
    function isMasked() { return maskFrame || netMask; }
    function clearMask() { maskFrame = false; netMask = false; applyMask(); }

    // ---- output, scroll lock ----
    function atBottom() { return logEl.scrollHeight - logEl.scrollTop - logEl.clientHeight <= 4; }
    function toBottom() { logEl.scrollTop = logEl.scrollHeight; stuck = true; unseen = 0; show(newEl, false); }
    function updateMarker() {
      if (stuck || unseen <= 0) { show(newEl, false); return; }
      setText(newEl, unseen === 1 ? '1 new line below' : unseen + ' new lines below');
      show(newEl, true);
    }
    logEl.addEventListener('scroll', function () {
      stuck = atBottom();
      if (stuck) { unseen = 0; }
      updateMarker();
    });
    newEl.addEventListener('click', function () { toBottom(); inputEl.focus(); });

    function flush() {
      flushScheduled = false;
      if (pending.length === 0) return;
      var tokens = pending; pending = [];
      var lineEnds = 0;
      for (var i = 0; i < tokens.length; i++) if (tokens[i].k === 'n') lineEnds++;
      var live = logEl.getAttribute('aria-live');
      var flood = lineEnds > 200 && live === 'polite';
      var tailTokens = null;
      if (flood) {          // a flood: only the last 20 lines go to the live region, the rest are in the pane
        var seen = 0, cut = tokens.length;
        for (var j = tokens.length - 1; j >= 0 && seen < 20; j--) { if (tokens[j].k === 'n') seen++; cut = j; }
        tailTokens = tokens.slice(cut); tokens = tokens.slice(0, cut);
        logEl.setAttribute('aria-live', 'off');
      }
      screen.write(tokens);
      var beforeTrim = logEl.scrollHeight;
      var removed = screen.trim();
      if (removed > 0 && !stuck) logEl.scrollTop -= Math.max(0, beforeTrim - logEl.scrollHeight); // Safari has no scroll anchoring
      if (tailTokens) {
        logEl.setAttribute('aria-live', live);
        screen.write(tailTokens);
      }
      var done = screen.takeCompleted();
      if (stuck) toBottom(); else { unseen += done; updateMarker(); }
      // Safety net for the password prompt in case the mask frame never comes
      if (/(assword|again): $/i.test(screen.tail()) && !maskFrame) { netMask = true; applyMask(); }
    }
    function schedule() {
      if (flushScheduled) return;
      flushScheduled = true;
      if (win.requestAnimationFrame) win.requestAnimationFrame(function () { if (flushScheduled) flush(); });
      win.setTimeout(function () { if (flushScheduled) flush(); }, 120); // a hidden tab never runs animation frames
    }
    function write(text) { var t = ansi.feed(text); for (var i = 0; i < t.length; i++) pending.push(t[i]); schedule(); }
    function note(text) {
      flush(); screen.note('*** ' + text + ' ***');
      var done = screen.takeCompleted();
      if (stuck) toBottom(); else { unseen += done; updateMarker(); }
    }

    // ---- states and banners ----
    function showSession(on) {
      show(logEl, on); show(formEl, on); show(pauseEl, on);
      show(startEl, !on);
    }
    function banner(text) { setText(bannerText, text); show(bannerEl, true); show(closedEl, false); }
    function hideBanners() { show(bannerEl, false); show(closedEl, false); }
    function closedBanner(text) { setText(closedText, text); show(closedEl, true); show(bannerEl, false); }

    function clearTimers() {
      [retryTimer, countdownTimer, stableTimer].forEach(function (t) { if (t) win.clearTimeout(t); });
      retryTimer = null; countdownTimer = null; stableTimer = null;
    }
    function stopPing() {
      if (pingTimer) win.clearInterval(pingTimer);
      if (watchTimer) win.clearInterval(watchTimer);
      if (probeTimer) win.clearTimeout(probeTimer);
      pingTimer = null; watchTimer = null; probeTimer = null;
    }

    function wsUrl() { return (win.location.protocol === 'https:' ? 'wss:' : 'ws:') + '//' + win.location.host + wsPath; }

    function connect() {
      clearTimers();
      userStopped = false; deadClose = false; lastSys = null;
      state = 'connecting';
      setState(retrying ? 'Reconnecting' : 'Connecting');
      hideBanners();
      showSession(true);
      var sock;
      relayout();
      try { sock = new win.WebSocket(wsUrl(), protocol); } catch (e) { ws = null; beginRetry(false); return; }
      ws = sock;
      sock.onopen = function () {
        if (ws !== sock) return;
        state = 'open';
        setState('Connected');
        lastRecv = Date.now();
        if (retrying) { note('Connected. Please sign in again.'); retrying = false; lost = false; }
        stableTimer = win.setTimeout(function () { attempt = 0; }, 60000);
        startKeepalive();
        inputEl.focus();
      };
      sock.onmessage = function (ev) { if (ws === sock) onMessage(ev.data); };
      sock.onclose = function (ev) { onClose(sock, ev); };
      sock.onerror = function () { /* a close event always follows */ };
    }

    function startKeepalive() {
      stopPing();
      // A small message of our own every PingSeconds: the bridge treats a silent page as dead, and it keeps the connection from idling out.
      pingTimer = win.setInterval(function () { pingN++; sendFrame({ t: 'ping', n: pingN }); }, pingSeconds * 1000);
      watchTimer = win.setInterval(function () {
        if (state === 'open' && Date.now() - lastRecv > 75000) declareDead();
      }, 5000);
    }

    function declareDead() {
      if (!ws) return;
      deadClose = true;
      try { ws.close(); } catch (e) { /* already closing */ }
      // Some browsers take long to report a dead socket; do not wait for them.
      var sock = ws;
      win.setTimeout(function () { if (ws === sock && state === 'open') onClose(sock, { code: 1006 }); }, 1500);
    }

    function probe() {
      // Woken up (phone unlocked, laptop opened): if nothing arrived for 30 s, ask, and give the answer 3 s.
      if (state !== 'open' || Date.now() - lastRecv < 30000) return;
      pingN++; sendFrame({ t: 'ping', n: pingN });
      if (probeTimer) win.clearTimeout(probeTimer);
      var mark = lastRecv;
      probeTimer = win.setTimeout(function () { if (state === 'open' && lastRecv === mark) declareDead(); }, 3000);
    }

    function sendFrame(obj) {
      if (!ws || ws.readyState !== 1) return false;
      try { ws.send(JSON.stringify(obj)); return true; } catch (e) { return false; }
    }

    function onMessage(data) {
      lastRecv = Date.now();
      if (typeof data !== 'string') return;
      var f;
      try { f = JSON.parse(data); } catch (e) { return; }
      if (!f || typeof f !== 'object' || typeof f.t !== 'string') return;
      switch (f.t) {
        case 'hello':
          if (typeof f.ping === 'number' && f.ping >= 5 && f.ping <= 60) { pingSeconds = f.ping; startKeepalive(); }
          break;
        case 'out':
          if (typeof f.d === 'string') write(f.d);
          break;
        case 'mask':
          maskFrame = f.on === true; if (!maskFrame) netMask = false; applyMask();
          break;
        case 'sys':
          if (typeof f.msg === 'string') {
            lastSys = { code: typeof f.code === 'string' ? f.code : '', msg: f.msg.slice(0, 300) };
            note(lastSys.msg);
          }
          break;
        default: break;   // gmcp (stage 2 uses it) and pong: nothing to draw in stage 1
      }
    }

    function onClose(sock, ev) {
      if (sock !== ws) return;
      ws = null;
      stopPing(); clearTimers();
      clearMask();
      flush();
      var wasOpen = state === 'open';
      var code = ev && typeof ev.code === 'number' ? ev.code : 1006;
      if (userStopped) { state = 'closed'; setState('Disconnected'); closedBanner('Disconnected.'); return; }
      if (code === 1000 && !deadClose) {
        state = 'closed'; setState('Disconnected');
        closedBanner((lastSys && lastSys.msg) || 'The connection was closed.');
        retrying = false; lost = false;
        return;
      }
      if (!wasOpen && !retrying && attempt === 0 && !lastSys) {
        // Refused before the socket opened: the site may be switched off, which the status page says plainly.
        checkStatus(function (s) {
          if (s && s.enabled === false) { state = 'idle'; showSession(false); showEntry(s); }
          else beginRetry(wasOpen);
        });
        return;
      }
      beginRetry(wasOpen);
    }

    function beginRetry(wasOpen) {
      state = 'reconnecting'; retrying = true;
      if (!lost) { lost = true; if (!lastSys) note('Connection lost'); }
      var secs = backoffDelay(attempt, Math.random);
      attempt++;
      retryAt = Date.now() + secs * 1000;
      setState('Reconnecting');
      var why = lastSys && (lastSys.code === 'full' || lastSys.code === 'unavailable') ? lastSys.msg + ' ' : 'Connection lost. ';
      function tick() {
        var left = Math.max(0, Math.ceil((retryAt - Date.now()) / 1000));
        banner(why + 'Trying again in ' + left + ' s.');
        countdownTimer = win.setTimeout(tick, 1000);
      }
      tick();
      retryTimer = win.setTimeout(function () { connect(); }, secs * 1000);
    }

    $('play-try').addEventListener('click', function () { if (state === 'reconnecting') connect(); });
    $('play-stop').addEventListener('click', function () { clearTimers(); userStopped = true; state = 'closed'; retrying = false; lost = false; setState('Disconnected'); closedBanner('Disconnected.'); });
    $('play-reconnect').addEventListener('click', function () { retrying = lost = false; attempt = 0; connect(); });

    // ---- sending ----
    function submit() {
      var masked = isMasked();
      var text = inputEl.value.replace(/[\r\n\u0000]+/g, ' ');
      if (state !== 'open') return;
      if (!sendFrame({ t: 'in', d: text })) return;
      inputEl.value = '';
      if (masked) { netMask = false; return; }      // nothing echoed, nothing remembered; the game sends the line end
      history.add(text);
      flush();
      screen.echo(text);
      screen.takeCompleted();
      toBottom();
    }
    formEl.addEventListener('submit', function (e) { e.preventDefault(); submit(); inputEl.focus(); });
    // Tapping Send or a command in the strip must not take focus from the input: the phone keyboard stays up.
    [formEl.querySelector('.play-send')].concat([].slice.call(doc.querySelectorAll('.play-code'))).forEach(function (b) {
      if (b) b.addEventListener('pointerdown', function (e) { e.preventDefault(); });
    });
    [].slice.call(doc.querySelectorAll('.play-code')).forEach(function (b) {
      b.addEventListener('click', function () {
        if (formEl.hidden || isMasked()) return;
        var add = b.getAttribute('data-insert') || '';
        inputEl.value = (inputEl.value === '' ? '' : inputEl.value.replace(/\s*$/, ' ')) + add;
        inputEl.focus();
      });
    });

    inputEl.addEventListener('keydown', function (e) {
      if (e.key === 'ArrowUp' && !isMasked()) { var p = history.prev(inputEl.value); if (p !== null) { inputEl.value = p; e.preventDefault(); } }
      else if (e.key === 'ArrowDown' && !isMasked()) { var n = history.next(); if (n !== null) { inputEl.value = n; e.preventDefault(); } }
      else if (e.key === 'Escape') { inputEl.value = ''; history.reset(); }
      else if (e.key === 'PageUp') { logEl.scrollTop -= Math.max(40, logEl.clientHeight * 0.9); e.preventDefault(); }
      else if (e.key === 'PageDown') { logEl.scrollTop += Math.max(40, logEl.clientHeight * 0.9); e.preventDefault(); }
      else if (e.key === 'End' && inputEl.selectionStart === inputEl.value.length) { toBottom(); }
    });

    // ---- pause announcements (aria-live) ----
    pauseEl.addEventListener('click', function () {
      var paused = pauseEl.getAttribute('aria-pressed') === 'true';
      pauseEl.setAttribute('aria-pressed', paused ? 'false' : 'true');
      logEl.setAttribute('aria-live', paused ? 'polite' : 'off');
    });

    // ---- the how-to-play strip ----
    show(stripEl, !settings.stripDismissed);
    $('play-strip-hide').addEventListener('click', function () {
      settings.stripDismissed = true; show(stripEl, false);
      if (storage) saveSettings(storage, settings);
      inputEl.focus();
    });

    // ---- the page before connecting ----
    function showEntry(s) {
      show(enterEl, false); show(noticeEl, false);
      if (!s || s.enabled === true && s.full !== true) { show(enterEl, true); return; }
      if (s.enabled === false) { setText(noticeEl, 'The browser client is switched off right now. You can still play with a MUD client or telnet: see How to play.'); show(noticeEl, true); return; }
      setText(noticeEl, 'The game is full right now. Please try again in a few minutes.');
      show(noticeEl, true);
    }
    function checkStatus(cb) {
      if (!win.fetch) { cb(null); return; }
      win.fetch(statusPath, { cache: 'no-store', credentials: 'omit' })
        .then(function (r) { return r.ok ? r.json() : null; })
        .then(function (j) { cb(j && typeof j === 'object' ? j : null); })
        .catch(function () { cb(null); });
    }
    function pollStatus() {
      if (state !== 'idle') return;
      checkStatus(function (s) { if (state === 'idle') showEntry(s); });
    }
    enterEl.addEventListener('click', function () { retrying = lost = false; attempt = 0; connect(); });
    pollStatus();
    win.setInterval(pollStatus, 30000);

    // ---- leaving, waking ----
    win.addEventListener('pagehide', function () {
      if (ws && state === 'open') { userStopped = true; try { ws.close(1000); } catch (e) { /* gone */ } }
    });
    win.addEventListener('pageshow', function (e) {
      if (e && e.persisted && state !== 'idle') {
        clearTimers(); state = 'closed'; userStopped = true; setState('Disconnected');
        closedBanner('You left this page; the game connection was closed.');
      }
    });
    function wake() {
      if (state === 'reconnecting') { clearTimers(); connect(); } else probe();
    }
    doc.addEventListener('visibilitychange', function () {
      if (doc.hidden) { if (isMasked()) inputEl.value = ''; } else wake();
    });
    win.addEventListener('online', wake);

    // ---- layout: font size for 52 columns on a phone, the visible viewport for the keyboard ----
    function fitViewport() {
      var vv = win.visualViewport;
      var h = vv ? vv.height : win.innerHeight;
      doc.documentElement.style.setProperty('--vh', Math.round(h) + 'px');
      if (stuck) logEl.scrollTop = logEl.scrollHeight;
    }
    function fitFont() {
      if (!(win.matchMedia && win.matchMedia('(max-width: 899px)').matches)) { logEl.style.removeProperty('font-size'); return; }
      var probeEl = doc.createElement('span');
      probeEl.className = 'play-measure';
      probeEl.appendChild(doc.createTextNode(new Array(53).join('0')));   // 52 columns
      logEl.parentNode.appendChild(probeEl);
      var perPx = probeEl.getBoundingClientRect().width / 52 / 100;       // measured at 100 px
      logEl.parentNode.removeChild(probeEl);
      var cs = win.getComputedStyle(logEl);
      var inner = logEl.clientWidth - parseFloat(cs.paddingLeft || '0') - parseFloat(cs.paddingRight || '0');
      logEl.style.fontSize = fitFontSize(inner, perPx) + 'px';
    }
    function relayout() { fitViewport(); fitFont(); }
    if (win.visualViewport) { win.visualViewport.addEventListener('resize', relayout); win.visualViewport.addEventListener('scroll', fitViewport); }
    win.addEventListener('resize', relayout);
    win.addEventListener('orientationchange', relayout);
    relayout();

    return { screen: screen, flush: flush, state: function () { return state; } };
  }

  var api = {
    createAnsi: createAnsi, createScreen: createScreen, createHistory: createHistory, backoffDelay: backoffDelay,
    loadSettings: loadSettings, saveSettings: saveSettings, fitFontSize: fitFontSize, boot: boot
  };
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else if (typeof window !== 'undefined') {
    var go = function () { boot(window, document); };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', go); else go();
  }
})();
