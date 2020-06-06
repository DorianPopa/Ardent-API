using Ardent_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Ardent_API
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            builder.Entity<Project>()
                .HasOne(p => p.Client);
            builder.Entity<Project>()
                .HasOne(p => p.Designer);
        }
    }
}
