using System.Net.Http.Json;
using EDChat.Web.Models;

namespace EDChat.Web.Services;

public class ApiClient(IHttpClientFactory factory)
{
    private readonly HttpClient Http = factory.CreateClient("Api");

    public UserModel? CurrentUser { get; private set; }

    public string BaseAddress => Http.BaseAddress?.ToString() ?? "";

    public async Task<UserModel?> LoginAsync(string username)
    {
        var response = await Http.PostAsJsonAsync("/api/users", new { username });

        if (response.IsSuccessStatusCode)
        {
            CurrentUser = await response.Content.ReadFromJsonAsync<UserModel>();
            return CurrentUser;
        }

        return null;
    }

    public async Task<List<RoomModel>> GetRoomsAsync()
    {
        return await Http.GetFromJsonAsync<List<RoomModel>>("/api/rooms") ?? [];
    }

    public async Task<RoomModel?> CreateRoomAsync(string name, string description)
    {
        var response = await Http.PostAsJsonAsync("/api/rooms", new { name, description });

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RoomModel>();
        }

        return null;
    }

    public async Task<List<MessageModel>> GetMessagesAsync(int roomId)
    {
        return await Http.GetFromJsonAsync<List<MessageModel>>($"/api/rooms/{roomId}/messages") ?? [];
    }
}
