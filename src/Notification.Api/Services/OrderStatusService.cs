using System.Collections.Concurrent;

namespace Notification.Api.Services;

/// <inheritdoc />
public class OrderStatusService : IOrderStatusService
{
    private readonly ConcurrentDictionary<int, string> _statuses = new();

    public OrderStatusService()
    {
        // The orders the split-screen demo talks about (plan §7).
        foreach (var orderId in new[] { 1, 2, 3 })
        {
            _statuses[orderId] = "Preparing";
        }
    }

    public bool TryUpdateStatus(int orderId, string status)
    {
        if (!_statuses.ContainsKey(orderId))
        {
            return false;
        }

        _statuses[orderId] = status;
        return true;
    }
}
