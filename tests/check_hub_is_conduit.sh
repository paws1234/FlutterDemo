#!/bin/sh
# T-1.HUB.01 — the hub must stay a pure conduit: no business rules, no data access.
# Fails if the hub file names a data-access type.
set -e
hub="${1:-src/Notification.Api/Hubs/NotificationHub.cs}"

if grep -nE 'DbContext|EntityFrameworkCore|SqlConnection|IDbConnection|Dapper|IOrderStatusService|Repository' "$hub"; then
  echo "FAIL: $hub references a data-access type"
  exit 1
fi

grep -q 'class NotificationHub : Hub' "$hub" || { echo "FAIL: NotificationHub does not derive from Hub"; exit 1; }
grep -q 'OnConnectedAsync' "$hub" || { echo "FAIL: connect is not logged"; exit 1; }
grep -q 'OnDisconnectedAsync' "$hub" || { echo "FAIL: disconnect is not logged"; exit 1; }

echo "OK: $hub derives from Hub, logs the lifecycle, and names no data-access type"
