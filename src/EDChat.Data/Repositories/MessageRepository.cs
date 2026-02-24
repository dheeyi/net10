using EDChat.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDChat.Data.Repositories;

public class MessageRepository(EDChatDb db) : IRepository<Message>
{
    public Task<List<Message>> GetAllAsync() =>
        db.Messages.Include(m => m.User).OrderBy(m => m.SentAt).ToListAsync();

    public Task<Message?> GetByIdAsync(int id) =>
        db.Messages.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == id);

    public Task<List<Message>> GetByRoomIdAsync(int roomId) =>
        db.Messages.Include(m => m.User).Where(m => m.RoomId == roomId).OrderBy(m => m.SentAt).ToListAsync();

    public async Task<Message> CreateAsync(Message entity)
    {
        db.Messages.Add(entity);
        await db.SaveChangesAsync();
        await db.Entry(entity).Reference(m => m.User).LoadAsync();
        return entity;
    }

    public async Task<Message> UpdateAsync(Message entity)
    {
        db.Messages.Update(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.Messages.FindAsync(id);
        if (entity is not null)
        {
            db.Messages.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
