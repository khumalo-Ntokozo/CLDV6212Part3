using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ABCRetailers.Controllers
{
    public class AdminController : Controller
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AuthDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Only existing admins can access this
        public IActionResult CreateAdmin()
        {
            // Check if current user is admin
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Login", "Login");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Login", "Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if username already exists
                    if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                    {
                        ModelState.AddModelError("Username", "Username already exists.");
                        return View(model);
                    }

                    var adminUser = new User
                    {
                        Username = model.Username,
                        PasswordHash = HashPassword(model.Password),
                        Role = "Admin"
                    };

                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Admin user {adminUser.Username} created by {HttpContext.Session.GetString("Username")}");
                    TempData["SuccessMessage"] = $"Admin user '{model.Username}' created successfully!";
                    return RedirectToAction("AdminDashboard", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating admin user {Username}", model.Username);
                    ModelState.AddModelError("", $"Error creating admin user: {ex.Message}");
                }
            }

            return View(model);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}