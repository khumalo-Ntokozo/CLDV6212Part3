using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<LoginController> _logger;

        public LoginController(AuthDbContext context, ILogger<LoginController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _logger.LogInformation($"Login attempt for user: {model.Username} as {model.Role}");

                    var hashedPassword = HashPassword(model.Password);

                    // Find user by username and password
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username && u.PasswordHash == hashedPassword);

                    if (user != null)
                    {
                        // Check if the selected role matches the user's actual role
                        if (user.Role == model.Role)
                        {
                            // Store user info in session
                            HttpContext.Session.SetString("Username", user.Username);
                            HttpContext.Session.SetString("Role", user.Role);

                            _logger.LogInformation($"User {user.Username} logged in successfully as {user.Role}");

                            // Redirect based on role
                            if (user.Role == "Admin")
                            {
                                TempData["SuccessMessage"] = "Welcome back, Admin!";
                                return RedirectToAction("AdminDashboard", "Home");
                            }
                            else
                            {
                                TempData["SuccessMessage"] = $"Welcome back, {user.Username}!";
                                return RedirectToAction("CustomerDashboard", "Home");
                            }
                        }
                        else
                        {
                            // User exists but wrong role selected
                            ModelState.AddModelError("Role", $"Please select '{user.Role}' role for this account.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid username or password.");
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", model.Username);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Your existing POST logic remains the same
            try
            {
                _logger.LogInformation($"Registration attempt for user: {model.Username} as {model.Role}");

                if (ModelState.IsValid)
                {
                    // Check if username already exists
                    if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                    {
                        ModelState.AddModelError("Username", "Username already exists. Please choose a different username.");
                        return View(model);
                    }

                    var user = new User
                    {
                        Username = model.Username,
                        PasswordHash = HashPassword(model.Password),
                        Role = model.Role
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User {user.Username} registered successfully as {user.Role}");

                    // Auto login after registration
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role);

                    TempData["SuccessMessage"] = $"Welcome {user.Username}! You have been registered successfully as a {user.Role}.";

                    // ✅ TEMPORARY FIX: Redirect to Home instead of Dashboard
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogWarning($"Registration failed validation for user: {model.Username}");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Username}", model.Username);
                ModelState.AddModelError("", $"An error occurred during registration: {ex.Message}. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var username = HttpContext.Session.GetString("Username");
            HttpContext.Session.Clear();
            _logger.LogInformation($"User {username} logged out");
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        [HttpGet]
        public async Task<IActionResult> DebugUsers()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                var result = "Users in database:\n";
                foreach (var user in users)
                {
                    result += $"Username: {user.Username}, Role: {user.Role}, PasswordHash: {user.PasswordHash}\n";
                }
                return Content(result);
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }

    }


}