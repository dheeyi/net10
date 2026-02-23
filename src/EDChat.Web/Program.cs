using MudBlazor.Services;
using EDChat.Web.Components;
using EDChat.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddScoped<ChatService>();

var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5001";
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiUrl);
});
builder.Services.AddScoped<ApiClient>();

var app = builder.Build();

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
