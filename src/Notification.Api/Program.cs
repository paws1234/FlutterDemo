using Notification.Api.Hubs;
using Notification.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IOrderStatusService, OrderStatusService>();

// The allowed origin list is configuration (plan §11), never a literal. "*" is the
// demo-only default: it is read from appsettings.json and must be tightened to the
// real client origins before anything non-local is deployed.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Contains("*"))
    {
        policy.AllowAnyOrigin();
    }
    else
    {
        policy.WithOrigins(allowedOrigins).AllowCredentials();
    }

    policy.AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

// Before the hub and the controllers: negotiate is an HTTP request and is subject
// to CORS (plan §3.4, §6.4).
app.UseCors();

app.MapControllers();
// Route fixed by CONTRACT.md (plan §3.2.2).
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
