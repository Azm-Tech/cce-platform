# SignalR Rooms & Events Reference

## Hub Endpoint

| API | URL | Auth |
|---|---|---|
| External (port 5001) | `/hubs/notifications` | JWT (`?access_token=` query param) |
| Internal (port 5002) | Not mapped (server-side push only) | — |

Dev mode: use `/dev/sign-in` to get a JWT, then pass it as `?access_token=<token>` on the WebSocket connect.

---

## Room / Group Name Patterns

All patterns are defined in `CCE.Application.Common.Realtime.RealtimeGroups`.

| Room Pattern | Method | Auto-join? | Purpose |
|---|---|---|---|
| `"moderation"` | `const` | ✅ if user has `Community_Post_Moderate` permission | Content-moderation events |
| `"user:{userId}"` | `User(string userId)` | ✅ on connect (all authenticated users) | Personal in-app notifications |
| `"post:{postId}"` | `Post(Guid postId)` | ❌ call `Subscribe(postId)` from client | Live reply, vote, poll, presence, typing |
| `"community:{communityId}"` | `Community(Guid communityId)` | ❌ call `SubscribeCommunity(communityId)` | Feed events (new post, moderation) |
| `"topic:{topicId}"` | `Topic(Guid topicId)` | ❌ call `SubscribeTopic(topicId)` | Feed events (new post) |

---

## Hub Methods (Client → Server)

Defined in `NotificationsHub.cs`.

| Method | Arguments | Auth Check | Description |
|---|---|---|---|
| `Subscribe(postId)` | `Guid` | ✅ Community read guard | Join a post's live room |
| `Unsubscribe(postId)` | `Guid` | ❌ | Leave a post's live room |
| `SubscribeCommunity(communityId)` | `Guid` | ✅ Community read guard | Join a community feed room |
| `UnsubscribeCommunity(communityId)` | `Guid` | ❌ | Leave a community feed room |
| `SubscribeTopic(topicId)` | `Guid` | ❌ (auth only) | Join a topic feed room |
| `UnsubscribeTopic(topicId)` | `Guid` | ❌ | Leave a topic feed room |
| `StartTyping(postId)` | `Guid` | ❌ | Broadcast typing indicator |
| `StopTyping(postId)` | `Guid` | ❌ | Stop typing indicator |

---

## Events (Server → Client)

All event names are constants in `CCE.Application.Common.Realtime.RealtimeEvents`.

| Event | Target Room | Payload | Trigger |
|---|---|---|---|
| `ReceiveNotification` | `user:{userId}` | `{ Id, TemplateId, RenderedSubjectAr, RenderedSubjectEn, RenderedBody, RenderedLocale, Status, SentOn }` | In-app notification dispatched |
| `NewReply` | `post:{postId}` | `{ postId, replyId, parentReplyId?, depth }` | `CreateReplyCommandHandler` |
| `VoteChanged` | `post:{postId}` | **Post vote:** `{ postId, upvoteCount, score }`<br/>**Reply vote:** `{ replyId, upvoteCount, score }` | `VotePostCommandHandler` / `VoteReplyCommandHandler` |
| `PollResultsChanged` | `post:{postId}` | `{ pollId, postId }` | `CastPollVoteCommandHandler` |
| `NewPost` | `community:{communityId}` +<br/>`topic:{topicId}` | `{ postId, communityId, topicId, authorId, publishedOn }` | `SignalRConsumer` (Worker — async via bus) |
| `PostModerated` | `post:{postId}` +<br/>`community:{communityId}` | `PostModeratedRealtime { PostId, ReplyId?, Action }` | `SoftDeleteReplyCommandHandler` / `SoftDeletePostCommandHandler` |
| `ContentModerated` | `moderation` | `ContentModeratedRealtime { ContentType, ContentId, PostId, ModeratorId, Action }` | `SoftDeleteReplyCommandHandler` / `SoftDeletePostCommandHandler` |
| `PresenceChanged` | `post:{postId}` | `PresenceChangedRealtime { PostId, Viewers }` | Hub `Subscribe` / `Unsubscribe` / disconnect |
| `TypingChanged` | `post:{postId}` (others only) | `TypingChangedRealtime { PostId, UserId, IsTyping }` | Hub `StartTyping` / `StopTyping` |

---

## Flow Diagram (Text)

```
Client                    External API (5001)          Worker
  │                            │                         │
  │  ──Subscribe(postId)──►   │                         │
  │  ◄──PresenceChanged────    │                         │
  │                            │                         │
  │  ──POST /api/community/    │                         │
  │     posts/{id}/replies──►  │                         │
  │                            │──PublishToPostAsync────►│ (Redis backplane)
  │  ◄──NewReply──────────────│                         │
  │                            │                         │
  │  ──POST /api/community/    │                         │
  │     posts/{id}/vote──────► │                         │
  │  ◄──VoteChanged───────────│                         │
  │                            │                         │
  │  Private posts/           │                         │
  │  communities are          │                         │
  │  access-guarded via       │                         │
  │  ICommunityAccessGuard    │                         │
```

## Testing Notes

- **Dev mode** (`Auth:DevMode=true`): Use `/dev/sign-in` to get a JWT, or set `access_token` cookie. The `TestAuthHandler` accepts any `sub` claim.
- **Redis backplane**: If Redis is down, SignalR degrades to in-process (single instance only). All pushes still work locally.
- **Payloads are minimal**: Clients refetch full DTOs via REST after receiving a realtime event.
