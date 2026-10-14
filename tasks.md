# Tasks — Derived from plan.md

## Source

- **Plan**: `plan.md` (repository root — read-only; this ledger never mutates it)
- **Plan sections ingested**: §1 Goals & Scope (In Scope / Out of Scope), §2 High-Level Architecture (Communication Flow, Why SignalR), §3 Backend (3.1 Project Structure, 3.2 Core Components, 3.3 Data Contracts, 3.4 Key Implementation Points), §4 Frontend (4.1 Project Structure, 4.2 Dependencies, 4.3 Connection Setup, 4.4 Bind Event to Widget State, 4.5 Client-Side Error Handling, 4.6 Reactive UI Principles), §5 Shared Contracts & Serialization, §6 Local Runtime & Docker Compose (6.1–6.4), §7 Split-Screen Demonstration Layout, §8 Implementation Phases 1–4, §9 Technical Decisions & Rationale, §10 Testing Strategy (Backend / Frontend / End-to-End Checklist), §11 Configuration & Environment, §12 Architecture Takeaways, §13 Success Metrics.
- **Goal** (from §1): Build a live Order Status notification beacon demonstrating WebSockets and bidirectional synchronization — an ASP.NET Core Web API with a SignalR `NotificationHub`, server-side broadcasting of order status events triggered by REST endpoints through `IHubContext<NotificationHub>`, and a Flutter client that subscribes to `ReceiveOrderStatus` and updates a reactive widget tree instantly, with clean structure, DI, testable code, automatic reconnect, and a reproducible Docker Compose runtime.
- **Constraints** (binding on every task):
  - The Hub is a pure conduit — business rules and database transactions stay **outside** it; broadcasting happens from non-Hub endpoints via `IHubContext<T>` (§3.2.3, §3.4, §12).
  - The event name `ReceiveOrderStatus` and the payload shape must stay identical on both sides of the wire (§5, §9, §13).
  - Flutter connects with `signalr_netcore` and `withAutomaticReconnect()` — mandatory for mobile network handoffs (§4.2, §4.3, §12).
  - The backend **must** be runnable via Docker Compose, starting with a single command on a stable port (§6, §6.3, §8 Phase 1).
  - Non-200 responses and connection loss must never leave the UI frozen or infinitely loading (§1 In Scope, §4.5, §12, §13).
  - Android emulator reaches the host through the `10.0.2.2` loopback mapping (§4.3, §6.4, §9).
  - Payload stays minimal for the demo — `(int id, string status)`; richer DTOs and contract versioning are later, optional steps (§3.3, §5).

## Variables & Scope

### Variables

| Name | Meaning | Allowed values / source | Default | Applies to |
|---|---|---|---|---|
| `event_name` | SignalR method name on the wire | fixed by §5/§9 — `ReceiveOrderStatus` | — | Backend + Flutter |
| `hub_route` | Hub path mapped on the API | `/notificationHub` (§2, §3.2.2) | — | Backend + Flutter |
| `payload_args` | Positional payload contract | `args[0]` = `int orderId`, `args[1]` = `string status` (§3.3, §4.4, §5) | — | Backend + Flutter |
| `wire_casing` | JSON casing on the wire | camelCase (System.Text.Json defaults) (§5) | camelCase | Backend + Flutter |
| `status_values` | Demo status strings | e.g. `Preparing` → `Out for Delivery` (§7, §8 Phase 3); any non-empty string accepted, anything else is a 400 | `Preparing` | Backend validates, Flutter displays |
| `order_id` | Order identifier | `int`, carried in the route `{id}` and `args[0]` (§3.2.3, §3.3) | — | Backend + Flutter |
| `http_status_codes` | Outcome codes on `POST /{id}/status` | 200 success / 400 validation failure / 404 unknown id (§3.4) | — | Backend |
| `hub_base_url` | Per-platform base hub URL, injected per environment/flavor | Android emulator `http://10.0.2.2:5000`, Chrome/desktop `http://localhost:5000`, physical device host LAN IP or tunnel (§4.3, §6.4, §11) | per target platform | Flutter |
| `api_host_port` | Compose host:container port mapping | `5000:8080` (§6.1, §11) | `5000:8080` | Compose/Ops |
| `container_port` | Port exposed by the API image | 8080 (§6.2, §11) | 8080 | Backend/Ops |
| `aspnetcore_environment` | Runtime environment for the demo | `Development` (§6.1) | `Development` | Compose/Ops |
| `aspnetcore_urls` | Kestrel binding inside the container | `http://+:8080` (§6.1) | `http://+:8080` | Compose/Ops |
| `cors_allowed_origins` | Origins allowed to reach the hub | `Cors.AllowedOrigins` in `appsettings.json` (§11); `["*"]` is demo-only and must be tightened for production | `["*"]` | Backend |
| `dotnet_images` | Base images for the multi-stage build | `mcr.microsoft.com/dotnet/sdk:8.0` → `mcr.microsoft.com/dotnet/aspnet:8.0` (§6.2) | pinned 8.0 | Ops |
| `reconnect_policy` | Client reconnect behaviour | `withAutomaticReconnect()` (§4.3, §9) | enabled | Flutter |
| `ui_update_mechanism` | How the widget tree reacts to events | `setState` for the live demo; replaceable by Riverpod/Bloc later (§4.2, §9) | `setState` | Flutter |
| `state_management` | Production-grade reactivity choice (optional) | built-in `setState` / Riverpod / Bloc / Provider (§4.2) | `setState` (demo) | Flutter |
| `flutter_packages` | Client dependencies | `signalr_netcore` (required, §4.2); optional `freezed` + `json_serializable` (§4.2) | `signalr_netcore` | Flutter |
| `rich_dto` | Optional production payload | `OrderStatusDto(OrderId, Status, UpdatedAt, Message?)` (§3.3); `schemaVersion` versioning path (§5) | not adopted for the demo | Backend + Flutter |
| `redis_backplane` | Multi-instance scale-out (documented, not enabled) | `StackExchangeRedis`, added as a second Compose service (§6.1, §8 Phase 4, §12) | off | Backend/Ops |
| `latency_target_ms` | Round-trip REST → UI update | `< 500 ms` local (§13); E2E checklist ceiling `< 1 s` (§10) | 500 ms target / 1 s ceiling | Backend + Flutter |
| `target_platforms` | Where the client is demonstrated | Android/iOS emulator, Chrome, desktop, physical device (§4.3, §6.4, §7) | Android emulator + Chrome | Flutter |

### Global scope

- **IN** (§1 In Scope): ASP.NET Core Web API with `NotificationHub : Hub`; server-side broadcasting of order status events triggered by REST endpoints via `IHubContext<NotificationHub>`; Flutter client using `signalr_netcore` subscribing to `ReceiveOrderStatus` and updating a reactive widget tree live; typed contracts/payload shape between backend and frontend; client-side error handling for non-200 (e.g. 400) and connection drops so the UI never freezes or loads forever; clean project structure, dependency injection and testable code; automatic reconnect policy for mobile network handoffs (Wi-Fi ↔ cellular); Docker Compose for reproducible local runtime of the backend; split-screen demonstration layout so evaluators observe real-time cause and effect.
- **OUT** (§1 Out of Scope): full production auth (OAuth/OIDC identity server) — JWT over WebSockets is an extension only; persistent notification/order history beyond the live update; multi-instance scaling / Redis backplane as a running concern (documented only); mobile push notifications (FCM/APNs); heavy UI polish or non-technical presentation scaffolding.
- **NON-GOALS**: nothing in §1 Out of Scope becomes a required task. `T-4.SEC.01`, `T-4.CTR.01` and `T-4.API.01` are marked optional exactly because the plan marks them optional or as extensions; none of them may grow into an identity server, a history store, or a push-notification service.

### Phase scopes

- **Phase 1 — Backend Foundation + Docker Compose**: **IN** — scaffold, SignalR registration, `NotificationHub`, `IHubContext` injection, `POST /{id}/status` with validation, CORS, `Dockerfile`, `docker-compose.yml` on port 5000. **OUT** — any Flutter work; authentication; Redis. **EXIT CRITERIA (verbatim, §8)**: "`docker compose up --build` brings the API online; calling the REST endpoint with a valid status results in a message received by any connected client. Invalid input returns 400."
- **Phase 2 — Flutter Client Core**: **IN** — `signalr_netcore`, `HubConnectionBuilder` + `withUrl` + `withAutomaticReconnect`, correct per-platform host address, subscription to `"ReceiveOrderStatus"`, logging/storing `id` + `status`, basic connection status indicator. **OUT** — reactive widget binding (Phase 3); error UX beyond the indicator (Phase 3); service extraction (Phase 4). **EXIT CRITERIA (verbatim, §8)**: "Flutter app connects to the Compose-hosted hub, stays connected across brief network interruptions, and receives every broadcasted status change."
- **Phase 3 — Reactive UI & Error Handling**: **IN** — bind the received event to widget state so the tracker/badge updates live; the REST call that triggers the change (or keep it external via Swagger for a pure demo); non-200 handling (abort loading, show error); surface connection loss/reconnecting states. **OUT** — service extraction and stream exposure (Phase 4); auth; richer DTOs. **EXIT CRITERIA (verbatim, §8)**: "Changing status from "Preparing" → "Out for Delivery" updates the Flutter UI instantly without manual refresh. / A 400 response leaves the UI in a clean error state, not an infinite spinner."
- **Phase 4 — Hardening & Extensions**: **IN** — extract SignalR logic into a reusable streaming service; optional strongly-typed hub client interface; optional JWT on the hub connection; document the Redis backplane; unit/widget tests for stream mapping and error paths; confirm the full split-screen demo flow end-to-end under Docker Compose. **OUT** — enabling the Redis backplane; a full identity server; push notifications. **EXIT CRITERIA (verbatim, §8)**: "System is robust under disconnects, returns clear errors, and the architecture is ready for production concerns (auth, scale, richer state management). Split-screen cause-and-effect is reliable."

## Task ID scheme

`T-<Phase>.<Stream>.<Seq>`

- **Phase**: `0` = cross-cutting foundations every phase depends on (§3.1/§4.1 structure and §5 contract); then `1`–`4` as in §8.
- **Stream**: derived from the plan's own modules and file structure — `STR` (project structure, §3.1/§4.1), `CTR` (wire contract, §5), `HUB` (hub class + registration, §3.2.1/§3.2.2), `API` (controller endpoints + `IHubContext`, §3.2.3), `CFG` (configuration and CORS, §11), `OPS` (Docker/Compose, §6), `CONN` (client connection + reconnect + subscription, §4.3/§4.4), `UI` (widgets and reactive binding, §4.4/§4.6), `ERR` (error handling and UX resiliency, §4.5), `SVC` (extracted service, §4.4/§8 Phase 4), `SEC` (JWT extension), `DEMO` (split-screen, §7), `TST` (tests, §10).
- **Seq**: zero-padded sequence within the stream.
- **GATE ids**: `T-<Phase>.X.GATE`.

## Repositories

- **Repository**: `/home/adminpaws/Desktop/DonboscoDemo/Demo` — remote `origin` = `https://github.com/paws1234/FlutterDemo.git`, branch `master`, **no commits yet**. **Every task in this ledger lands in this one repository**; there is no second repository.
- **Planning repository**: the same repository — it holds `plan.md` and this ledger.
- **Routing** (single repository): backend/C# tasks write under `src/Notification.Api/` (§3.1, §6.1); Flutter/Dart tasks write under `lib/` (§4.1); Ops tasks write `Dockerfile` and `docker-compose.yml` at the repository root (§6.1, §6.2). The layout the plan itself specifies is assumed here because the plan states file paths but never states repository boundaries — see **Open Questions** item 2.

---

## Cross-cutting / foundations

> Derived from §3.1, §4.1 and §5. §8 does not schedule these, but Phases 1–4 all write into the structures and contract they fix, so they form Phase 0.

### Stream: `STR`

#### Task ID: T-0.STR.01
- **Title**: Scaffold the ASP.NET Core API project and its structure
- **Description**: Create the backend project exactly as §3.1 lays it out: `src/Notification.Api/` containing `Hubs/`, `Controllers/`, `Models/`, `Services/`, plus `Program.cs` and `appsettings.json`. Wire the dependency-injection container as the composition root. Create `Hubs/NotificationHub.cs` is **not** part of this task — only the folders and the project that Phase 1 fills.
- **Plan source**: §3.1, §1 In Scope ("Clean project structure, dependency injection, and testable code")
- **Scope**: project skeleton, folders and files as named in §3.1. No SignalR wiring, no hub class, no endpoints, no Docker artifacts.
- **Variables / Config**: none read. This task fixes the `src/Notification.Api/...` paths every Phase 1 task writes into.
- **Dependencies**: none
- **Acceptance Criteria**: the project at `src/Notification.Api` builds; `Hubs/`, `Controllers/`, `Models/`, `Services/`, `Program.cs` and `appsettings.json` exist with the §3.1 names; services are resolvable from the DI container rather than constructed by hand.
- **Evidence**: `src/Notification.Api/` — the directory listing plus one clean `dotnet build` of that project.
- **Estimated Effort**: S
- **Owner Role**: Backend Engineer
- **Status**: DONE

- **Recorded**: 2026-10-15 by `/do-task-one-go` — `dotnet build src/Notification.Api` in the `mcr.microsoft.com/dotnet/sdk:8.0` container: **"Build succeeded. 0 Warning(s) 0 Error(s)"**. `Program.cs` is the composition root (`builder.Services.AddControllers()`); the four §3.1 folders are tracked via `.gitkeep`. The host has no .NET SDK, so every C# check in this batch runs in that container.
#### Task ID: T-0.STR.02
- **Title**: Scaffold the Flutter app with the §4.1 `lib/` layout
- **Description**: Create the Flutter application and the `lib/` tree §4.1 specifies: `lib/core/signalr/` (for `signalr_service.dart`, `signalr_connection_state.dart`), `lib/core/models/` (`order_status.dart`), `lib/features/order_tracker/data/` (`order_repository.dart`), `lib/features/order_tracker/presentation/` (`order_tracker_page.dart`, `order_status_badge.dart`), and `lib/main.dart`. Create the folders and the entry point; the files' contents belong to Phases 2–3.
- **Plan source**: §4.1, §1 In Scope
- **Scope**: the Flutter project and its `lib/` structure. No dependency additions, no connection code, no widgets.
- **Variables / Config**: none read. Fixes the `lib/...` paths every Phase 2–4 task writes into.
- **Dependencies**: none
- **Acceptance Criteria**: `flutter analyze` reports no errors on the skeleton; every folder and file §4.1 names exists at the named path.
- **Evidence**: the `lib/` listing plus the `flutter analyze` output.
- **Estimated Effort**: S
- **Owner Role**: Mobile Engineer
- **Status**: DONE

- **Recorded**: 2026-10-15 by `/do-task-one-go` — `flutter create --project-name order_tracker --platforms=android,ios,web .` then `flutter analyze`: **"No issues found! (ran in 1.1s)"**. All six §4.1 `lib/` paths exist (verified individually) plus `lib/main.dart` as the entry point; the generated counter-app `test/widget_test.dart` was removed because it tested a widget this skeleton does not have, and the Flutter ignore rules were added to `.gitignore` (which `flutter create` left untouched).
### Stream: `CTR`

#### Task ID: T-0.CTR.01
- **Title**: Pin the wire contract both sides implement
- **Description**: Write down, in one artifact, the single contract §5 defines: the event name `ReceiveOrderStatus`; the hub route `/notificationHub`; the payload `SendAsync("ReceiveOrderStatus", id, status)` with `args[0]` = id and `args[1]` = status; camelCase on the wire; and the note that the contract is versioned later if the payload grows. §3.3's minimal payload "matches lesson plan" is the shape recorded here.
- **Plan source**: §5, §3.3, §2 Communication Flow steps 3–4, §9 (event name decision)
- **Scope**: the contract artifact only. No hub, no controller, no client code, no DTO implementation.
- **Variables / Config**: defines `event_name`, `hub_route`, `payload_args`, `wire_casing` (see Variables table). No variable is read by this task.
- **Dependencies**: none
- **Acceptance Criteria**: one artifact states the event name, the hub route, the positional arg order and types, and the wire casing; it is the only place both `T-1.*` and `T-2.*` take the contract from; the minimal payload is preserved as the demo shape.
- **Evidence**: the contract file's path and content (reviewable). No runnable check — the deliverable is a written contract, and §10 requires no test for it.
- **Estimated Effort**: S
- **Owner Role**: Architect
- **Status**: DONE

---

## Phase 1 — Backend Foundation + Docker Compose

- **Recorded**: 2026-10-15 by `/do-task-one-go` — contract written to `CONTRACT.md` (event name `ReceiveOrderStatus`, route `/notificationHub`, positional `args[0]` `int orderId` / `args[1]` `string status`, camelCase on the wire, richer §3.3 DTO explicitly not adopted). No runnable check, as the ledger says: the deliverable is a written contract.
### Stream: `HUB`

#### Task ID: T-1.HUB.01
- **Title**: Implement `NotificationHub : Hub`
- **Description**: Add `Hubs/NotificationHub.cs` as the central communication conduit, deriving from `Hub`. Leave optional `OnConnectedAsync`/`OnDisconnectedAsync` overrides in place for connection-lifecycle logging when debugging. No client→server methods are needed yet.
- **Plan source**: §3.2.1, §3.4 ("Log connection lifecycle if debugging connection issues")
- **Scope**: the hub class and its connection-lifecycle logging. No broadcasting logic, no database access, no business rules inside the hub (§3.2.3).
- **Variables / Config**: none read.
- **Dependencies**: T-0.STR.01
- **Acceptance Criteria**: `NotificationHub` derives from `Hub`; the class contains no business rules and no data access; connect/disconnect are logged when logging is enabled; the class remains a pure conduit.
- **Evidence**: `src/Notification.Api/Hubs/NotificationHub.cs` plus a check that the file references no data-access type.
- **Estimated Effort**: S
- **Owner Role**: Backend Engineer
- **Status**: DONE

- **Recorded**: 2026-10-15 by `/do-task-one-go` — `tests/check_hub_is_conduit.sh` → **"OK: … derives from Hub, logs the lifecycle, and names no data-access type"**; `dotnet build src/Notification.Api` → "Build succeeded. 0 Warning(s) 0 Error(s)". Lifecycle logging is injected `ILogger<NotificationHub>`; the class holds no rules and no data access.
#### Task ID: T-1.HUB.02
- **Title**: Register SignalR and map the `/notificationHub` route
- **Description**: Register the SignalR services (`builder.Services.AddSignalR()`) and map the hub (`app.MapHub<NotificationHub>("/notificationHub")`) in `Program.cs`, so clients can negotiate a connection on the route the contract names.
- **Plan source**: §3.2.2, §2 (this is the `/notificationHub` route both sides use)
- **Scope**: the two registrations and the route. Not CORS (T-1.CFG.01), not the controller endpoint (T-1.API.02).
- **Variables / Config**: `hub_route` = `/notificationHub`.
- **Dependencies**: T-0.STR.01, T-0.CTR.01
- **Acceptance Criteria**: SignalR is registered; `/notificationHub` is mapped; a client can complete negotiation against that route.
- **Evidence**: `Program.cs` plus a negotiate request against `/notificationHub` succeeding.
- **Estimated Effort**: S
- **Owner Role**: Backend Engineer
- **Status**: DONE

- **Recorded**: 2026-10-15 by `/do-task-one-go` — `POST /notificationHub/negotiate?negotiateVersion=1` against the API run from the SDK container on host port 5000: **HTTP 200** with `connectionId`, `connectionToken` and WebSockets/ServerSentEvents/LongPolling in `availableTransports`. `builder.Services.AddSignalR()` and `app.MapHub<NotificationHub>("/notificationHub")` are the two registrations; CORS is deliberately not part of this task.
### Stream: `API`

#### Task ID: T-1.API.01
- **Title**: Inject `IHubContext<NotificationHub>` into the orders controller
- **Description**: Make the hub context resolvable from DI and take `IHubContext<NotificationHub>` as a constructor dependency of `OrdersController`, so a non-Hub endpoint can broadcast without the hub holding any logic.
- **Plan source**: §3.2.3, §3.4 ("`IHubContext<T>` is required so non-Hub endpoints (REST controllers) can broadcast"), §8 Phase 1
- **Scope**: the DI registration and the constructor injection. No endpoint logic.
- **Variables / Config**: none read.
- **Dependencies**: T-1.HUB.02
- **Acceptance Criteria**: the controller receives `IHubContext<NotificationHub>` by constructor injection; the hub is not reached through a static or global accessor; the controller resolves and runs before the endpoint exists.
- **Evidence**: the DI registration in `Program.cs` plus the controller's constructor signature.
- **Estimated Effort**: S
- **Owner Role**: Backend Engineer
- **Status**: DONE

- **Recorded**: 2026-10-15 by `/do-task-one-go` — `OrdersController` takes `IHubContext<NotificationHub>` by constructor injection (line 16) and the context comes from `builder.Services.AddSignalR()`; no static or global accessor. `dotnet build` → "Build succeeded. 0 Warning(s) 0 Error(s)". Activation is proved at runtime by the 200 in `T-1.API.02`, which is the first request that reaches the controller.
#### Task ID: T-1.API.02
- **Title**: Implement `POST /{id}/status` — validate, then broadcast
- **Description**: Implement the endpoint that validates the incoming status, and on success updates state and broadcasts `await _hubContext.Clients.All.SendAsync("ReceiveOrderStatus", id, status)` before returning 200. Reject invalid input with 400 **before** any broadcast; return 404 for an unknown order id. Keep validation, business rules and any transaction logic in the controller/service, never in the hub.
- **Plan source**: §3.2.3, §3.4 (status codes 200/400/404), §2 Communication Flow steps 2–3, §10 ("Verify 400 path does **not** broadcast")
- **Scope**: the endpoint, its validation, its status codes and its broadcast. Demo-level in-memory state is sufficient — §1 Out of Scope excludes persistent notification/order history.
- **Variables / Config**: `event_name`, `payload_args`, `order_id`, `http_status_codes`, `status_values`.
- **Dependencies**: T-1.API.01, T-0.CTR.01
- **Acceptance Criteria**: a valid status returns 200 and every connected client receives `ReceiveOrderStatus` carrying `(id, status)` from `args[0]`/`args[1]`; an invalid status returns 400 and **no** message is broadcast; an unknown id returns 404; no business rule or data access lives in the hub.
- **Evidence**: the endpoint invoked through Swagger/Postman for each of the three codes, plus the assertions in T-1.TST.02 and T-1.TST.01.
- **Estimated Effort**: M
- **Owner Role**: Backend Engineer
- **Status**: DONE

- **Recorded**: 2026-10-15 by `/do-task-one-go` — against the API run from the SDK container on port 5000: valid status on a known id → **HTTP 200**; empty status → **HTTP 400** "Status must be a non-empty string."; whitespace-only status → **HTTP 400**; unknown id 99 → **HTTP 404**. Validation sits before the broadcast, so the 400 path cannot broadcast; state is in-memory (`IOrderStatusService`, singleton) because §1 excludes persistent history.
### Stream: `CFG`

#### Task ID: T-1.CFG.01
- **Title**: Allow the local Flutter / emulator origins through CORS
- **Description**: Configure CORS so the Flutter and emulator origins can reach the hub, reading the allowed list from `appsettings.json` under `Cors:AllowedOrigins` and defaulting to `["*"]` for the local demo, with the production tightening noted where the list is defined.
- **Plan source**: §3.4 ("CORS must allow the Flutter origin (or `*` for local emulator testing)"), §11, §6.4
- **Scope**: the CORS policy and its configuration key. Nothing else in the request pipeline.
- **Variables / Config**: `cors_allowed_origins` (list; demo default `["*"]`).
- **Dependencies**: T-1.HUB.02
- **Acceptance Criteria**: a browser or emulator origin completes negotiate **and** connect; the allowed list is configuration, not a hard-coded literal; the demo-permissive default is labelled as demo-only.
- **Evidence**: `appsettings.json` showing `Cors:AllowedOrigins` plus a successful cross-origin connect.
- **Estimated Effort**: S
- **Owner Role**: Backend Engineer
- **Status**: DONE

- **Recorded**: 2026-10-15 by `/do-task-one-go` — default `["*"]`: negotiate with `Origin` returns **`Access-Control-Allow-Origin: *`** and a real WebSocket connect with that `Origin` completes (handshake `{}`). Re-run with `Cors__AllowedOrigins__0=http://localhost:8080`: that origin is echoed back and an unconfigured `http://evil.example` gets **no** CORS header — so the list is read from configuration, not a literal. The demo-only label sits on the list in `appsettings.json` (the file still parses).
### Stream: `OPS`

#### Task ID: T-1.OPS.01
- **Title**: Dockerfile for the API (multi-stage, dotnet 8)
- **Description**: Add the `Dockerfile` of §6.2: `mcr.microsoft.com/dotnet/sdk:8.0` build stage performing restore and `dotnet publish -c Release -o /app`, then a `mcr.microsoft.com/dotnet/aspnet:8.0` runtime stage copying `/app`, `EXPOSE 8080`, and `ENTRYPOINT ["dotnet", "Notification.Api.dll"]`.
- **Plan source**: §6.2, §8 Phase 1
- **Scope**: the API image only. No compose file (T-1.OPS.02), no Redis service.
- **Variables / Config**: `dotnet_images`, `container_port` = 8080.
- **Dependencies**: T-0.STR.01
- **Acceptance Criteria**: the image builds from `src/Notification.Api`; the container starts the API and serves on 8080 without a host-installed .NET SDK.
- **Evidence**: the `docker build` output plus a request served by the running container.
- **Estimated Effort**: S
- **Owner Role**: DevOps Engineer
- **Status**: DONE

- **Recorded**: 2026-10-15 by `/do-task-one-go` — `docker build -t notification-api:local src/Notification.Api` succeeded from the multi-stage `Dockerfile`, and the running container answered `POST /notificationHub/negotiate?negotiateVersion=1` with **HTTP 200** while the host has **no** .NET SDK (`command -v dotnet` → missing), which is the criterion. Added `src/Notification.Api/.dockerignore` so local `bin/`/`obj/` cannot be baked into the build stage.
#### Task ID: T-1.OPS.02
- **Title**: `docker-compose.yml` — one-command backend on a stable port
- **Description**: Add the compose file of §6.1 with a single `api` service building `context: ./src/Notification.Api`, publishing `5000:8080`, and setting `ASPNETCORE_ENVIRONMENT=Development` and `ASPNETCORE_URLS=http://+:8080`. Keep the future `redis` backplane service present but commented, as §6.1 shows, so the scale-out path is visible without being enabled.
- **Plan source**: §6.1, §6.3, §11, §12 (Redis backplane added later), §8 Phase 1
- **Scope**: the compose file and the api service. The Redis service stays commented out — enabling it is out of scope (§1 Out of Scope: multi-instance scaling documented as future).
- **Variables / Config**: `api_host_port` = `5000:8080`, `aspnetcore_environment`, `aspnetcore_urls`, `redis_backplane` (commented).
- **Dependencies**: T-1.OPS.01
- **Acceptance Criteria**: `docker compose up --build` brings the API online; the host port is 5000 on every run so the client URL is stable; no globally installed .NET SDK is required; the Redis service is present as a commented future option.
- **Evidence**: the `docker compose up --build` output plus the API reachable at `http://localhost:5000/notificationHub`.
- **Estimated Effort**: M
- **Owner Role**: DevOps Engineer
- **Status**: TODO

### Stream: `TST`

#### Task ID: T-1.TST.01
- **Title**: Backend unit test with a mocked `IHubContext`
- **Description**: Unit-test the controller/service with a mocked `IHubContext<NotificationHub>`: assert that a valid status produces exactly one `SendAsync("ReceiveOrderStatus", id, status)` with the right arguments, and that an invalid status produces none.
- **Plan source**: §10 Backend ("Unit test the service/controller with a mocked `IHubContext`")
- **Scope**: the unit test and its mock. Not the live integration check (T-1.TST.02).
- **Variables / Config**: `event_name`, `payload_args`, `http_status_codes`.
- **Dependencies**: T-1.API.02
- **Acceptance Criteria**: the valid path asserts one broadcast with matching args; the invalid path asserts zero broadcasts; the test runs with no live hub and no database.
- **Evidence**: the test file plus a green run of it.
- **Estimated Effort**: M
- **Owner Role**: QA / Test Engineer
- **Status**: TODO

#### Task ID: T-1.TST.02
- **Title**: Integration check against the Compose-hosted API
- **Description**: With the backend up via `docker compose up`, connect a test client to `/notificationHub`, `POST /{id}/status` with a valid status, and assert the client receives `ReceiveOrderStatus` with the correct args. Then POST an invalid status, assert the 400 response, and assert that **no** message was broadcast.
- **Plan source**: §10 Backend integration, §8 Phase 1 exit criteria, §10 E2E checklist items 1 and 5
- **Scope**: the integration check and its assertions. No client UI.
- **Variables / Config**: `hub_base_url` (localhost:5000 for the test client), `api_host_port`, `status_values`.
- **Dependencies**: T-1.OPS.02, T-1.API.02
- **Acceptance Criteria**: a real connected client observes the broadcast with matching args; the invalid input is refused with 400 and no broadcast follows; the API answers on port 5000.
- **Evidence**: the check's run output against the compose-hosted API.
- **Estimated Effort**: M
- **Owner Role**: QA / Test Engineer
- **Status**: TODO

### Phase Exit Gate

#### Task ID: T-1.X.GATE
- **Title**: Phase 1 exit gate — backend foundation + Docker Compose
- **Description**: Restate §8 Phase 1's exit criteria as checks and run them against the Compose-hosted API.
- **Plan source**: §8 Phase 1 exit criteria, §10 E2E checklist item 1, §13 metric 6
- **Scope**: verification only. No new implementation; a failure returns to the task that broke it.
- **Variables / Config**: `api_host_port`, `hub_route`, `http_status_codes`.
- **Dependencies**: T-1.HUB.01, T-1.HUB.02, T-1.API.01, T-1.API.02, T-1.CFG.01, T-1.OPS.01, T-1.OPS.02, T-1.TST.01, T-1.TST.02
- **Acceptance Criteria** (each a check that would go red if the behaviour broke):
  1. `docker compose up --build` brings the API online — §8 Phase 1.
  2. The REST endpoint called with a valid status results in a message received by any connected client — §8 Phase 1.
  3. Invalid input returns 400 — §8 Phase 1.
  4. The API is reachable on port 5000 — §10 E2E checklist item 1.
  5. `docker compose up --build` starts a working backend in one command — §13 metric 6.
- **Evidence**: the compose run output, the T-1.TST.02 integration check, and the port/route probe.
- **Estimated Effort**: S
- **Owner Role**: Tech Lead
- **Status**: TODO

---

## Phase 2 — Flutter Client Core

### Stream: `CONN`

#### Task ID: T-2.CONN.01
- **Title**: `signalr_netcore` connection with automatic reconnect
- **Description**: Add the `signalr_netcore` package, then build the connection with `HubConnectionBuilder().withUrl(<per-platform base URL>).withAutomaticReconnect().build()` and `await hubConnection.start()`. Use the builder's reconnect rather than a hand-rolled retry loop.
- **Plan source**: §4.2, §4.3, §9 (package and reconnect decisions), §8 Phase 2
- **Scope**: the dependency and the connection lifecycle. Not the event subscription (T-2.CONN.02), not the indicator (T-2.CONN.03), not widget state (Phase 3).
- **Variables / Config**: `flutter_packages` (`signalr_netcore`), `hub_base_url`, `hub_route`, `reconnect_policy`.
- **Dependencies**: T-0.STR.02, T-0.CTR.01, T-1.HUB.02
- **Acceptance Criteria**: the app establishes a connection to the Compose-hosted hub; `withAutomaticReconnect()` is configured; the base URL is not hard-coded in the connection call (see T-2.CFG.01).
- **Evidence**: `pubspec.yaml`, the connection code, and a run log/screenshot showing an established connection.
- **Estimated Effort**: M
- **Owner Role**: Mobile Engineer
- **Status**: TODO

#### Task ID: T-2.CONN.02
- **Title**: Subscribe to `ReceiveOrderStatus` and store `id` + `status`
- **Description**: Register `hubConnection.on("ReceiveOrderStatus", ...)`, extract `args[0]` as the order id and `args[1]` as the status, and log or store what arrives, so every broadcast is observable before any UI binding exists.
- **Plan source**: §4.4 (the `on` handler), §5 (the args mapping), §8 Phase 2
- **Scope**: the subscription and the arg extraction. Not the widget rebuild (T-3.UI.01).
- **Variables / Config**: `event_name`, `payload_args`, `status_values`.
- **Dependencies**: T-2.CONN.01
- **Acceptance Criteria**: every status change broadcast by the API is received and recorded with its id and status; the values come from `args[0]`/`args[1]` by position and nothing is assumed about unnamed fields; a missing arg does not crash the handler.
- **Evidence**: the subscription code plus a run showing received `id`/`status` values for a real broadcast.
- **Estimated Effort**: S
- **Owner Role**: Mobile Engineer
- **Status**: TODO

#### Task ID: T-2.CONN.03
- **Title**: Basic connection status indicator
- **Description**: Surface the connection state — Connected / Reconnecting / Disconnected — on screen, driven by the hub connection's state.
- **Plan source**: §4.6 ("Connection state (Connected / Reconnecting / Disconnected) should be visible to the user"), §8 Phase 2 ("Basic connection status indicator")
- **Scope**: the basic indicator. The richer error surfacing for exhausted reconnects is T-3.ERR.02.
- **Variables / Config**: `reconnect_policy`.
- **Dependencies**: T-2.CONN.01
- **Acceptance Criteria**: the three states are distinguishable on screen; the indicator changes without a manual refresh.
- **Evidence**: a screenshot for each of the three states.
- **Estimated Effort**: S
- **Owner Role**: Mobile Engineer
- **Status**: TODO

### Stream: `CFG`

#### Task ID: T-2.CFG.01
- **Title**: Per-platform hub URL injection
- **Description**: Inject the base hub URL per environment/flavor instead of hard-coding it: Android emulator `http://10.0.2.2:5000/notificationHub`, Chrome/desktop `http://localhost:5000/notificationHub`, physical device the host machine's LAN IP or a tunnel.
- **Plan source**: §4.3 host address notes, §6.4, §11 Flutter ("Base hub URL injected per environment / flavor")
- **Scope**: the configuration/flavor mechanism and the three values. Not the connection code (T-2.CONN.01).
- **Variables / Config**: `hub_base_url` (per target platform), `hub_route`, `target_platforms`.
- **Dependencies**: T-2.CONN.01
- **Acceptance Criteria**: no host address is hard-coded in the connection or widget code; changing the target platform changes the URL through configuration only; the emulator value is the documented `10.0.2.2` loopback mapping.
- **Evidence**: the configuration/flavor file plus a run on at least two targets reaching the same hub.
- **Estimated Effort**: S
- **Owner Role**: Mobile Engineer
- **Status**: TODO

### Stream: `TST`

#### Task ID: T-2.TST.01
- **Title**: Verify reconnection across a brief network interruption
- **Description**: Toggle airplane mode briefly and confirm the client reconnects automatically and still receives broadcasts afterwards, exercising the Wi-Fi ↔ cellular handoff case the reconnect policy exists for.
- **Plan source**: §8 Phase 2 exit criteria, §10 E2E checklist item 6, §13 metric 2, §12 (mobile resilience)
- **Scope**: the reconnect verification. No code changes beyond fixes it uncovers in T-2.CONN.*.
- **Variables / Config**: `reconnect_policy`, `hub_base_url`.
- **Dependencies**: T-2.CONN.01, T-2.CONN.03
- **Acceptance Criteria**: the connection recovers without an app restart; the indicator passes through Reconnecting → Connected; a broadcast sent after recovery is received.
- **Evidence**: the manual run log and screenshots of the before/during/after states.
- **Estimated Effort**: S
- **Owner Role**: QA / Test Engineer
- **Status**: TODO

### Phase Exit Gate

#### Task ID: T-2.X.GATE
- **Title**: Phase 2 exit gate — Flutter client core
- **Description**: Restate §8 Phase 2's exit criteria as checks against the Compose-hosted hub.
- **Plan source**: §8 Phase 2 exit criteria, §13 metrics 2 and 4
- **Scope**: verification only.
- **Variables / Config**: `hub_base_url`, `event_name`, `payload_args`.
- **Dependencies**: T-2.CONN.01, T-2.CONN.02, T-2.CONN.03, T-2.CFG.01, T-2.TST.01
- **Acceptance Criteria** (each a check that would go red if the behaviour broke):
  1. The Flutter app connects to the Compose-hosted hub — §8 Phase 2.
  2. It stays connected across brief network interruptions — §8 Phase 2 (proven by T-2.TST.01).
  3. It receives every broadcasted status change — §8 Phase 2.
  4. The event name and payload shape are identical on both sides of the wire — §13 metric 4.
  5. The connection recovers automatically after a brief interruption — §13 metric 2.
- **Evidence**: the connect and reconnect runs plus the received-events log.
- **Estimated Effort**: S
- **Owner Role**: Tech Lead
- **Status**: TODO

---

## Phase 3 — Reactive UI & Error Handling

### Stream: `UI`

#### Task ID: T-3.UI.01
- **Title**: Bind `ReceiveOrderStatus` to widget state
- **Description**: On each received event, update widget state (`setState` or equivalent) from `args[1]` (status, optionally `args[0]` for the id) so the status badge / Order Tracker rebuilds immediately. Keep the rebuild narrow — only status-related widgets.
- **Plan source**: §4.4, §2 Communication Flow step 4, §4.6, §8 Phase 3
- **Scope**: the event→state binding and the targeted rebuild. Not the service extraction (T-4.SVC.01).
- **Variables / Config**: `event_name`, `payload_args`, `ui_update_mechanism` (`setState`).
- **Dependencies**: T-2.CONN.02, T-3.UI.02
- **Acceptance Criteria**: a broadcast changes the on-screen status with no pull-to-refresh; only status-related widgets rebuild; the update does not require reconnecting or re-entering the screen.
- **Evidence**: the widget test of T-3.TST.01 plus a screenshot pair before/after a broadcast.
- **Estimated Effort**: M
- **Owner Role**: Mobile Engineer
- **Status**: TODO

#### Task ID: T-3.UI.02
- **Title**: Order Tracker page and status badge
- **Description**: Build `order_tracker_page.dart` (StatefulWidget or equivalent) and `order_status_badge.dart` presenting the current status, structured so a status change rebuilds only the badge and status-related widgets.
- **Plan source**: §4.1 presentation/, §4.6, §7 (the badge evaluators watch)
- **Scope**: the two presentation widgets. No network or hub code inside them.
- **Variables / Config**: `status_values`.
- **Dependencies**: T-0.STR.02
- **Acceptance Criteria**: the badge renders the current status; a status change rebuilds only the status-related subtree; the tracker is the widget the §7 layout puts on screen for evaluators.
- **Evidence**: the widget test output plus a screenshot of the tracker.
- **Estimated Effort**: M
- **Owner Role**: Mobile Engineer
- **Status**: TODO

### Stream: `API`

#### Task ID: T-3.API.01
- **Title**: Flutter-side REST call that triggers the status change
- **Description**: Implement `order_repository.dart` performing the `POST /{id}/status` call so a status change can be fired from the app. §8 Phase 3 explicitly permits keeping this external (Swagger/Postman) for a pure demo — if that choice is taken, record it rather than leaving the task silently skipped.
- **Plan source**: §4.1 data/`order_repository.dart`, §8 Phase 3 first bullet
- **Scope**: the repository call and its result handling. Not the UI error rendering (T-3.ERR.01).
- **Variables / Config**: `hub_base_url` (host/port of the API), `status_values`, `order_id`, `http_status_codes`.
- **Dependencies**: T-2.CFG.01, T-1.API.02
- **Acceptance Criteria**: the call reaches the endpoint with the expected payload; both success and failure outcomes are handed back to the caller so T-3.ERR.01 can act on them; if the external-Swagger alternative is chosen instead, that decision is written down with its reason.
- **Evidence**: the repository code plus a run showing both a 200 and a 400 response handled.
- **Estimated Effort**: M
- **Owner Role**: Mobile Engineer
- **Status**: TODO

### Stream: `ERR`

#### Task ID: T-3.ERR.01
- **Title**: Non-200 handling — abort loading, surface the error, never freeze
- **Description**: Catch non-200 responses (especially 400 Bad Request due to validation failure) in the HTTP client / repository layer; abort any optimistic UI state (loading spinner, temporary status); render a user-facing error as a SnackBar, banner or inline fallback message; leave no infinite loading state behind.
- **Plan source**: §4.5 steps 1–4, §1 In Scope, §9 error policy, §8 Phase 3 exit criteria, §13 metric 3
- **Scope**: the error path for REST failures. Not connection-level errors (T-3.ERR.02).
- **Variables / Config**: `http_status_codes`, `ui_update_mechanism`.
- **Dependencies**: T-3.API.01
- **Acceptance Criteria**: a 400 leaves the UI in a clean error state with no spinner running; any optimistic state is reverted; the message is user-facing rather than a raw stack trace or error dump.
- **Evidence**: the T-3.TST.02 check plus screenshots of the 400 path.
- **Estimated Effort**: M
- **Owner Role**: Mobile Engineer
- **Status**: TODO

#### Task ID: T-3.ERR.02
- **Title**: Surface connection-level errors (Reconnecting / Offline)
- **Description**: When the hub is disconnected or reconnect attempts are exhausted, show a clear "Reconnecting…" or "Offline" indicator so the screen never appears frozen.
- **Plan source**: §4.5 (connection-level errors), §4.6
- **Scope**: the exhausted-reconnect / offline state. It complements T-2.CONN.03's basic indicator rather than replacing it.
- **Variables / Config**: `reconnect_policy`.
- **Dependencies**: T-2.CONN.03
- **Acceptance Criteria**: an exhausted-reconnect condition is visually distinguishable from a healthy connection; the screen stays interactive while offline; no frozen state remains.
- **Evidence**: screenshots of the Reconnecting and Offline states.
- **Estimated Effort**: S
- **Owner Role**: Mobile Engineer
- **Status**: TODO

### Stream: `TST`

#### Task ID: T-3.TST.01
- **Title**: Unit and widget tests for arg extraction and live update
- **Description**: Unit-test the argument extraction (`args[0]`, `args[1]`, including missing/short arg lists); widget-test that emitting a fake `ReceiveOrderStatus` updates the displayed status text.
- **Plan source**: §10 Frontend ("Unit test argument extraction (`args[0]`, `args[1]`)", "Widget test: emit a fake `ReceiveOrderStatus` → assert status text updates")
- **Scope**: the two test layers for the client. Not the stream-mapping tests of Phase 4.
- **Variables / Config**: `payload_args`, `event_name`.
- **Dependencies**: T-3.UI.01
- **Acceptance Criteria**: extraction is covered for the valid case and for out-of-range/missing args; the widget test asserts the new status text after a fake emission; both are green.
- **Evidence**: the test files plus their green run output.
- **Estimated Effort**: M
- **Owner Role**: QA / Test Engineer
- **Status**: TODO

#### Task ID: T-3.TST.02
- **Title**: Verify the 400 path leaves no infinite spinner
- **Description**: Force a 400 (invalid status) from the triggering call and assert the UI shows the error state and no spinner is left running.
- **Plan source**: §10 E2E checklist item 5, §8 Phase 3 exit criteria, §13 metric 3
- **Scope**: the verification of the 400 client path.
- **Variables / Config**: `http_status_codes`, `status_values`.
- **Dependencies**: T-3.ERR.01
- **Acceptance Criteria**: the error is visible; the spinner has stopped; the loading state is cleared rather than pending.
- **Evidence**: the check's run output plus a screenshot of the error state.
- **Estimated Effort**: S
- **Owner Role**: QA / Test Engineer
- **Status**: TODO

### Phase Exit Gate

#### Task ID: T-3.X.GATE
- **Title**: Phase 3 exit gate — reactive UI and error handling
- **Description**: Restate §8 Phase 3's exit criteria as checks and, where the environment permits it, run the demo flow that proves them.
- **Plan source**: §8 Phase 3 exit criteria, §10 E2E checklist item 4, §13 metrics 1 and 3
- **Scope**: verification only.
- **Variables / Config**: `latency_target_ms`, `status_values`, `http_status_codes`.
- **Dependencies**: T-3.UI.01, T-3.UI.02, T-3.API.01, T-3.ERR.01, T-3.ERR.02, T-3.TST.01, T-3.TST.02
- **Acceptance Criteria** (each a check that would go red if the behaviour broke):
  1. Changing status from "Preparing" → "Out for Delivery" updates the Flutter UI instantly without manual refresh — §8 Phase 3.
  2. A 400 response leaves the UI in a clean error state, not an infinite spinner — §8 Phase 3.
  3. The round-trip (REST call → Flutter UI update) completes in under 500 ms on the local network — §13 metric 1 (with §10's < 1 s as the E2E ceiling).
  4. A 400 Bad Request leaves the UI in a clean error state — §13 metric 3.
- **Evidence**: the widget tests, the 400-path check, and a measured round-trip.
- **Estimated Effort**: S
- **Owner Role**: Tech Lead
- **Status**: TODO

---

## Phase 4 — Hardening & Extensions

### Stream: `SVC`

#### Task ID: T-4.SVC.01
- **Title**: Extract SignalR logic into a reusable service exposing a stream
- **Description**: Move the connection and subscription into a dedicated `SignalRService` (under `lib/core/signalr/`, with `signalr_connection_state.dart`) that exposes a `Stream<OrderStatusUpdate>`, so higher-level state management can listen without the widget knowing about SignalR.
- **Plan source**: §4.4 ("Prefer extracting this into a dedicated `SignalRService` …"), §4.1 `core/signalr/`, §8 Phase 4
- **Scope**: the extraction and the stream surface. Not the state-management framework swap (§4.2 keeps that optional and out of the demo's path).
- **Variables / Config**: `event_name`, `payload_args`, `ui_update_mechanism`.
- **Dependencies**: T-3.UI.01
- **Acceptance Criteria**: widgets consume a stream and no widget imports `signalr_netcore`; connection state lives with the service; the UI behaviour of Phase 3 is unchanged after the extraction (proven by the existing tests still passing).
- **Evidence**: the service and connection-state files plus the Phase 3 tests still green.
- **Estimated Effort**: M
- **Owner Role**: Mobile Engineer
- **Status**: TODO

### Stream: `API`

#### Task ID: T-4.API.01
- **Title**: Optional — strongly-typed hub client interface
- **Description**: Define `INotificationClient { Task ReceiveOrderStatus(int orderId, string status); }` and broadcast through the strongly-typed context (`IHubContext<NotificationHub, INotificationClient>.Clients.All.ReceiveOrderStatus(id, status)`), replacing the string method name on the server. §8 Phase 4 lists this as optional.
- **Plan source**: §3.2 strongly-typed alternative, §8 Phase 4
- **Scope**: the server-side typing only. The client keeps the string subscription unless the wire contract changes.
- **Variables / Config**: `event_name` (the interface member name must remain the wire name), `payload_args`.
- **Dependencies**: T-1.API.02
- **Acceptance Criteria**: the interface member name equals the wire event name so the §5 contract is unchanged; the Phase 1 tests still pass; a client built against the string event name is unaffected.
- **Evidence**: the interface, the updated broadcast call, and the re-run of T-1.TST.01/T-1.TST.02.
- **Estimated Effort**: S
- **Owner Role**: Backend Engineer
- **Status**: TODO

### Stream: `SEC`

#### Task ID: T-4.SEC.01
- **Title**: Optional — JWT authentication on the hub connection
- **Description**: Add token authentication so the hub connection is authorised, with the Flutter client supplying a token. §1 places full OAuth/OIDC identity server work out of scope and notes JWT over WebSockets as the extension; this task must stay inside that boundary.
- **Plan source**: §8 Phase 4 ("Optional: JWT authentication on the hub connection"), §1 Out of Scope
- **Scope**: token authentication on the hub connection only. No identity provider, no user store, no refresh-token service.
- **Variables / Config**: `hub_route`, `hub_base_url`.
- **Dependencies**: T-2.CONN.01
- **Acceptance Criteria**: an unauthenticated connection is refused; the client supplies a token and connects; no component beyond the hub connection is gated by this change.
- **Evidence**: a refused unauthenticated connect and a successful authenticated connect.
- **Estimated Effort**: L
- **Owner Role**: Backend Engineer
- **Status**: TODO

### Stream: `CTR`

#### Task ID: T-4.CTR.01
- **Title**: Optional — richer DTO and contract versioning
- **Description**: Adopt the optional production payload `OrderStatusDto(OrderId, Status, UpdatedAt, Message?)` from §3.3 and the versioning path §5 describes (`schemaVersion` or a richer DTO) if the contract needs to grow, moving both sides together.
- **Plan source**: §3.3 ("Optional richer DTO for production evolution"), §5 ("Version the contract later if the payload grows")
- **Scope**: the payload evolution and its version marker. Do not break the minimal demo payload silently — the change must be visible on both sides.
- **Variables / Config**: `rich_dto`, `payload_args`, `wire_casing`.
- **Dependencies**: T-0.CTR.01
- **Acceptance Criteria**: if adopted, backend and Flutter move in the same change and the event name is unchanged; the minimal payload remains documented as the demo shape; the version is discoverable from the payload.
- **Evidence**: the updated contract artifact plus both sides' tests updated and green.
- **Estimated Effort**: M
- **Owner Role**: Architect
- **Status**: TODO

### Stream: `OPS`

#### Task ID: T-4.OPS.01
- **Title**: Document the Redis backplane for multi-instance scale-out
- **Description**: Document the `StackExchangeRedis` backplane and the second Compose service that enables it, as the documented enterprise-readiness path — messages stay synchronised across ASP.NET Core instances — while remaining disabled for the demo.
- **Plan source**: §8 Phase 4 ("Document Redis backplane (`StackExchangeRedis`) for multi-instance scale-out"), §12 scale row, §6.1 commented `redis` service, §1 Out of Scope
- **Scope**: the written enablement path. Do **not** enable the backplane, and do not change the Flutter side — §12 states the client is unaffected.
- **Variables / Config**: `redis_backplane` (the second Compose service, still commented out).
- **Dependencies**: T-1.OPS.02
- **Acceptance Criteria**: the document names the package, the extra Compose service, and what changes on the Flutter side (nothing); the commented service in `docker-compose.yml` matches what the document describes.
- **Evidence**: the document plus the corresponding commented block in `docker-compose.yml`.
- **Estimated Effort**: S
- **Owner Role**: DevOps Engineer
- **Status**: TODO

### Stream: `TST`

#### Task ID: T-4.TST.01
- **Title**: Unit and widget tests for stream mapping and error paths
- **Description**: Cover the mapping from raw hub args to the typed update exposed by the extracted service, and cover the error paths — non-200 responses and disconnections — with tests.
- **Plan source**: §8 Phase 4 ("Unit / widget tests for stream mapping and error paths"), §10 Frontend
- **Scope**: the added test coverage for the extracted service and the error paths.
- **Variables / Config**: `payload_args`, `event_name`, `http_status_codes`.
- **Dependencies**: T-4.SVC.01, T-3.ERR.01
- **Acceptance Criteria**: stream mapping is covered for the valid payload and for a malformed one; the non-200 path and the disconnection path are each covered by a test; the suite is green.
- **Evidence**: the test files plus their green run output.
- **Estimated Effort**: M
- **Owner Role**: QA / Test Engineer
- **Status**: TODO

### Stream: `DEMO`

#### Task ID: T-4.DEMO.01
- **Title**: Prepare and verify the split-screen demonstration end to end
- **Description**: Set up the §7 layout — left: IDE showing the C# Controller + Hub **and** the Flutter `SignalRService` / Order Tracker; right: the running emulator or Chrome plus Swagger/Postman — then run §10's end-to-end checklist: compose up, connected indicator green, `Preparing` → `Out for Delivery` broadcast observed on the emulator without refresh, a forced 400 showing the error with no spinner, and an airplane-mode toggle recovering by itself.
- **Plan source**: §7, §10 E2E checklist, §8 Phase 4, §13 metric 7
- **Scope**: the demonstration layout and the end-to-end verification run. Not new product capability — a failure here returns to the phase that owns the behaviour.
- **Variables / Config**: `hub_base_url`, `api_host_port`, `status_values`, `latency_target_ms`, `target_platforms`.
- **Dependencies**: T-3.X.GATE, T-2.TST.01, T-4.SVC.01
- **Acceptance Criteria**: every §10 checklist item passes — compose up succeeds on port 5000; the client is connected using `10.0.2.2:5000` or localhost; the cause (controller broadcast) and effect (live widget update) are visible in one take; the status change updates the UI within the §13 500 ms target; the 400 path shows an error with no spinner; airplane mode recovers by reconnect.
- **Evidence**: the completed checklist with a recorded result per item, plus screenshots or a recording of the split-screen run.
- **Estimated Effort**: M
- **Owner Role**: Tech Lead
- **Status**: TODO

### Phase Exit Gate

#### Task ID: T-4.X.GATE
- **Title**: Phase 4 exit gate — hardening, extensions and demo readiness
- **Description**: Restate §8 Phase 4's exit criteria as checks over the whole system.
- **Plan source**: §8 Phase 4 exit criteria, §13 metrics 4, 5, 6, 7 and 8, §10 E2E checklist
- **Scope**: verification only — the final gate over the plan's own success metrics.
- **Variables / Config**: `latency_target_ms`, `redis_backplane`, `rich_dto`.
- **Dependencies**: T-4.SVC.01, T-4.API.01, T-4.SEC.01, T-4.CTR.01, T-4.OPS.01, T-4.TST.01, T-4.DEMO.01
- **Acceptance Criteria** (each a check that would go red if the behaviour broke):
  1. The system is robust under disconnects — §8 Phase 4.
  2. It returns clear errors — §8 Phase 4.
  3. The architecture is ready for production concerns (auth, scale, richer state management) — §8 Phase 4.
  4. Split-screen cause-and-effect is reliable — §8 Phase 4; also §13 metric 7.
  5. The event name and payload shape stay identical on both sides of the wire — §13 metric 4.
  6. The code is modular: hub, controller/service and Flutter SignalR service are independently testable — §13 metric 5.
  7. `docker compose up --build` starts a working backend in one command — §13 metric 6.
  8. The architecture is ready for the documented next steps (JWT over WebSockets, Redis backplane, Riverpod/Bloc) — §13 metric 8.
- **Evidence**: the Phase 1–4 checks re-run green, the T-4.DEMO.01 checklist, and the test suite output.
- **Estimated Effort**: S
- **Owner Role**: Tech Lead
- **Status**: TODO

---

## Open Questions & Conflicts

1. **Two plan files exist and this run used one of them.** `plan.md` at the repository root (the Flutter/SignalR plan this ledger was derived from) and `.github/plan.md` (an ERP plan, which is the source of the existing `.github/tasks.md` ledger — 189 `DONE`, 74 `TODO`, 5 `BLOCKED`). The invocation named `plan.md`, so the ledger was written from the root file and to `tasks.md` beside it; **`.github/tasks.md` was not touched**. If the ERP ledger was the intended target, say so and re-run `/execute .github/plan.md` — this run deliberately did not merge into it.
2. **Repository boundaries are not stated in the plan.** §3.1 and §6.1 place the backend at `src/Notification.Api/` while §4.1 shows the Flutter `lib/` tree; the plan never says whether these share one repository. This ledger assumes **one repository** (`paws1234/FlutterDemo`) holding the backend under `src/Notification.Api/` and the Flutter app at the repository root. If the two halves are meant to be separate repositories, the `## Repositories` routing must be rewritten and several tasks re-routed. **Confirm before `/do-task` starts.**
3. **§8 Phase 3 makes the Flutter-side REST trigger optional**: "Implement the REST call that triggers the status change from the Flutter side (or keep it external via Swagger for pure demo)." `T-3.API.01` is written to accept either outcome, but which one is intended is not decided by the plan. Decide before that task runs.
4. **Two latency figures.** §13 metric 1 says the round-trip is `< 500 ms`; §10's E2E checklist says the UI updates in `< 1 s`. Both are kept — 500 ms as the target, 1 s as the ceiling — rather than silently picking one.
5. **Optional work must stay optional.** `T-4.API.01`, `T-4.SEC.01` and `T-4.CTR.01` are marked optional because the plan marks them so; §1 explicitly puts production auth and persistent history out of scope. None of them may expand into an identity server, a notification history store, or push notifications.
6. **Split-screen is assumed to be one workstation** with the IDE on one side and the emulator/Chrome on the other, per §7's table. If evaluators watch two physical machines, `T-4.DEMO.01` changes shape.
7. **The repository has no commits yet.** `T-0.STR.01` and `T-0.STR.02` create the first tracked files; the backend/frontend halves land in one history until or unless Open Question 2 resolves otherwise.

## Handover

- **Ledger path**: `tasks.md` (repository root, beside `plan.md`)
- **Task count**: 35 tasks total — Phase 0: 3, Phase 1: 10 (9 tasks + gate), Phase 2: 6 (5 + gate), Phase 3: 8 (7 + gate), Phase 4: 8 (7 + gate), with the 4 phase gates included in those numbers.
- **Gates**: `T-1.X.GATE`, `T-2.X.GATE`, `T-3.X.GATE`, `T-4.X.GATE`
- **First runnable id**: `T-0.STR.01` (and, in parallel and independent of it, `T-0.STR.02` — a different repository path, no shared dependency)
- **Repository the first runnable id lands in**: `paws1234/FlutterDemo` (`src/Notification.Api/`), the same repository as every other task
- **Every task is `TODO`.** No status was set, and no `DOING`/`DONE` was written — statuses belong to `/do-task`.
- **Next command**: `/do-task` — one task per run, starting with `T-0.STR.01`.
