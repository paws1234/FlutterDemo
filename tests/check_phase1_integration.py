#!/usr/bin/env python3
"""T-1.TST.02 - the Phase 1 integration check, run against the compose-hosted API.

Bring the stack up first (the T-1.OPS.02 check does exactly this):

    docker compose up --build -d

Then run this file. It connects a real SignalR client to /notificationHub over the
JSON protocol and asserts, against the live API:

  1. the API answers on host port 5000
  2. a valid status reaches the connected client as `ReceiveOrderStatus` with the
     args CONTRACT.md fixes
  3. an invalid status is refused with 400 and nothing is broadcast
  4. an unknown order id is refused with 404 and nothing is broadcast

Exit status is non-zero if any assertion fails.
"""

import asyncio
import json
import sys
import urllib.error
import urllib.request

import websockets

BASE = "http://localhost:5000"
HUB = f"{BASE}/notificationHub"
HUB_WS = HUB.replace("http://", "ws://")
ORIGIN = "http://localhost:8080"  # a browser / emulator origin, so CORS is exercised
RECORD_SEPARATOR = "\x1e"
INVOCATION = 1

failures = []


def check(label, ok, detail=""):
    print(f"{'PASS' if ok else 'FAIL'}: {label}{(' - ' + detail) if detail else ''}")
    if not ok:
        failures.append(label)


def negotiate():
    request = urllib.request.Request(
        f"{HUB}/negotiate?negotiateVersion=1", method="POST", headers={"Origin": ORIGIN}
    )
    with urllib.request.urlopen(request, timeout=10) as response:
        return json.loads(response.read())["connectionToken"]


def post_status(order_id, status):
    body = json.dumps(status).encode()
    request = urllib.request.Request(
        f"{BASE}/{order_id}/status",
        data=body,
        method="POST",
        headers={"Content-Type": "application/json", "Origin": ORIGIN},
    )
    try:
        with urllib.request.urlopen(request, timeout=10) as response:
            return response.status, response.read().decode()
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode()


async def collect_invocations(connection, seconds):
    """Everything the server invokes in the next `seconds`, ignoring keep-alive pings."""
    seen = []
    loop = asyncio.get_running_loop()
    deadline = loop.time() + seconds
    while True:
        remaining = deadline - loop.time()
        if remaining <= 0:
            return seen
        try:
            frame = await asyncio.wait_for(connection.recv(), timeout=remaining)
        except asyncio.TimeoutError:
            return seen
        for record in frame.split(RECORD_SEPARATOR):
            if not record.strip():
                continue
            message = json.loads(record)
            if message.get("type") == INVOCATION:
                seen.append((message.get("target"), message.get("arguments")))


async def main():
    token = negotiate()
    check("the API negotiates on host port 5000", bool(token))

    async with websockets.connect(
        f"{HUB_WS}?id={token}", additional_headers={"Origin": ORIGIN}
    ) as connection:
        await connection.send(json.dumps({"protocol": "json", "version": 1}) + RECORD_SEPARATOR)
        handshake = await asyncio.wait_for(connection.recv(), timeout=10)
        check("the client is connected", handshake.startswith("{}"), repr(handshake))

        code, _ = post_status(1, "Out for Delivery")
        check("a valid status is accepted (200)", code == 200, f"HTTP {code}")
        broadcasts = await collect_invocations(connection, 5)
        check("the connected client receives exactly one broadcast", len(broadcasts) == 1, f"{broadcasts}")
        if broadcasts:
            target, arguments = broadcasts[0]
            check("the event name is ReceiveOrderStatus", target == "ReceiveOrderStatus", str(target))
            check(
                "the args are [1, 'Out for Delivery']",
                arguments == [1, "Out for Delivery"],
                str(arguments),
            )

        code, body = post_status(1, "")
        check("an invalid status is refused (400)", code == 400, f"HTTP {code} {body.strip()[:60]}")
        check("the 400 path broadcasts nothing", await collect_invocations(connection, 2) == [])

        code, _ = post_status(999, "Preparing")
        check("an unknown order id is refused (404)", code == 404, f"HTTP {code}")
        check("the 404 path broadcasts nothing", await collect_invocations(connection, 2) == [])


asyncio.run(main())
print()
if failures:
    print(f"FAILED: {len(failures)} assertion(s): {', '.join(failures)}")
    sys.exit(1)
print("OK: every Phase 1 integration assertion held")
