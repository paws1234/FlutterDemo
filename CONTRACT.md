# Wire contract — `ReceiveOrderStatus`

The single contract both sides implement (plan §5, with §3.3's minimal payload as the
demo shape and §2's communication-flow steps 3–4 as the sequence).

| | |
|---|---|
| **Event name** | `ReceiveOrderStatus` |
| **Hub route** | `/notificationHub` |
| **Server call** | `await hubContext.Clients.All.SendAsync("ReceiveOrderStatus", id, status)` |
| **Client subscription** | `hubConnection.on("ReceiveOrderStatus", (args) { … })` |
| **Wire casing** | camelCase (System.Text.Json defaults) |

## Payload — positional, in this order

| Position | Name | Type | Notes |
|---|---|---|---|
| `args[0]` | `orderId` | `int` | the `{id}` the REST call used |
| `args[1]` | `status` | `string` | non-empty; e.g. `Preparing` → `Out for Delivery` |

The payload is positional, not an object: `args[0]` is the id, `args[1]` is the status.
Both sides take this contract from this file — the backend broadcast and the Flutter
subscription point here rather than restating the names.

## Deliberately not adopted

§3.3's richer `OrderStatusDto(OrderId, Status, UpdatedAt, Message?)` and a
`schemaVersion` field are **not** part of this contract: §1 In Scope keeps the live
demo payload minimal. If the payload ever grows, the version is added here first, in
this file, before either side changes.
