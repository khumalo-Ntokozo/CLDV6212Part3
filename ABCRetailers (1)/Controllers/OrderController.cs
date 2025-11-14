using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionService _functionService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IAzureStorageService storageService, IFunctionService functionService, IHttpClientFactory httpClientFactory, ILogger<OrderController> logger)
        {
            _storageService = storageService;
            _functionService = functionService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // ... (keep your existing TestFunctionsConnection method)

        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _storageService.GetAllEntitiesAsync<Order>();
                return View(orders);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading orders: {ex.Message}";
                return View(new List<Order>());
            }
        }

        public async Task<IActionResult> Create()
        {
            var model = new OrderCreateViewModel();
            await PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product details
                    var customer = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);

                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Check stock availability
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    _logger.LogInformation("Creating order for customer: {Customer}, product: {Product}, quantity: {Quantity}",
                        customer.Username, product.ProductName, model.Quantity);

                    // Convert DateTime to UTC for Azure compatibility
                    DateTimeOffset utcOrderDate = model.OrderDate.ToUniversalTime();

                    // Use double directly
                    double unitPrice = product.Price;
                    double totalPrice = product.Price * model.Quantity;

                    // Call Functions instead of local storage
                    var functionSuccess = await _functionService.CreateOrderAsync(
                        model.CustomerId,
                        customer.Username,
                        model.ProductId,
                        product.ProductName,
                        utcOrderDate,
                        model.Quantity,
                        unitPrice,
                        totalPrice,
                        "Submitted"
                    );

                    _logger.LogInformation("FunctionService returned: {Success}", functionSuccess);

                    if (functionSuccess)
                    {
                        // Update product stock locally (optional)
                        product.StockAvailable -= model.Quantity;
                        await _storageService.UpdateEntityAsync(product);

                        TempData["Success"] = "Order created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to create order via Functions. Please try again.");
                        _logger.LogWarning("FunctionService returned false for order creation");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in Create order for customer {CustomerId}", model.CustomerId);
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                    await PopulateDropdowns(model);
                    return View(model);
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure OrderDate is UTC before saving
                    if (order.OrderDate.Kind != DateTimeKind.Utc)
                    {
                        order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                    }

                    await _storageService.UpdateEntityAsync(order);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating order {OrderId}", order.OrderId);
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price, // Now returns double directly
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product price for {ProductId}", productId);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                var previousStatus = order.Status;
                order.Status = newStatus;

                // Recalculate total price when updating status
                order.TotalPrice = order.UnitPrice * order.Quantity;

                await _storageService.UpdateEntityAsync(order);

                _logger.LogInformation("Order {OrderId} status updated from {PreviousStatus} to {NewStatus}",
                    order.OrderId, previousStatus, newStatus);

                // Send queue message for status update
                var statusMessage = new
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Username,
                    ProductName = order.ProductName,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "System"
                };
                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));

                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }

        public async Task<IActionResult> MyOrders()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "Please login to view your orders.";
                return RedirectToAction("Login", "Login");
            }

            if (role != "Customer")
            {
                TempData["Error"] = "This page is for customers only.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Get orders for the current customer
                var allOrders = await _storageService.GetAllEntitiesAsync<Order>();
                var customerOrders = allOrders.Where(o => o.Username == username).ToList();

                return View(customerOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for user {Username}", username);
                TempData["Error"] = $"Error loading orders: {ex.Message}";
                return View(new List<Order>());
            }
        }
    }
}