(function() {
  'use strict';

  let connection = null;
  let eventCount = 0;

  const PROD_URL = 'https://cce-external-api.runasp.net';

  const $ = id => document.getElementById(id);
  const logEl = $('log');
  const eventCountEl = $('eventCount');

  function toggleMode() {
    var isProd = document.querySelector('input[name="mode"]:checked').value === 'prod';
    $('serverUrl').value = isProd ? PROD_URL : '';
    $('btnDevSignIn').disabled = isProd;
    $('devRole').disabled = isProd;
  }

  window.toggleMode = toggleMode;

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
      connection.on(evt, function() {
        var args = Array.prototype.slice.call(arguments);
        var payload = args.length === 1 ? args[0] : args;
        log('event', evt, payload);
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

  log('info', 'Ready. Enter your connection details and click Connect.');
})();
