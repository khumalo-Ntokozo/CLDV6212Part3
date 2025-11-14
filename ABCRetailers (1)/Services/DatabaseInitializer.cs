using ABCRetailers.Data;
using ABCRetailers.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ABCRetailers.Services
{
    public class DatabaseInitializer
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(AuthDbContext context, ILogger<DatabaseInitializer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing database...");

                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database ensured created");

                // Check if we have any users, if not, create default admin and customer
                if (!await _context.Users.AnyAsync())
                {
                    _logger.LogInformation("No users found, creating default users...");

                    var adminUser = new User
                    {
                        Username = "admin",
                        PasswordHash = HashPassword("admin123"),
                        Role = "Admin"
                    };

                    var customerUser = new User
                    {
                        Username = "customer01",
                        PasswordHash = HashPassword("customer123"),
                        Role = "Customer"
                    };

                    _context.Users.Add(adminUser);
                    _context.Users.Add(customerUser);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Default users created successfully");
                    _logger.LogInformation("Admin: admin / admin123 (Select Admin role)");
                    _logger.LogInformation("Customer: customer01 / customer123 (Select Customer role)");
                }
                else
                {
                    var userCount = await _context.Users.CountAsync();
                    _logger.LogInformation($"Database already has {userCount} users");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}