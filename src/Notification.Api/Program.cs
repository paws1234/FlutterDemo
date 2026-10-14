using Notification.Api.Hubs;
using Notification.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IOrderStatusService, OrderStatusService>();

var app = builder.Build();

app.MapControllers();
// Route fixed by CONTRACT.md (plan §3.2.2).
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
