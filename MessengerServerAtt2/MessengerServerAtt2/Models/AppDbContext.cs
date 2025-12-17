using Microsoft.EntityFrameworkCore;

namespace MessengerServer.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Dialog> Dialogs { get; set; }
        public DbSet<UserDialog> UserDialogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();

                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(u => u.Status).HasDefaultValue(UserStatus.Offline);
            });

            modelBuilder.Entity<UserDialog>(entity =>
            {
                entity.HasKey(ud => new { ud.UserId, ud.DialogId });

                entity.Property(ud => ud.JoinedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.Property(m => m.SentAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(m => m.Status).HasDefaultValue(MessageStatus.Sent);
            });
        }
    }
}