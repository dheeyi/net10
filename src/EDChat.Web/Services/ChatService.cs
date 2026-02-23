using Microsoft.AspNetCore.SignalR.Client;
using EDChat.Web.Models;

namespace EDChat.Web.Services;

public class ChatService : IAsyncDisposable
{
    private HubConnection? _connection;

    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    public event Action<MessageModel>? OnMessageReceived;
    public event Action? OnReconnected;
    public event Action<string>? OnClosed;

    public async Task ConnectAsync(string hubUrl)
    {
        if (_connection is not null) return;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<MessageModel>("ReceiveMessage", message =>
        {
            OnMessageReceived?.Invoke(message);
        });

        _connection.Reconnected += _ =>
        {
            OnReconnected?.Invoke();
            return Task.CompletedTask;
        };

        _connection.Closed += exception =>
        {
            OnClosed?.Invoke(exception?.Message ?? string.Empty);
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
    }

    public async Task JoinRoomAsync(int roomId)
    {
        if (_connection is null) return;
        await _connection.InvokeAsync("JoinRoom", roomId);
    }

    public async Task LeaveRoomAsync(int roomId)
    {
        if (_connection is null) return;
        await _connection.InvokeAsync("LeaveRoom", roomId);
    }

    public async Task SendMessageAsync(int roomId, int userId, string username, string content)
    {
        if (_connection is null) return;
        await _connection.InvokeAsync("SendMessage", roomId, userId, username, content);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
