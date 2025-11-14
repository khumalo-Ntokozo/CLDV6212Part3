using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionService _functionService;

        public CustomerController(IAzureStorageService storageService, IFunctionService functionService)
        {
            _storageService = storageService;
            _functionService = functionService;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            return View(customers);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ ONLY call Functions (no local storage)
                    var functionSuccess = await _functionService.CreateCustomerAsync(
                        customer.Name,
                        customer.Surname,
                        customer.Username,
                        customer.Email,
                        customer.ShippingAddress
                    );

                    if (functionSuccess)
                    {
                        // ❌ REMOVED: await _storageService.AddEntityAsync(customer);

                        TempData["Success"] = "Customer created successfully! Check Functions terminal! 🎯";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to create customer via Functions.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _storageService.GetEntityAsync<Customer>("Customer", id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _storageService.UpdateEntityAsync(customer);
                    TempData["Success"] = "Customer updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Customer>("Customer", id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ UPDATED: Test method with correct properties
        public async Task<IActionResult> TestFunctions()
        {
            try
            {
                var success = await _functionService.CreateCustomerAsync(
                    "Test",
                    "Customer",
                    "testuser",
                    "test@functions.com",
                    "123 Test Street"
                );

                if (success)
                {
                    TempData["Success"] = "✅ Functions connection working! Check terminal!";
                }
                else
                {
                    TempData["Error"] = "❌ Functions connection failed";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Functions error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}