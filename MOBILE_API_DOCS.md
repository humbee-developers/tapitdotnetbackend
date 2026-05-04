# TapitAI Mobile API & Real-Time Integration Prompt

> Give this entire document to your mobile developer or AI assistant to implement the mobile side of TapitAI.

---

## Context

You are building the mobile client (React Native) for **TapitAI**, a proximity-based dating app. The backend is a .NET 10 REST API with SignalR for real-time events and Stream Chat for messaging.

- **Base URL (local dev):** `http://192.168.1.9:3000`
- **Base URL (production):** _to be set_
- **Auth:** Auth0 JWT. Send the token as `Authorization: Bearer <token>` on every request except `/api/users/sync`.
- **All responses** are wrapped: `{ "succeeded": true, "data": { ... }, "errors": [] }`

---

## Authentication Flow

### 1. After Auth0 login — sync user to backend

```
POST /api/users/sync
Authorization: Bearer <auth0_token>   ← raw Auth0 token, no role required
Content-Type: application/json

{
  "auth0UserId": "auth0|abc123",
  "email": "user@example.com",
  "firstName": "Jay",
  "lastName": "Patel",
  "pictureUrl": "https://..."   // optional
}
```

**Response:**
```json
{
  "succeeded": true,
  "data": {
    "id": "auth0|abc123",
    "email": "user@example.com",
    "firstName": "Jay",
    "lastName": "Patel",
    "profileImageUrl": null,
    "role": "User",
    "isActive": true,
    "createdAt": "2026-05-04T10:00:00Z"
  }
}
```

> Call this immediately after every Auth0 login. It creates the account on first call and updates it on subsequent calls.

---

### 2. Register device for push notifications

```
POST /api/device/register
Content-Type: application/json

{
  "fcmToken": "firebase_device_token_here",
  "devicePlatform": "android"   // or "ios"
}
```

**Response:** `{ "succeeded": true }`

> Call this once per app launch after syncing the user.

---

## Dating Profile

### Get my profile

```
GET /api/dating-profile
GET /api/dating-profile/me    ← alias, both work
```

**Response:**
```json
{
  "succeeded": true,
  "data": {
    "id": "uuid",
    "userId": "auth0|abc123",
    "displayName": "Jay",
    "gender": "MALE",
    "genderPreference": ["FEMALE"],
    "ageRange": "25-30",
    "heightFt": 5,
    "heightIn": 10,
    "heightPreference": ["5'6\"-5'10\""],
    "lifestyle": ["Active", "Foodie"],
    "lookingFor": ["LongTerm"],
    "bio": "Hey there!",
    "primaryPhotoUrl": "https://...",
    "primaryPhotoId": "uuid",
    "photos": [
      { "id": "uuid", "publicUrl": "https://...", "displayOrder": 1, "isPrimary": true }
    ],
    "videos": [
      { "id": "uuid", "publicUrl": "https://...", "displayOrder": 1 }
    ]
  }
}
```

---

### Create or update profile (upsert)

```
PUT /api/dating-profile
Content-Type: application/json

{
  "displayName": "Jay",
  "gender": "MALE",
  "genderPreference": ["FEMALE", "NON_BINARY"],
  "ageRange": "25-30",
  "heightFt": 5,
  "heightIn": 10,
  "heightPreference": ["5'6\"-5'10\""],
  "lifestyle": ["Active", "Foodie"],
  "lookingFor": ["LongTerm"],
  "bio": "Hey there!"
}
```

**Response:** same shape as GET profile above.

> Send all fields every time (full update). Arrays can be empty `[]`.

---

### Upload photos

```
POST /api/dating-profile/photos
Content-Type: multipart/form-data

photos: [File, File, ...]   ← field name must be "photos"
```

**Response:** `{ "succeeded": true }`

---

### Set primary photo

```
PUT /api/dating-profile/photos/{photoId}/primary
```

**Response:** `{ "succeeded": true }`

---

### Delete photo

```
DELETE /api/dating-profile/photos/{photoId}
```

**Response:** `{ "succeeded": true }`

---

### Upload video

```
POST /api/dating-profile/videos
Content-Type: multipart/form-data

video: File   ← field name must be "video"
```

**Response:** `{ "succeeded": true }`

---

### Delete video

```
DELETE /api/dating-profile/videos/{videoId}
```

**Response:** `{ "succeeded": true }`

---

## Location

### Update location (call whenever device GPS updates)

```
PUT /api/location
Content-Type: application/json

{
  "longitude": 72.8777,
  "latitude": 19.0760,
  "accuracy": 10.5   // optional, meters
}
```

**Response:** `{ "succeeded": true }`

> Required before discovery works. Send every 30–60 seconds while app is active.

---

## Tap Status (Online / Offline)

### Get my tap status

```
GET /api/tap-status
GET /api/tap-status/me    ← alias, both work
```

**Response:**
```json
{
  "succeeded": true,
  "data": {
    "userId": "auth0|abc123",
    "status": "TappedIn",        // "TappedIn" or "TappedOut"
    "autoTapInAt": null,         // DateTime if tapped out with duration
    "tapOutReason": null
  }
}
```

---

### Tap In (go online / available)

```
POST /api/tap-status/tapin
```

**Response:** `{ "succeeded": true, "data": { ...TapStatusDto } }`

---

### Tap Out (go offline)

```
POST /api/tap-status/tapout
Content-Type: application/json

{
  "durationMinutes": 30,    // how long to stay tapped out (then auto tap-in)
  "reason": "In a meeting"  // optional
}
```

**Response:** `{ "succeeded": true, "data": { ...TapStatusDto } }`

---

## Discovery (Map / Nearby Users)

### Get nearby users

```
GET /api/discovery
GET /api/discovery?radius=50    // optional override in miles, default from admin settings
```

**Response:**
```json
{
  "succeeded": true,
  "data": [
    {
      "userId": "auth0|xyz",
      "maskedName": "J*y",
      "ageRange": "25-30",
      "selfGender": "FEMALE",
      "placeholderPhotoUrl": "https://...",
      "distanceMiles": 0.4,
      "canSendConnectionRequest": true,
      "existingConnectionId": null,
      "existingConnectionStatus": null
    }
  ]
}
```

> Names are masked (alternate characters replaced with `*`) for privacy until connection is accepted.
> `canSendConnectionRequest: false` means a pending/active connection already exists.

---

## Spotlight (Featured Users Feed)

### Get current spotlight session

```
GET /api/spotlight
GET /api/spotlight/current    ← alias, both work
```

**Response:**
```json
{
  "succeeded": true,
  "data": {
    "sessionId": "uuid",
    "generatedAt": "2026-05-04T10:00:00Z",
    "expiresAt": "2026-05-04T11:00:00Z",
    "feedItems": [
      {
        "spotlightSessionFeedId": "uuid",
        "userId": "auth0|xyz",
        "maskedName": "S*r*h",
        "ageRange": "22-27",
        "selfGender": "FEMALE",
        "placeholderPhotoUrl": "https://...",
        "distanceMiles": 0.0,
        "hasLiked": false,
        "canSendConnectionRequest": true,
        "existingConnectionId": null,
        "viewedAt": null,
        "likedAt": null
      }
    ]
  }
}
```

> Returns `null` data if no active session exists. Sessions are generated in the background.

---

### Like a user in spotlight (send a Pulse)

```
POST /api/spotlight/feed/{spotlightSessionFeedId}/like
```

**Response:** `{ "succeeded": true }`

---

## Pulses (Likes Sent)

### Get all pulses I've sent

```
GET /api/v1/pulses/sent
```

**Response:**
```json
{
  "succeeded": true,
  "data": [
    {
      "id": "uuid",
      "likedUserId": "auth0|xyz",
      "maskedName": "S*r*h",
      "placeholderPhotoUrl": "https://...",
      "ageRange": "22-27",
      "likedAt": "2026-05-04T10:30:00Z"
    }
  ]
}
```

---

## Connections

### Get all accepted connections

```
GET /api/connection
```

**Response:**
```json
{
  "succeeded": true,
  "data": [
    {
      "connectionId": "uuid",
      "otherUserId": "auth0|xyz",
      "otherUserDisplayName": "Sarah",        // revealed after acceptance
      "otherUserPrimaryPhotoUrl": "https://...", // revealed after acceptance
      "otherUserAgeRange": "22-27",
      "invitationStatus": "Accepted",
      "myConnectionStatus": "Connected",       // null | "Connected"
      "partnerConnectionStatus": "Connected",
      "chatChannelId": "connection-uuid",      // null until both start chat
      "invitedAt": "2026-05-04T09:00:00Z",
      "connectedAt": "2026-05-04T09:05:00Z",
      "isSender": true
    }
  ]
}
```

---

### Get pending & rejected invitations

```
GET /api/connection/invitations
```

**Response:**
```json
{
  "succeeded": true,
  "data": [
    {
      "connectionId": "uuid",
      "otherUserMaskedName": "S*r*h",
      "otherUserPlaceholderPhotoUrl": "https://...",
      "otherUserAgeRange": "22-27",
      "invitationMessage": "Hey! I'd love to connect.",
      "initiatedVia": "Map",              // "Map" | "System" | "Spotlight"
      "invitedAt": "2026-05-04T09:00:00Z",
      "status": "Pending",               // "Pending" | "Rejected"
      "isSender": false
    }
  ]
}
```

---

### Send a connection request

```
POST /api/connection/send/{receiverUserId}
Content-Type: application/json

{
  "message": "Hey! Saw you on the map."   // optional
}
```

**Response:**
```json
{
  "succeeded": true,
  "data": {
    "connectionId": "uuid",
    "status": "Pending",
    "chatChannelId": null,
    "bothConnected": false,
    "message": null
  }
}
```

---

### Accept a connection request

```
POST /api/connection/{connectionId}/accept
```

**Response:** `{ "succeeded": true, "data": { "connectionId": "uuid", "status": "Accepted" } }`

---

### Reject a connection request

```
POST /api/connection/{connectionId}/reject
Content-Type: application/json

{
  "message": "Maybe later!"   // optional
}
```

**Response:** `{ "succeeded": true, "data": { "connectionId": "uuid", "status": "Rejected" } }`

---

### Withdraw a connection request (cancel as sender)

```
POST /api/connection/{connectionId}/withdraw
```

**Response:** `{ "succeeded": true, "data": { "connectionId": "uuid", "status": "Withdrawn" } }`

---

### Pass on a connection (after acceptance, decline to chat)

```
POST /api/connection/{connectionId}/pass
Content-Type: application/json

{
  "message": "Good luck!"   // optional
}
```

**Response:** `{ "succeeded": true, "data": { "connectionId": "uuid", "status": "Passed" } }`

---

### Start chat (after connection accepted, both must confirm)

```
POST /api/connection/{connectionId}/start-chat
Content-Type: application/json

{
  "message": "Hey, let's talk!"   // optional opening message
}
```

**Response:**
```json
{
  "succeeded": true,
  "data": {
    "connectionId": "uuid",
    "status": "Accepted",
    "chatChannelId": "connection-uuid",   // populated once BOTH users confirm
    "bothConnected": true,               // true = chat channel is ready
    "message": null
  }
}
```

> When `bothConnected: false`, show a "Waiting for partner to confirm chat..." state.
> When `bothConnected: true`, `chatChannelId` is the Stream Chat channel ID — open the chat screen.

---

## Chat (Stream Chat)

### Get active chat session token

```
GET /api/v1/chat/active
```

**Response:**
```json
{
  "apiKey": "your_stream_api_key",
  "userId": "auth0-abc123",
  "token": "stream_jwt_token"
}
```

> Call this once on app launch. Use `apiKey` + `token` to initialise the Stream Chat SDK.
> **Note:** `userId` is the Stream-safe version of the Auth0 ID (`|` replaced with `-`). Always use this `userId` value (not your raw Auth0 ID) when initialising the Stream Chat SDK.
> The mobile connects directly to Stream Chat servers for messaging — not through this backend.

**Stream Chat SDK setup (React Native):**
```js
import { StreamChat } from 'stream-chat';

const client = StreamChat.getInstance(apiKey);
await client.connectUser({ id: userId }, token);

// Open a channel
const channel = client.channel('messaging', chatChannelId);
await channel.watch();
```

---

## Real-Time Events (SignalR)

### Install

```bash
npm install @microsoft/signalr
```

### Connect

**Important for React Native:** Use `skipNegotiation: true` with `WebSockets` transport. React Native's WebSocket does not go through the negotiate HTTP step, so passing the token in the URL as `access_token` is required.

```js
import * as signalR from '@microsoft/signalr';

let hubConnection = null;

export function createHubConnection(auth0Token) {
  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(
      `http://192.168.1.9:3000/hubs/connection?access_token=${auth0Token}`,
      {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      }
    )
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

  return hubConnection;
}

export async function startHubConnection(auth0Token) {
  const connection = createHubConnection(auth0Token);

  connection.onreconnecting(() => console.log('[SignalR] Reconnecting...'));
  connection.onreconnected(() => console.log('[SignalR] Reconnected'));
  connection.onclose(() => console.log('[SignalR] Connection closed'));

  try {
    await connection.start();
    console.log('[SignalR] Connected');
  } catch (err) {
    console.error('[SignalR] Connection failed:', err);
  }

  return connection;
}
```

> - Call `startHubConnection(token)` after login and keep the reference alive.
> - When Auth0 token refreshes, stop the old connection and create a new one with the fresh token.
> - The `access_token` query param is how the server reads the JWT for WebSocket connections.

**Re-connect on token refresh:**
```js
async function refreshHubConnection(newToken) {
  if (hubConnection) {
    await hubConnection.stop();
  }
  await startHubConnection(newToken);
}
```

---

### Events Reference & UI Behaviour

---

#### `ConnectionRequestReceived`

**When:** Another user sends you a connection request.

**Payload:**
```json
{
  "connectionId": "uuid",
  "senderMaskedName": "J*y",
  "senderAgeRange": "25-30",
  "senderGender": "MALE",
  "message": "Hey! I'd love to connect.",
  "initiatedVia": "Map",
  "expiresAt": "2026-05-04T10:30:00Z"
}
```

**UI to show:**
- **Full-screen modal / bottom sheet** that slides up:
  - Placeholder avatar (gender-based)
  - Masked name + age range
  - Message bubble with their invitation text
  - Timer countdown to `expiresAt`
  - Two buttons: **"Accept"** (green) and **"Reject"** (red/outline)
- Also show a **badge on the Invitations tab** (increment count)
- Play a **soft chime sound**

---

#### `ConnectionRequestSent`

**When:** Your own connection request was successfully queued (confirmation to sender).

**Payload:**
```json
{
  "connectionId": "uuid",
  "receiverMaskedName": "S*r*h",
  "expiresAt": "2026-05-04T10:30:00Z"
}
```

**UI to show:**
- **Toast / snackbar** at bottom: _"Request sent to S\*r\*h — expires in 30 min"_
- On the map, change that user's pin to a "pending" state (dimmed / clock icon)

---

#### `ConnectionAccepted`

**When:** The other user accepted your request (fires for both sender and receiver).

**Payload:**
```json
{
  "connectionId": "uuid",
  "otherUserDisplayName": "Sarah",       // REAL name revealed here
  "otherUserPhotoUrl": "https://...",    // REAL photo revealed here
  "otherUserAgeRange": "22-27",
  "message": "Your connection request was accepted!"
}
```

**UI to show:**
- **Celebration full-screen reveal animation:**
  - Confetti / sparkle effect
  - Real photo fades in from placeholder
  - Real name appears
  - Headline: _"It's a Match! 🎉"_ (or without emoji per preference)
  - Subtext: _"You and Sarah are now connected"_
  - Two buttons: **"Start Chat"** and **"Later"**
- Update connections list to show real name + photo
- Play **match sound effect**

---

#### `ConnectionRejected`

**When:** The receiver rejected your connection request.

**Payload:**
```json
{
  "connectionId": "uuid",
  "message": "Maybe later!"   // optional, may be null
}
```

**UI to show:**
- **Subtle toast**: _"Your request was not accepted this time."_ (do NOT show their name/message to protect privacy)
- Remove from pending invitations list silently

---

#### `ConnectionWithdrawn`

**When:** The sender withdrew their request to you.

**Payload:**
```json
{
  "connectionId": "uuid"
}
```

**UI to show:**
- **Dismiss the pending invitation card** silently (no modal)
- If the invitation modal is currently open, close it with a note: _"Request was withdrawn."_

---

#### `ConnectionPassed`

**When:** After an accepted connection, the partner chose to pass (not start chat).

**Payload:**
```json
{
  "connectionId": "uuid",
  "message": "Good luck!"   // optional
}
```

**UI to show:**
- **Toast**: _"Your match decided to pass. Keep going!"_
- Remove connection card from connections list

---

#### `WaitingForPartner`

**When:** You hit "Start Chat" but your partner hasn't confirmed yet.

**Payload:**
```json
{
  "connectionId": "uuid",
  "message": "Your match wants to start a chat — waiting for your response!"
}
```

**UI to show:**
- **Inline banner on the connection card**: _"[Name] wants to chat! Confirm to open chat."_
- Show a **"Let's Chat!"** action button on that connection
- Send a **push notification** if app is backgrounded: _"[Masked name] wants to chat with you!"_

---

#### `ChatStarted`

**When:** Both users confirmed chat — the Stream Chat channel is ready.

**Payload:**
```json
{
  "connectionId": "uuid",
  "chatChannelId": "connection-uuid",
  "senderConnectionMessage": "Hey, let's talk!",
  "receiverConnectionMessage": "Sure!"
}
```

**UI to show:**
- **Auto-navigate to the chat screen** using `chatChannelId`
- OR show a **"Chat is ready!"** banner with a **"Open Chat"** button (if you prefer not to auto-navigate)
- Initialize Stream Chat channel: `client.channel('messaging', chatChannelId)`
- Play a **message notification sound**

---

#### `TapStatusChanged`

**When:** Your own tap status changes (after tapin/tapout API calls, and when auto-tap-in timer fires).

**Payload:**
```json
{
  "userId": "auth0|abc123",
  "status": "TappedOut",
  "autoTapInAt": "2026-05-04T11:00:00Z",
  "tapOutReason": "In a meeting"
}
```

**UI to show:**
- Update the **Tap button** in the header/nav bar:
  - `TappedIn` → green pulsing dot + **"Tap Out"** button
  - `TappedOut` → grey dot + **"Tap In"** button + countdown timer to `autoTapInAt`
- Show a **brief toast**: _"You're now tapped out until 11:00 AM"_

---

## App Launch Sequence

Run these in order on every app start:

```
1. Auth0 login (if not logged in)
2. POST /api/users/sync           ← create/update user record
3. POST /api/device/register      ← register FCM push token
4. PUT  /api/location             ← send current GPS location
5. GET  /api/dating-profile/me    ← load profile (check if onboarding needed)
6. GET  /api/tap-status/me        ← get current online status
7. GET  /api/v1/chat/active       ← get Stream Chat token
8. Connect to SignalR hub         ← start real-time events
9. GET  /api/discovery            ← load nearby users for map
10. GET /api/spotlight            ← load spotlight feed
```

---

## Error Handling

All API errors follow this shape:

```json
{
  "succeeded": false,
  "data": null,
  "errors": ["Error message here"]
}
```

| HTTP Status | Meaning |
|---|---|
| 200 | Success |
| 400 | Validation error or business rule violation (check `errors[]`) |
| 401 | Token missing or expired — re-authenticate with Auth0 |
| 403 | Insufficient role — user not synced yet |
| 404 | Resource not found |
| 500 | Server error |

> On 401: call Auth0 token refresh, then retry the request once. On 403: call `/api/users/sync` again.

---

## Notes for Developer

- **Masked names:** All user names in Discovery, Spotlight, and Invitations are privacy-masked (e.g. `"J*y P*t*l"`). Real names only reveal in `ConnectionAccepted` event and in accepted connection details.
- **Placeholder photos:** Gender-based placeholder avatars are used everywhere until a connection is accepted and real photos are revealed.
- **Expiry:** Connection requests expire (default 30 min, admin-configurable). Always show a countdown from `expiresAt`.
- **Connection limits:** Backend enforces daily send/receive/connection limits. On `400` from send-request, show the error message from `errors[0]` directly to the user.
- **SignalR reconnect:** Use `.withAutomaticReconnect()`. On reconnect, re-fetch invitations and connections lists as events may have been missed.
