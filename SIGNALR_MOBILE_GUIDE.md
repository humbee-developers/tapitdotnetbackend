# TapitAI — SignalR Real-Time Connection Guide (React Native)

---

## Root Cause of Your Error

The three errors in 233ms tell the full story — the backend was never reached:

```
11:16:12.394  "connection was stopped during negotiation"   ← StrictMode mount #1 cleanup
11:16:12.404  "connection was stopped during negotiation"   ← StrictMode mount #2 cleanup
11:16:12.627  "Failed to start HttpConnection before stop"  ← race: stop() before start() resolves
```

**Two bugs in the mobile code:**

| Bug | Cause |
|---|---|
| Errors 1 & 2 | React StrictMode mounts → unmounts → remounts every component in dev. `start()` begins, cleanup immediately calls `stop()`. |
| Error 3 | The connection is created inside `useEffect`. The cleanup runs before `start()` resolves, calling `stop()` while `start()` is still in-flight. |

**The fix: never create the hub connection inside a component.**

---

## Install

```bash
npm install @microsoft/signalr
```

---

## Step 1 — Singleton Hub Service

Create this file once. It lives outside React.

```js
// src/services/SignalRService.js
import * as signalR from '@microsoft/signalr';

const HUB_URL = 'http://192.168.1.9:3000/hubs/connection';

class SignalRService {
  constructor() {
    this._connection = null;
    this._getToken = null;
    this._starting = false;
  }

  /** Call once at app startup with a function that returns the current Auth0 access token */
  configure(getTokenFn) {
    this._getToken = getTokenFn;
  }

  _build() {
    return new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
        accessTokenFactory: () => this._getToken(),   // SDK URL-encodes the JWT automatically
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }

  async start() {
    // Guard: don't start if already connected or starting
    if (this._starting) return;
    if (this._connection?.state === signalR.HubConnectionState.Connected) return;

    this._starting = true;

    try {
      if (this._connection) {
        await this._connection.stop().catch(() => {});
        this._connection = null;
      }

      this._connection = this._build();

      this._connection.onclose((err) => {
        console.warn('[SignalR] Closed:', err?.message ?? 'clean');
        this._starting = false;
      });
      this._connection.onreconnecting(() => console.log('[SignalR] Reconnecting...'));
      this._connection.onreconnected((id) => console.log('[SignalR] Reconnected, id=', id));

      await this._connection.start();
      console.log('[SignalR] ✅ Connected');
    } catch (err) {
      console.error('[SignalR] ❌ Failed:', err?.message);
      this._connection = null;
      throw err;
    } finally {
      this._starting = false;
    }
  }

  async stop() {
    if (this._connection) {
      await this._connection.stop().catch(() => {});
      this._connection = null;
    }
  }

  /** Register an event listener. Safe to call before start(). */
  on(event, handler) {
    this._connection?.on(event, handler);
  }

  /** Remove an event listener. */
  off(event, handler) {
    this._connection?.off(event, handler);
  }

  get state() {
    return this._connection?.state ?? signalR.HubConnectionState.Disconnected;
  }
}

export const hubService = new SignalRService();
```

---

## Step 2 — Connect Once After Login

Call `start()` exactly once — in your Auth callback or root navigator,
**not** inside any screen component.

```js
// App.js  or  src/navigation/RootNavigator.js
import { useEffect } from 'react';
import { useAuth0 } from 'react-native-auth0';
import { hubService } from './src/services/SignalRService';
import { registerHubEvents } from './src/services/hubEvents';

export default function App() {
  const { getCredentials, isAuthenticated } = useAuth0();

  useEffect(() => {
    if (!isAuthenticated) return;

    async function connect() {
      hubService.configure(async () => {
        const creds = await getCredentials();
        return creds.accessToken;   // ← must be the ACCESS token, not the ID token
      });

      await hubService.start();
      registerHubEvents();          // register all global event handlers
    }

    connect().catch(console.error);

    return () => {
      // Only stop on actual logout, not on every render
      // hubService.stop();  ← call this only from your logout handler
    };
  }, [isAuthenticated]);
}
```

---

## Step 3 — Register All Event Handlers

```js
// src/services/hubEvents.js
import { hubService } from './SignalRService';

export function registerHubEvents() {
  // Remove any previous handlers first to avoid duplicates
  unregisterHubEvents();

  hubService.on('ConnectionRequestReceived', onConnectionRequestReceived);
  hubService.on('ConnectionRequestSent',     onConnectionRequestSent);
  hubService.on('ConnectionAccepted',        onConnectionAccepted);
  hubService.on('ConnectionRejected',        onConnectionRejected);
  hubService.on('ConnectionWithdrawn',       onConnectionWithdrawn);
  hubService.on('ConnectionPassed',          onConnectionPassed);
  hubService.on('WaitingForPartner',         onWaitingForPartner);
  hubService.on('ChatStarted',               onChatStarted);
  hubService.on('TapStatusChanged',          onTapStatusChanged);
}

export function unregisterHubEvents() {
  hubService.off('ConnectionRequestReceived', onConnectionRequestReceived);
  hubService.off('ConnectionRequestSent',     onConnectionRequestSent);
  hubService.off('ConnectionAccepted',        onConnectionAccepted);
  hubService.off('ConnectionRejected',        onConnectionRejected);
  hubService.off('ConnectionWithdrawn',       onConnectionWithdrawn);
  hubService.off('ConnectionPassed',          onConnectionPassed);
  hubService.off('WaitingForPartner',         onWaitingForPartner);
  hubService.off('ChatStarted',               onChatStarted);
  hubService.off('TapStatusChanged',          onTapStatusChanged);
}

// ── Handlers ─────────────────────────────────────────────────────────────────

function onConnectionRequestReceived(data) {
  /*
  data = {
    connectionId:      string,   (UUID)
    senderMaskedName:  string,   e.g. "J*y"
    senderAgeRange:    string,   e.g. "25-30"
    senderGender:      string,   e.g. "MALE"
    message:           string | null,
    initiatedVia:      string,   "Map" | "System" | "Spotlight"
    expiresAt:         string,   ISO datetime
  }
  UI: Show full-screen bottom sheet modal with accept/reject buttons + countdown timer
  */
  console.log('[Hub] ConnectionRequestReceived', data);
}

function onConnectionRequestSent(data) {
  /*
  data = {
    connectionId:       string,
    receiverMaskedName: string,
    expiresAt:          string,
  }
  UI: Show toast "Request sent to S*r*h — expires in 30 min"
      Dim that user's pin on the map
  */
  console.log('[Hub] ConnectionRequestSent', data);
}

function onConnectionAccepted(data) {
  /*
  data = {
    connectionId:        string,
    otherUserDisplayName: string,  ← REAL name revealed here
    otherUserPhotoUrl:   string,   ← REAL photo revealed here
    otherUserAgeRange:   string,
    message:             string,
  }
  UI: Celebration modal — confetti, real photo fades in, "It's a Match!" headline
      Two buttons: "Start Chat" and "Later"
  */
  console.log('[Hub] ConnectionAccepted', data);
}

function onConnectionRejected(data) {
  /*
  data = { connectionId: string, message: string | null }
  UI: Subtle toast "Your request wasn't accepted this time"
      Remove from pending list (do NOT show their message)
  */
  console.log('[Hub] ConnectionRejected', data);
}

function onConnectionWithdrawn(data) {
  /*
  data = { connectionId: string }
  UI: Silently dismiss the invitation card/modal if open
  */
  console.log('[Hub] ConnectionWithdrawn', data);
}

function onConnectionPassed(data) {
  /*
  data = { connectionId: string, message: string | null }
  UI: Toast "Your match decided to pass — keep going!"
      Remove from connections list
  */
  console.log('[Hub] ConnectionPassed', data);
}

function onWaitingForPartner(data) {
  /*
  data = { connectionId: string, message: string }
  UI: Banner on the connection card "Your match wants to chat! Tap to confirm"
      Push notification if app is backgrounded
  */
  console.log('[Hub] WaitingForPartner', data);
}

function onChatStarted(data) {
  /*
  data = {
    connectionId:              string,
    chatChannelId:             string,  ← use this with Stream Chat SDK
    senderConnectionMessage:   string | null,
    receiverConnectionMessage: string | null,
  }
  UI: Navigate to chat screen using data.chatChannelId
      or show "Chat is ready! Open Chat" banner
  */
  console.log('[Hub] ChatStarted', data);
}

function onTapStatusChanged(data) {
  /*
  data = {
    userId:        string,
    status:        "TappedIn" | "TappedOut",
    autoTapInAt:   string | null,  ISO datetime
    tapOutReason:  string | null,
  }
  UI: Update Tap button:
      TappedIn  → green pulsing dot, "Tap Out" button
      TappedOut → grey dot, "Tap In" button, countdown to autoTapInAt
  */
  console.log('[Hub] TapStatusChanged', data);
}
```

---

## Step 4 — Logout

```js
// Call from your logout button handler
import { hubService } from './src/services/SignalRService';
import { unregisterHubEvents } from './src/services/hubEvents';

async function logout() {
  unregisterHubEvents();
  await hubService.stop();
  // then call Auth0 logout
}
```

---

## Step 5 — Token Refresh

When Auth0 refreshes the token, restart the hub (the `accessTokenFactory` will
call your `getCredentials()` fresh on next reconnect automatically).
You only need to force a restart if you want to guarantee fresh auth immediately:

```js
async function onTokenRefreshed() {
  await hubService.stop();
  await hubService.start();
}
```

---

## Listening to Events in a Specific Screen

If a screen needs to react to an event, add/remove inside `useEffect`.
Always remove on unmount to avoid duplicate handlers.

```js
import { useEffect } from 'react';
import { hubService } from '../services/SignalRService';

export function InvitationsScreen() {
  useEffect(() => {
    function handleRequest(data) {
      fetchInvitations(); // refresh list
    }

    hubService.on('ConnectionRequestReceived', handleRequest);
    return () => hubService.off('ConnectionRequestReceived', handleRequest);
  }, []);
}
```

---

## Verification Checklist

Run through these in order — stop at the first failure:

```
[ ] 1. Backend is running:
        dotnet run --project src/TapitAI.API --launch-profile http
        Should see "Now listening on: http://0.0.0.0:3000"

[ ] 2. REST works from device:
        fetch('http://192.168.1.9:3000/api/tap-status/me', {
          headers: { Authorization: `Bearer ${accessToken}` }
        }).then(r => r.json()).then(console.log)
        Should return TapStatus (not 401/403)

[ ] 3. Token is the ACCESS token (not ID token):
        Paste token at https://jwt.io
        "aud" claim should be your Auth0 API identifier
        NOT the Auth0 client ID
        "exp" must be in the future

[ ] 4. Hub connection with debug logging:
        .configureLogging(signalR.LogLevel.Debug)
        Run and paste full log output — server will show:
        "[Hub] Client connected: connectionId=... userId=..."
        if the connection reaches the server

[ ] 5. Server logs show connection:
        After step 4, check backend terminal for:
        "[Hub] Client connected" → success
        No log at all → connection not reaching server (network/firewall)
        "[Hub] Client disconnected with error" → server-side auth failure
```

---

## If It Still Fails After All the Above

Add this one-time test call right after login to isolate the issue:

```js
import * as signalR from '@microsoft/signalr';

async function testConnection(accessToken) {
  console.log('[Test] Token preview:', accessToken?.substring(0, 30) + '...');

  const conn = new signalR.HubConnectionBuilder()
    .withUrl('http://192.168.1.9:3000/hubs/connection', {
      skipNegotiation: true,
      transport: signalR.HttpTransportType.WebSockets,
      accessTokenFactory: () => accessToken,
    })
    .configureLogging(signalR.LogLevel.Debug)
    .build();

  conn.onclose(e => console.log('[Test] onclose:', e?.message));

  try {
    await conn.start();
    console.log('[Test] ✅ STATE:', conn.state);  // should be "Connected"
    await conn.stop();
  } catch (e) {
    console.error('[Test] ❌ ERROR:', e.message);
  }
}
```

**Share the full `[Test]` log output** — that will show exactly where it fails.
