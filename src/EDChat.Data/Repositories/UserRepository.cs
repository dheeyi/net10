using EDChat.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDChat.Data.Repositories;

public class UserRepository(EDChatDb db) : IRepository<User>
{
    public Task<List<User>> GetAllAsync() =>
        db.Users.OrderBy(u => u.Username).ToListAsync();

    public Task<User?> GetByIdAsync(int id) =>
        db.Users.FindAsync(id).AsTask();

    public Task<User?> GetByUsernameAsync(string username) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User> CreateAsync(User entity)
    {
        db.Users.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<User> UpdateAsync(User entity)
    {
        db.Users.Update(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.Users.FindAsync(id);
        if (entity is not null)
        {
            db.Users.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
