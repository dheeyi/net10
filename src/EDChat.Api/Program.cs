using EDChat.Api.Endpoints;
using EDChat.Api.Handlers;
using EDChat.Api.Hubs;
using EDChat.Api.Middlewares;
using EDChat.Data;
using EDChat.Data.Entities;
using EDChat.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<EDChatDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=edchat.db"));
builder.Services.AddScoped<IRepository<User>, UserRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IRepository<Room>, RoomRepository>();
builder.Services.AddScoped<IRepository<Message>, MessageRepository>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddSingleton<RequestLoggingMiddleware>();
builder.Services.AddValidation();
builder.Services.AddSignalR();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EDChatDb>();
    db.Database.EnsureCreated();
}

app.UseExceptionHandler();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/", () => "Hello World!");

app.MapRoomEndpoints();
app.MapUserEndpoints();
app.MapMessageEndpoints();

app.MapHub<ChatHub>("/chat");

app.Run();
