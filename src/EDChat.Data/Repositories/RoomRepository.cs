using EDChat.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDChat.Data.Repositories;

public class RoomRepository(EDChatDb db) : IRepository<Room>
{
    public Task<List<Room>> GetAllAsync() =>
        db.Rooms.OrderBy(r => r.Name).ToListAsync();

    public Task<Room?> GetByIdAsync(int id) =>
        db.Rooms.FindAsync(id).AsTask();

    public async Task<Room> CreateAsync(Room entity)
    {
        db.Rooms.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<Room> UpdateAsync(Room entity)
    {
        db.Rooms.Update(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.Rooms.FindAsync(id);
        if (entity is not null)
        {
            db.Rooms.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
