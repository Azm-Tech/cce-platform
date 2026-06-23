(function() {
  'use strict';

  let connection = null;
  let eventCount = 0;

  // Phase 1 envelope tracking — eventId for dedup, occurredOn for ordering + Phase 3 since cursor.
  let lastEventId = null;
  let lastEventTime = null;
  let sinceEdited = false;  // user-typed-into-since-field flag (don't auto-overwrite once edited)

  const PROD_URL = 'https://cce-external-api.runasp.net';
  const INTERNAL_URL = 'http://localhost:5002';

  const $ = id => document.getElementById(id);
  const logEl = $('log');
  const eventCountEl = $('eventCount');
  const lastEventEl = $('lastEvent');
  const catchUpSinceEl = $('catchUpSince');

  // Stop auto-filling the since field once the user manually edits it.
  catchUpSinceEl.addEventListener('input', function() { sinceEdited = true; });

  function toggleMode() {
    var mode = document.querySelector('input[name="mode"]:checked').value;
    $('serverUrl').value = mode === 'prod' ? PROD_URL
                         : mode === 'internal5002' ? INTERNAL_URL
                         : '';
    // Dev sign-in is also available on the Internal API (Auth:DevMode=true).
    $('btnDevSignIn').disabled = (mode === 'prod');
    $('devRole').disabled = (mode === 'prod');
  }

  window.toggleMode = toggleMode;

  function updateLastEvent() {
    if (!lastEventId) {
      lastEventEl.textContent = '(no events yet)';
      return;
    }
    var shortId = lastEventId.slice(0, 8);
    lastEventEl.textContent = 'last: ' + shortId + ' @ ' + lastEventTime;
    if (!sinceEdited) catchUpSinceEl.value = lastEventTime || '';
  }

  function log(type, eventName, payload) {
    const now = new Date();
    const time = now.toLocaleTimeString('en-US', { hour12: false }) + '.' + String(now.getMilliseconds()).padStart(3, '0');
    const line = document.createElement('div');
    line.className = 'log-entry';
    const typeClass = type === 'error' ? 'log-error' : type === 'info' ? 'log-info' : type === 'event' ? 'log-event' : 'log-payload';
    line.innerHTML = '<span class="log-time">[' + time + ']</span> <span class="' + typeClass + '">' + eventName + '</span>' + (payload ? ' ' + syntaxHighlight(payload) : '');
    logEl.appendChild(line);
    logEl.scrollTop = logEl.scrollHeight;
    if (type !== 'info') {
      eventCount++;
      eventCountEl.textContent = eventCount + ' events';
    }
  }

  function syntaxHighlight(obj) {
    var json = typeof obj === 'string' ? obj : JSON.stringify(obj, null, 1);
    return json.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/"([^"]+)":/g, '<span style="color:#9cdcfe">"$1"</span>:')
      .replace(/"([^"]+)"/g, '<span style="color:#ce9178">"$1"</span>')
      .replace(/\b(true|false|null)\b/g, '<span style="color:#569cd6">$1</span>')
      .replace(/\b(\d+\.?\d*)\b/g, '<span style="color:#b5cea8">$1</span>');
  }

  function setStatus(connected, connectionId) {
    var badge = $('connectionStatus');
    var idLabel = $('connectionIdLabel');
    if (connected) {
      badge.textContent = 'Connected';
      badge.className = 'badge badge-connected';
      idLabel.textContent = 'ConnectionId: ' + (connectionId || '');
      $('btnConnect').disabled = true;
      $('btnDisconnect').disabled = false;
      $('serverUrl').disabled = true;
      $('jwtToken').disabled = true;
    } else {
      badge.textContent = 'Disconnected';
      badge.className = 'badge badge-disconnected';
      idLabel.textContent = '';
      $('btnConnect').disabled = false;
      $('btnDisconnect').disabled = true;
      $('serverUrl').disabled = false;
      $('jwtToken').disabled = false;
    }
  }

  function getGuid(id) {
    var val = $(id).value.trim();
    if (!val) throw new Error(id.replace('Id','') + ' ID is required');
    return val;
  }

  var events = [
    'ReceiveNotification', 'NewReply', 'VoteChanged', 'PollResultsChanged',
    'NewPost', 'PostModerated', 'ContentModerated', 'PresenceChanged', 'TypingChanged'
  ];

  $('btnDevSignIn').addEventListener('click', async function() {
    var baseUrl = $('serverUrl').value.trim() || '';
    var role = $('devRole').value;
    try {
      var res = await fetch(baseUrl + '/dev/sign-in?role=' + encodeURIComponent(role), {
        credentials: 'same-origin'
      });
      if (!res.ok) {
        var text = await res.text();
        log('error', 'Dev sign-in failed (' + res.status + ')', text);
        return;
      }
      var data = await res.json();
      log('info', 'Signed in as ' + role, data);
    } catch (err) {
      log('error', 'Dev sign-in error', err.message);
    }
  });

  $('btnConnect').addEventListener('click', async function() {
    if (connection) return;

    var url = ($('serverUrl').value.trim().replace(/\/+$/, '') || '') + '/hubs/notifications';
    var token = $('jwtToken').value.trim();

    var options = {};

    if (token) {
      options.accessTokenFactory = function() { return token; };
    }

    connection = new signalR.HubConnectionBuilder()
      .withUrl(url, options)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    events.forEach(function(evt) {
      connection.on(evt, function(envelope) {
        // Phase 1 contract: every push is wrapped in { eventId, occurredOn, payload }.
        // Unwrap so the log shows the inner payload; record eventId/occurredOn for
        // dedup + Phase 3 catch-up cursor. Fallback: dump the raw arg if an old
        // (non-enveloped) server was hit so the harness still works after a rollback.
        if (envelope && typeof envelope === 'object'
            && 'eventId' in envelope && 'occurredOn' in envelope && 'payload' in envelope) {
          lastEventId = envelope.eventId;
          lastEventTime = envelope.occurredOn;
          updateLastEvent();
          var shortId = envelope.eventId.slice(0, 8);
          log('event', evt + '  [eid ' + shortId + ']', envelope.payload);
          log('info', '  ↳ eventId=' + envelope.eventId + '  occurredOn=' + envelope.occurredOn);
        } else {
          log('event', evt, envelope);
        }
      });
    });

    connection.onreconnecting(function() {
      log('info', 'Connection reconnecting...');
      setStatus(false);
    });

    connection.onreconnected(function(connectionId) {
      log('info', 'Reconnected as ' + connectionId);
      setStatus(true, connectionId);
    });

    connection.onclose(function(err) {
      log(err ? 'error' : 'info', 'Connection closed' + (err ? ': ' + err.message : ''));
      setStatus(false);
      connection = null;
    });

    try {
      await connection.start();
      log('info', 'Connected to ' + url);
      setStatus(true, connection.connectionId);
    } catch (err) {
      log('error', 'Connection failed', err.message);
      connection = null;
      setStatus(false);
    }
  });

  $('btnDisconnect').addEventListener('click', async function() {
    if (!connection) return;
    try {
      await connection.stop();
    } catch (err) {
      log('error', 'Disconnect error', err.message);
    }
    connection = null;
    setStatus(false);
  });

  $('btnClearLog').addEventListener('click', function() {
    logEl.innerHTML = '';
    eventCount = 0;
    eventCountEl.textContent = '0 events';
  });

  $('btnSubscribe').addEventListener('click', function() {
    if (!connection) { log('error', 'Not connected'); return; }
    try { connection.invoke('Subscribe', getGuid('postId')); log('info', 'Subscribe(' + $('postId').value.trim() + ')'); }
    catch (e) { log('error', 'Subscribe failed', e.message); }
  });

  $('btnUnsubscribe').addEventListener('click', function() {
    if (!connection) { log('error', 'Not connected'); return; }
    try { connection.invoke('Unsubscribe', getGuid('postId')); log('info', 'Unsubscribe(' + $('postId').value.trim() + ')'); }
    catch (e) { log('error', 'Unsubscribe failed', e.message); }
  });

  $('btnSubscribeCommunity').addEventListener('click', function() {
    if (!connection) { log('error', 'Not connected'); return; }
    try { connection.invoke('SubscribeCommunity', getGuid('communityId')); log('info', 'SubscribeCommunity(' + $('communityId').value.trim() + ')'); }
    catch (e) { log('error', 'SubscribeCommunity failed', e.message); }
  });

  $('btnUnsubscribeCommunity').addEventListener('click', function() {
    if (!connection) { log('error', 'Not connected'); return; }
    try { connection.invoke('UnsubscribeCommunity', getGuid('communityId')); log('info', 'UnsubscribeCommunity(' + $('communityId').value.trim() + ')'); }
    catch (e) { log('error', 'UnsubscribeCommunity failed', e.message); }
  });

  $('btnSubscribeTopic').addEventListener('click', function() {
    if (!connection) { log('error', 'Not connected'); return; }
    try { connection.invoke('SubscribeTopic', getGuid('topicId')); log('info', 'SubscribeTopic(' + $('topicId').value.trim() + ')'); }
    catch (e) { log('error', 'SubscribeTopic failed', e.message); }
  });

  $('btnUnsubscribeTopic').addEventListener('click', function() {
    if (!connection) { log('error', 'Not connected'); return; }
    try { connection.invoke('UnsubscribeTopic', getGuid('topicId')); log('info', 'UnsubscribeTopic(' + $('topicId').value.trim() + ')'); }
    catch (e) { log('error', 'UnsubscribeTopic failed', e.message); }
  });

  $('btnStartTyping').addEventListener('click', function() {
    if (!connection) { log('error', 'Not connected'); return; }
    try { connection.invoke('StartTyping', getGuid('postId')); log('info', 'StartTyping(' + $('postId').value.trim() + ')'); }
    catch (e) { log('error', 'StartTyping failed', e.message); }
  });

  $('btnStopTyping').addEventListener('click', function() {
    if (!connection) { log('error', 'Not connected'); return; }
    try { connection.invoke('StopTyping', getGuid('postId')); log('info', 'StopTyping(' + $('postId').value.trim() + ')'); }
    catch (e) { log('error', 'StopTyping failed', e.message); }
  });

  // Phase 3 — reconnect catch-up. Calls the GetPostActivity endpoint with the since cursor
  // (auto-filled from the last enveloped event's occurredOn; user can override). Note the
  // endpoint is AllowAnonymous, so no Authorization header is needed. When pointing at the
  // Internal API (port 5002) the fetch is cross-origin — if it fails with a CORS error,
  // either serve the harness from the Internal API's own wwwroot or open it directly.
  $('btnCatchUp').addEventListener('click', async function() {
    var postId = $('postId').value.trim();
    if (!postId) { log('error', 'Post ID required for catch-up'); return; }
    var baseUrl = $('serverUrl').value.trim().replace(/\/+$/, '');
    var since = ($('catchUpSince').value.trim() || lastEventTime || '');
    if (!since) { log('error', 'No since cursor — connect and receive at least one event first, or type a timestamp'); return; }
    var url = baseUrl + '/api/community/posts/' + encodeURIComponent(postId)
            + '/activity?since=' + encodeURIComponent(since);
    log('info', 'GET ' + url);
    try {
      var res = await fetch(url, { credentials: 'same-origin' });
      var body = await res.text();
      if (!res.ok) { log('error', 'Catch-up ' + res.status, body); return; }
      var data = JSON.parse(body);
      // Standard Response<T> envelope: { success, code, data, errors, ... }
      var payload = data && data.data ? data.data : data;
      var newCount = payload && payload.newReplies ? payload.newReplies.length : 0;
      log('event', 'Catch-up result', payload);
      log('info', '  ↳ ' + newCount + ' new replies, upvote=' + (payload ? payload.upvoteCount : '?') + ', score=' + (payload ? payload.score : '?'));
    } catch (err) { log('error', 'Catch-up error', err.message); }
  });

  log('info', 'Ready. Enter your connection details and click Connect.');
})();
