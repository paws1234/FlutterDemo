# Plan: Real-Time Order Status Updates with ASP.NET Core SignalR + Flutter
**Stack**: ASP.NET Core (C#) + SignalR | Flutter (Dart) + signalr_netcore  
**Scenario**: Live Order Status / Notification Beacon  
**Core Focus**: WebSockets & Bidirectional Synchronization  
**Focus**: Technical architecture, coding structure, and implementation roadmap

---

## 1. Goals & Scope

### In Scope
- ASP.NET Core Web API with a SignalR Hub (`NotificationHub : Hub`)
- Server-side broadcasting of order status events triggered by REST endpoints via `IHubContext<NotificationHub>`
- Flutter client that connects to the hub using `signalr_netcore`, subscribes to `ReceiveOrderStatus`, and updates a reactive widget tree live (`setState` or equivalent state management)
- Typed contracts / payload shape between backend and frontend
- Client-side error handling for non-200 responses (e.g. 400 Bad Request) and connection drops to avoid infinite loading / frozen UI
- Clean project structure, dependency injection, and testable code
- Automatic reconnect policy for mobile network handoffs (Wi-Fi ↔ cellular)
- Docker Compose for reproducible local runtime of the backend (and optional supporting services)
- Split-screen demonstration layout so evaluators observe real-time cause and effect

### Out of Scope
- Full production auth (OAuth/OIDC identity server) — JWT over WebSockets is noted as extension
- Persistent notification / order history storage beyond the live update
- Multi-instance scaling / Redis backplane (documented as future enterprise readiness; can be added later as a Compose service)
- Mobile push notifications (FCM/APNs)
- Heavy UI polish or non-technical presentation scaffolding

---

## 2. High-Level Architecture

```
┌─────────────────────┐         REST + SignalR          ┌──────────────────────┐
│  Flutter App        │ ◄─────────────────────────────► │  ASP.NET Core API    │
│  - signalr_netcore  │                                 │  - NotificationHub   │
│  - Order Tracker UI │                                 │  - Controllers       │
│  - Reactive State   │                                 │  - IHubContext       │
└─────────────────────┘                                 └──────────────────────┘
```

### Communication Flow
1. Flutter connects to `/notificationHub` via WebSocket (SignalR protocol) with automatic reconnect.
2. A REST endpoint `POST /{id}/status` (or equivalent) is called with a new order status.
3. Controller injects `IHubContext<NotificationHub>` and calls `Clients.All.SendAsync("ReceiveOrderStatus", id, status)`.
4. Connected Flutter clients receive the event, extract payload args, and call `setState` (or notify listeners) → status badge / Order Tracker widget rebuilds instantly.
5. On 400 Bad Request or network drop, Flutter aborts any optimistic loading state and surfaces a user-facing error (snackbar / fallback UI).

### Why SignalR over HTTP Polling
- Persistent connection eliminates repeated HTTP request/response header overhead.
- Server pushes events only when state changes → lower bandwidth, fewer radio wake-ups, better battery life on mobile.
- Scales better for many concurrent clients watching the same order or status stream.

---

## 3. Backend (ASP.NET Core)

### 3.1 Project Structure
```
src/
  Notification.Api/
    Hubs/
      NotificationHub.cs
    Controllers/
      OrdersController.cs          // or NotificationsController
    Models/
      OrderStatusUpdate.cs
    Services/
      IOrderStatusService.cs
      OrderStatusService.cs
    Program.cs
    appsettings.json
```

### 3.2 Core Components

#### 1. Create Hub Class
```csharp
public class NotificationHub : Hub
{
    // Hub acts as the central communication conduit.
    // SignalR automatically manages client connections over
    // WebSockets, Server-Sent Events, or Long Polling.
    // Optional: override OnConnectedAsync / OnDisconnectedAsync
    // Optional: Client → Server methods if needed later.
}
```

#### 2. Register in Program.cs
```csharp
builder.Services.AddSignalR();
// ... other services

var app = builder.Build();
// ... middleware

app.MapHub<NotificationHub>("/notificationHub");
```

#### 3. Inject Hub Context & Broadcast from Controller
```csharp
[HttpPost("{id}/status")]
public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
{
    // Validation / database update logic here...
    // If validation fails → return BadRequest(...) so Flutter can react

    await _hubContext.Clients.All.SendAsync("ReceiveOrderStatus", id, status);
    return Ok();
}
```

- Inject `IHubContext<NotificationHub>` (or the strongly-typed variant) into the controller or a dedicated service.
- Prefer keeping database transactions and business rules **outside** the Hub class; use the Hub only as the push mechanism.
- Event name on the wire: `"ReceiveOrderStatus"`.
- Payload: `(int id, string status)` — keep it simple and match what Flutter expects in `args`.

#### Strongly-Typed Alternative (Recommended for larger codebases)
```csharp
public interface INotificationClient
{
    Task ReceiveOrderStatus(int orderId, string status);
}

// Then register and inject IHubContext<NotificationHub, INotificationClient>
// Call: await _hubContext.Clients.All.ReceiveOrderStatus(id, status);
```

### 3.3 Data Contracts
Keep the live-demo payload minimal (matches lesson plan):

```csharp
// Wire payload for ReceiveOrderStatus
// args[0] = orderId (int)
// args[1] = status  (string)   e.g. "Preparing" → "Out for Delivery"
```

Optional richer DTO for production evolution:
```csharp
public record OrderStatusDto(
    int OrderId,
    string Status,
    DateTime UpdatedAt,
    string? Message = null
);
```

### 3.4 Key Implementation Points
- `IHubContext<T>` is required so non-Hub endpoints (REST controllers) can broadcast.
- CORS must allow the Flutter origin (or `*` for local emulator testing).
- Return proper HTTP status codes (200 / 400 / 404) so the Flutter side can distinguish success from validation failure.
- Log connection lifecycle if debugging connection issues.

---

## 4. Frontend (Flutter)

### 4.1 Project Structure
```
lib/
  core/
    signalr/
      signalr_service.dart
      signalr_connection_state.dart
    models/
      order_status.dart
  features/
    order_tracker/
      data/
        order_repository.dart
      presentation/
        order_tracker_page.dart      // StatefulWidget or ConsumerWidget
        order_status_badge.dart
  main.dart
```

### 4.2 Dependencies
- `signalr_netcore` (primary package used in the lesson plan)
- State management: built-in `setState` for the live demo; Riverpod / Bloc / Provider for production-grade reactivity
- Optional: `freezed` + `json_serializable` if moving to richer DTOs

### 4.3 SignalR Connection Setup
```dart
final hubConnection = HubConnectionBuilder()
    .withUrl("http://10.0.2.2:5000/notificationHub")  // Android Emulator → host loopback
    .withAutomaticReconnect()
    .build();

await hubConnection.start();
```

**Host address notes**:
- Android Emulator: `10.0.2.2` maps to the host machine’s localhost.
- iOS Simulator / physical device: use the machine’s LAN IP or a tunnel.
- Always prefer HTTPS + proper certificate handling in non-local environments.

**Automatic reconnect**:
- `withAutomaticReconnect()` handles transient network drops and Wi-Fi ↔ cellular handoffs.
- Critical for mobile UX; without it the client stays disconnected after the first interruption.

### 4.4 Bind Event to Widget State
```dart
hubConnection.on("ReceiveOrderStatus", (args) {
  setState(() {
    orderStatus = args?[1]?.toString() ?? "";
    // optionally also capture orderId = args?[0]
  });
});
```

- Receiving the payload triggers a targeted rebuild of the status badge / Order Tracker widget tree.
- Prefer extracting this into a dedicated `SignalRService` that exposes a `Stream<OrderStatusUpdate>` so higher-level state management (Riverpod, Bloc, etc.) can listen without the widget knowing about SignalR.

### 4.5 Client-Side Error Handling & UX Resiliency (Affective Objective)
When the REST call that triggers the status change returns a non-200 response (especially 400 Bad Request due to validation failure):

1. Catch the error in the Flutter HTTP client / repository layer.
2. Abort any optimistic UI state (loading spinner, temporary status).
3. Render a user-facing error (SnackBar, banner, or inline fallback message).
4. Do **not** leave the UI in an infinite loading state.

Connection-level errors (hub disconnected, reconnect attempts exhausted) should also surface a clear “Reconnecting…” or “Offline” indicator rather than a frozen screen.

### 4.6 Reactive UI Principles
- Live status badge updates without requiring the user to pull-to-refresh.
- Keep the widget tree update as narrow as possible (only the status-related widgets).
- Connection state (Connected / Reconnecting / Disconnected) should be visible to the user.

---

## 5. Shared Contracts & Serialization

| Side     | Event Name            | Payload Shape                          |
|----------|-----------------------|----------------------------------------|
| Backend  | `ReceiveOrderStatus`  | `SendAsync("ReceiveOrderStatus", id, status)` |
| Flutter  | `on("ReceiveOrderStatus", ...)` | `args[0]` = id, `args[1]` = status    |

- Field names and types must stay in sync.
- Prefer camelCase on the wire if using System.Text.Json defaults.
- Version the contract later if the payload grows (`schemaVersion` or a richer DTO).

---

## 6. Local Runtime & Docker Compose

The backend **must** be runnable via Docker Compose so the demonstration environment is reproducible, starts with a single command, and exposes stable ports for the Flutter emulator / Chrome.

### 6.1 Recommended `docker-compose.yml` (minimal)
```yaml
services:
  api:
    build:
      context: ./src/Notification.Api
      dockerfile: Dockerfile
    ports:
      - "5000:8080"          # host:container (adjust to match launchSettings / Kestrel)
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
    # Optional later:
    # depends_on:
    #   - redis
    # volumes for hot-reload during live coding can be added if desired

  # Optional future service for scale-out demo
  # redis:
  #   image: redis:7-alpine
  #   ports:
  #     - "6379:6379"
```

### 6.2 Dockerfile (API)
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Notification.Api.dll"]
```

### 6.3 Why Docker Compose for this demo
- One command (`docker compose up --build`) brings the SignalR backend online.
- Guarantees the same port mapping every time → Flutter can reliably use `http://10.0.2.2:5000/notificationHub`.
- Isolates the .NET runtime from the host machine’s global SDK state.
- Makes it trivial to later add Redis (backplane) as another service without changing the Flutter side.
- Keeps the live-coding focus on Hub + Controller + Flutter service instead of fighting local IIS / Kestrel configuration.

### 6.4 Flutter connection under Compose
- Android Emulator → `http://10.0.2.2:5000/notificationHub`
- Chrome / desktop → `http://localhost:5000/notificationHub`
- Physical device → host machine LAN IP (or use a tunnel)

CORS on the API must allow the Flutter origin (or be temporarily permissive for the demo).

---

## 7. Split-Screen Demonstration Layout

Because this is a live teaching demonstration, the runtime setup must support **real-time cause-and-effect** visibility for evaluators.

### Recommended Screen Layout
| Side | Content |
|------|---------|
| **Left** | VS Code / Visual Studio — C# backend (Controller + Hub) **and** Flutter `SignalRService` / Order Tracker code side-by-side or in split editor |
| **Right** | Active Android / iOS emulator **or** Chrome instance running the Flutter app + Swagger UI / Postman in a browser tab |

### Execution Flow Visible to Evaluators
1. Backend is already running via `docker compose up`.
2. Flutter app is connected (connection indicator visible).
3. Instructor fires a `POST /{id}/status` (Swagger or Postman) changing status from `"Preparing"` → `"Out for Delivery"`.
4. Evaluators instantly see the Order Tracker / status badge update on the emulator **without** any manual refresh.
5. Optional: deliberately send an invalid status to trigger the 400 path and show the error SnackBar / fallback state.

This layout keeps the pedagogical focus on the technical cause (controller broadcast) → effect (reactive widget rebuild) while the Docker Compose layer guarantees a stable backend for the entire session.

---

## 8. Implementation Phases

### Phase 1 – Backend Foundation + Docker Compose
- Scaffold ASP.NET Core Web API project (pre-baked baseline: EF Core models, Swagger, etc. can already exist).
- Add SignalR package and configure `AddSignalR()` + `MapHub<NotificationHub>("/notificationHub")`.
- Implement empty `NotificationHub : Hub`.
- Inject `IHubContext<NotificationHub>` into the Orders controller.
- Implement `POST /{id}/status` that validates input, updates state, then broadcasts `ReceiveOrderStatus`.
- Enable CORS for local Flutter / emulator origins.
- Add `Dockerfile` and `docker-compose.yml` so the API starts with a single command and exposes a stable port (5000).
- Verify with Swagger / Postman + a simple JS or .NET test client against the Compose-hosted API.

**Exit Criteria**: `docker compose up --build` brings the API online; calling the REST endpoint with a valid status results in a message received by any connected client. Invalid input returns 400.

### Phase 2 – Flutter Client Core
- Add `signalr_netcore` dependency.
- Implement connection using `HubConnectionBuilder` + `withUrl` + `withAutomaticReconnect`.
- Use the correct host address for the target platform (`10.0.2.2:5000` for Android emulator when API is in Compose).
- Subscribe to `"ReceiveOrderStatus"` and log / store the received `id` + `status`.
- Basic connection status indicator.

**Exit Criteria**: Flutter app connects to the Compose-hosted hub, stays connected across brief network interruptions, and receives every broadcasted status change.

### Phase 3 – Reactive UI & Error Handling
- Bind the received event to widget state (`setState` or equivalent) so the Order Tracker / status badge updates live.
- Implement the REST call that triggers the status change from the Flutter side (or keep it external via Swagger for pure demo).
- Handle non-200 responses: abort loading state, show error SnackBar / fallback.
- Surface connection loss / reconnecting states in the UI.

**Exit Criteria**: 
- Changing status from “Preparing” → “Out for Delivery” updates the Flutter UI instantly without manual refresh.
- A 400 response leaves the UI in a clean error state, not an infinite spinner.

### Phase 4 – Hardening & Extensions
- Extract SignalR logic into a reusable service that exposes streams.
- Optional: strongly-typed hub client interface on the backend.
- Optional: JWT authentication on the hub connection.
- Document Redis backplane (`StackExchangeRedis`) for multi-instance scale-out (can be added as a second Compose service).
- Unit / widget tests for stream mapping and error paths.
- Confirm the full split-screen demo flow works end-to-end under Docker Compose.

**Exit Criteria**: System is robust under disconnects, returns clear errors, and the architecture is ready for production concerns (auth, scale, richer state management). Split-screen cause-and-effect is reliable.

---

## 9. Technical Decisions & Rationale

| Decision                        | Choice                                      | Reason |
|--------------------------------|---------------------------------------------|--------|
| Hub vs Controller broadcast    | `IHubContext` injected into controller      | Keeps business logic & DB transactions out of the Hub; Hub stays a pure conduit |
| Event name                     | `ReceiveOrderStatus`                        | Matches the lesson-plan contract exactly |
| Flutter package                | `signalr_netcore`                           | Protocol-compatible with ASP.NET Core SignalR |
| Reconnect policy               | `withAutomaticReconnect()`                  | Essential for mobile Wi-Fi/cellular handoffs |
| Host for Android emulator      | `10.0.2.2`                                  | Standard loopback mapping to host machine |
| UI update mechanism (demo)     | `setState`                                  | Fastest path for the live coding window; replaceable by Riverpod/Bloc later |
| Error policy                   | Catch non-200 → abort loading → show error  | Prevents frozen / infinite-loading UX |
| Local runtime                  | Docker Compose                              | Reproducible backend, stable ports, one-command start for the live demo |
| Demo visibility                | Split-screen (IDE + Emulator/Chrome)        | Evaluators see controller broadcast → live widget update in real time |

---

## 10. Testing Strategy

### Backend
- Unit test the service/controller with a mocked `IHubContext`.
- Integration: `docker compose up` → connect a test client → POST status → assert `ReceiveOrderStatus` received with correct args.
- Verify 400 path does **not** broadcast.

### Frontend
- Unit test argument extraction (`args[0]`, `args[1]`).
- Widget test: emit a fake `ReceiveOrderStatus` → assert status text updates.
- Manual: Android emulator + Compose-hosted backend; change status via Swagger/Postman and watch the badge update.

### End-to-End Checklist (Split-Screen Ready)
- `docker compose up --build` succeeds and API is reachable on port 5000.
- Flutter app connected (connection indicator green) using `10.0.2.2:5000` (or localhost for Chrome).
- Left screen shows IDE (Controller + Flutter service); right screen shows emulator + Swagger/Postman.
- POST status change “Preparing” → “Out for Delivery” → UI updates in real time (< 1 s).
- Force a 400 (invalid status) → UI shows error, no spinner left spinning.
- Toggle airplane mode briefly → automatic reconnect recovers.

---

## 11. Configuration & Environment

### Backend (Docker Compose)
```yaml
# docker-compose.yml (see Section 6)
ports:
  - "5000:8080"
```
```json
{
  "Cors": {
    "AllowedOrigins": [ "*" ]   // tighten for production
  }
}
```
- Prefer running exclusively through Compose during the demonstration so port and environment are identical every time.

### Flutter
- Base hub URL injected per environment / flavor.
- Local Android (API in Compose): `http://10.0.2.2:5000/notificationHub`
- Local Chrome / desktop: `http://localhost:5000/notificationHub`
- Physical device: host machine LAN IP (or tunnel)

---

## 12. Architecture Takeaways (from Lesson Plan)

| Dimension                | Key Technical Point |
|--------------------------|---------------------|
| Bandwidth & Efficiency   | WebSockets remove recurring HTTP header overhead → lower mobile data use and fewer radio wake-ups. |
| Scale & Enterprise       | Multiple ASP.NET Core instances require a Redis backplane (`StackExchangeRedis`) so messages are synchronized across nodes. Can be added later as a Compose service. |
| Separation of Concerns   | Inject `IHubContext<T>` into controllers/services instead of putting all logic inside the Hub class. |
| Mobile Resilience        | `withAutomaticReconnect()` is mandatory for real-world network handoffs. |
| UX Resilience            | Always handle non-success HTTP responses and connection state so the UI never freezes on a loading spinner. |
| Demo Reproducibility     | Docker Compose + split-screen layout guarantees evaluators see live cause-and-effect every time. |

---

## 13. Success Metrics

- Round-trip (REST call → Flutter UI update) < 500 ms on local network.
- Connection recovers automatically after brief network interruption.
- 400 Bad Request leaves the UI in a clean error state (no infinite loading).
- Event name and payload shape stay identical on both sides of the wire.
- Code is modular: Hub, controller/service, and Flutter SignalR service are independently testable.
- `docker compose up --build` starts a working backend in one command.
- Split-screen demonstration reliably shows controller action → live UI update for evaluators.
- Architecture is ready for the documented next steps (JWT over WebSockets, Redis backplane, Riverpod/Bloc).
