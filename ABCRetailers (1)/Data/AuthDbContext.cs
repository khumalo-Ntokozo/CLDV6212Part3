using Microsoft.EntityFrameworkCore;
using ABCRetailers.Models;

namespace ABCRetailers.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Cart> Cart { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => e.Username).IsUnique(); // Unique username
            });

            // Configure Cart entity
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.ToTable("Cart");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerUsername).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ProductId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.DateAdded).IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}