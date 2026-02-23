using EDChat.Api.Endpoints;
using EDChat.Api.Hubs;
using EDChat.Api.Middlewares;
using EDChat.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ChatStore>();
builder.Services.AddSingleton<RequestLoggingMiddleware>();
builder.Services.AddValidation();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/", () => "Hello World!");

app.MapRoomEndpoints();
app.MapUserEndpoints();
app.MapMessageEndpoints();

app.MapHub<ChatHub>("/chat");

app.Run();
