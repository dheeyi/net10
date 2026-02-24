using EDChat.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDChat.Data;

public class EDChatDb(DbContextOptions<EDChatDb> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).IsRequired().HasMaxLength(2000);
            entity.HasOne(m => m.User).WithMany(u => u.Messages).HasForeignKey(m => m.UserId);
            entity.HasOne(m => m.Room).WithMany(r => r.Messages).HasForeignKey(m => m.RoomId);
        });

        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, Name = "General", Description = "Sala de chat general", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Room { Id = 2, Name = "Tecnologia", Description = "Discusiones sobre tecnologia", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
