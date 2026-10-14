using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Notification.Api.Hubs;

namespace Notification.Api.Controllers;

/// <summary>
/// Receives the REST call that changes an order's status and broadcasts it through
/// the hub context (plan §3.2.3). The hub is injected, never reached statically.
/// </summary>
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public OrdersController(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }
}
