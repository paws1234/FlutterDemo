using Notification.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

app.MapControllers();
// Route fixed by CONTRACT.md (plan §3.2.2).
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
