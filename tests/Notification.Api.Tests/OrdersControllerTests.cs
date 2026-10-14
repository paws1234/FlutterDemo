using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Notification.Api.Controllers;
using Notification.Api.Hubs;
using Notification.Api.Services;

namespace Notification.Api.Tests;

/// <summary>
/// The controller's two halves (plan §10): a valid status produces exactly one
/// broadcast with CONTRACT.md's args, and every refusal produces none. No live hub,
/// no database — the hub context is mocked and the order state is in-memory.
/// </summary>
public class OrdersControllerTests
{
    private static (OrdersController Controller, List<(string Method, object?[] Args)> Sent) Build()
    {
        var sent = new List<(string Method, object?[] Args)>();

        var proxy = new Mock<IClientProxy>();
        proxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((method, args, _) => sent.Add((method, args)))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.SetupGet(c => c.All).Returns(proxy.Object);

        var hubContext = new Mock<IHubContext<NotificationHub>>();
        hubContext.SetupGet(h => h.Clients).Returns(clients.Object);

        return (new OrdersController(hubContext.Object, new OrderStatusService()), sent);
    }

    [Fact]
    public async Task ValidStatus_BroadcastsOnce_WithTheContractArgs()
    {
        var (controller, sent) = Build();

        var result = await controller.UpdateStatus(1, "Out for Delivery");

        Assert.IsType<OkResult>(result);
        var message = Assert.Single(sent);
        Assert.Equal("ReceiveOrderStatus", message.Method);
        Assert.Equal(new object?[] { 1, "Out for Delivery" }, message.Args);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidStatus_IsRefused_AndBroadcastsNothing(string status)
    {
        var (controller, sent) = Build();

        var result = await controller.UpdateStatus(1, status);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(sent);
    }

    [Fact]
    public async Task UnknownOrder_IsRefused_AndBroadcastsNothing()
    {
        var (controller, sent) = Build();

        var result = await controller.UpdateStatus(99, "Preparing");

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(sent);
    }
}
