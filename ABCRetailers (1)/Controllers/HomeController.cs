using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAzureStorageService _storageService;

        public HomeController(ILogger<HomeController> logger, IAzureStorageService storageService)
        {
            _logger = logger;
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var customers = await _storageService.GetAllEntitiesAsync<Customer>();
                var products = await _storageService.GetAllEntitiesAsync<Product>();
                var orders = await _storageService.GetAllEntitiesAsync<Order>();

                var model = new HomeViewModel
                {
                    CustomerCount = customers.Count,
                    ProductCount = products.Count,
                    OrderCount = orders.Count,
                    FeaturedProducts = products.Take(5).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page data");
                // Return empty model if there's an error
                return View(new HomeViewModel
                {
                    CustomerCount = 0,
                    ProductCount = 0,
                    OrderCount = 0,
                    FeaturedProducts = new List<Product>()
                });
            }
        }

        // ✅ ADD THESE MISSING DASHBOARD ACTIONS
        public IActionResult AdminDashboard()
        {
            // Check if user is actually an admin
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Login", "Login");
            }
            return View();
        }

        public IActionResult CustomerDashboard()
        {
            // Check if user is actually a customer
            if (HttpContext.Session.GetString("Role") != "Customer")
            {
                TempData["Error"] = "Access denied. Customer account required.";
                return RedirectToAction("Login", "Login");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}