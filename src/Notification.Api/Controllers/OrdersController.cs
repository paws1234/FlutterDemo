using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Notification.Api.Hubs;
using Notification.Api.Services;

namespace Notification.Api.Controllers;

/// <summary>
/// Receives the REST call that changes an order's status and broadcasts it through
/// the hub context (plan §3.2.3). The hub is injected, never reached statically, and
/// the hub class itself keeps no rules.
/// </summary>
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IOrderStatusService _orders;

    public OrdersController(IHubContext<NotificationHub> hubContext, IOrderStatusService orders)
    {
        _hubContext = hubContext;
        _orders = orders;
    }

    [HttpPost("/{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            // Refused before anything is broadcast (§3.4).
            return BadRequest("Status must be a non-empty string.");
        }

        if (!_orders.TryUpdateStatus(id, status))
        {
            return NotFound();
        }

        // Event name and positional args are CONTRACT.md's.
        await _hubContext.Clients.All.SendAsync("ReceiveOrderStatus", id, status);

        return Ok();
    }
}
