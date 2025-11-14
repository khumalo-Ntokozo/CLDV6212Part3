using System.Text.Json;
using ABCRetailers.Models;
using ABCRetailers.Services;
using ABCRetailers.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Controllers
{
    public class CartController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionService _functionService;
        private readonly ILogger<CartController> _logger;

        public CartController(IAzureStorageService storageService, IFunctionService functionService, ILogger<CartController> logger)
        {
            _storageService = storageService;
            _functionService = functionService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "Please login to view your cart.";
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var cartItems = await GetCartItems(username);

                // Convert to List<CartItemViewModel> that your view expects
                var cartItemViewModels = new List<CartItemViewModel>();
                foreach (var item in cartItems)
                {
                    var product = await _storageService.GetEntityAsync<Product>("Product", item.ProductId);
                    cartItemViewModels.Add(new CartItemViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Price = item.UnitPrice, // double
                        Quantity = item.Quantity,
                        Description = product?.Description ?? "",
                        ImageUrl = product?.ImageUrl ?? ""
                    });
                }

                // Return List<CartItemViewModel> that the view expects
                return View(cartItemViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart for user {Username}", username);
                TempData["Error"] = "Error loading cart items.";
                return View(new List<CartItemViewModel>()); // Return empty list
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "Please login to add items to cart.";
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                if (quantity > product.StockAvailable)
                {
                    TempData["Error"] = $"Insufficient stock. Only {product.StockAvailable} available.";
                    return RedirectToAction("Index", "Product");
                }

                // Get existing cart items
                var cartItems = await GetCartItems(username);

                // Check if product already in cart
                var existingItem = cartItems.FirstOrDefault(item => item.ProductId == productId);
                if (existingItem != null)
                {
                    // Update quantity
                    existingItem.Quantity += quantity;
                    existingItem.TotalPrice = existingItem.UnitPrice * existingItem.Quantity;
                }
                else
                {
                    // Add new item
                    var cartItem = new CartItem
                    {
                        ProductId = productId,
                        ProductName = product.ProductName,
                        UnitPrice = product.Price,
                        Quantity = quantity,
                        TotalPrice = product.Price * quantity
                    };
                    cartItems.Add(cartItem);
                }

                // Save cart to session
                HttpContext.Session.SetString($"Cart_{username}", JsonSerializer.Serialize(cartItems));

                TempData["Success"] = "Product added to cart successfully!";
                return RedirectToAction("Index", "Product");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart for user {Username}", productId, username);
                TempData["Error"] = "Error adding product to cart.";
                return RedirectToAction("Index", "Product");
            }
        }

        // For AJAX requests (keep this version for JavaScript calls)
        [HttpPost]
        public async Task<JsonResult> AddToCartAjax(string productId, int quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Please login to add items to cart." });
            }

            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                if (quantity > product.StockAvailable)
                {
                    return Json(new { success = false, message = $"Insufficient stock. Only {product.StockAvailable} available." });
                }

                var cartItems = await GetCartItems(username);
                var existingItem = cartItems.FirstOrDefault(item => item.ProductId == productId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.TotalPrice = existingItem.UnitPrice * existingItem.Quantity;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        ProductId = productId,
                        ProductName = product.ProductName,
                        UnitPrice = product.Price,
                        Quantity = quantity,
                        TotalPrice = product.Price * quantity
                    };
                    cartItems.Add(cartItem);
                }

                HttpContext.Session.SetString($"Cart_{username}", JsonSerializer.Serialize(cartItems));

                return Json(new { success = true, message = "Product added to cart successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart for user {Username}", productId, username);
                return Json(new { success = false, message = "Error adding product to cart." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "Please login to checkout.";
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var cartItems = await GetCartItems(username);
                if (!cartItems.Any())
                {
                    TempData["Error"] = "Cart is empty.";
                    return RedirectToAction("Index");
                }

                // Create orders for each cart item
                var errors = new List<string>();
                foreach (var item in cartItems)
                {
                    try
                    {
                        var product = await _storageService.GetEntityAsync<Product>("Product", item.ProductId);
                        if (product == null)
                        {
                            errors.Add($"Product {item.ProductName} not found.");
                            continue;
                        }

                        if (item.Quantity > product.StockAvailable)
                        {
                            errors.Add($"Insufficient stock for {item.ProductName}. Available: {product.StockAvailable}");
                            continue;
                        }

                        // Create order using FunctionService
                        var functionSuccess = await _functionService.CreateOrderAsync(
                            customerId: "", // You'll need to get the customer ID
                            username: username,
                            productId: item.ProductId,
                            productName: item.ProductName,
                            orderDate: DateTimeOffset.UtcNow,
                            quantity: item.Quantity,
                            unitPrice: item.UnitPrice,
                            totalPrice: item.TotalPrice,
                            status: "Submitted"
                        );

                        if (!functionSuccess)
                        {
                            errors.Add($"Failed to create order for {item.ProductName}");
                        }
                        else
                        {
                            // Update product stock
                            product.StockAvailable -= item.Quantity;
                            await _storageService.UpdateEntityAsync(product);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating order for product {ProductId}", item.ProductId);
                        errors.Add($"Error creating order for {item.ProductName}: {ex.Message}");
                    }
                }

                if (errors.Any())
                {
                    TempData["Error"] = $"Failed to create some orders. Errors: {string.Join(", ", errors)}";
                    return RedirectToAction("Index");
                }

                // Clear cart after successful checkout
                HttpContext.Session.Remove($"Cart_{username}");

                TempData["Success"] = "Checkout successful! Your orders have been created.";
                return RedirectToAction("Index", "Order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout for user {Username}", username);
                TempData["Error"] = $"Error during checkout: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // For AJAX requests (keep this version for JavaScript calls)
        [HttpPost]
        public async Task<JsonResult> CheckoutAjax()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Please login to checkout." });
            }

            try
            {
                var cartItems = await GetCartItems(username);
                if (!cartItems.Any())
                {
                    return Json(new { success = false, message = "Cart is empty." });
                }

                var errors = new List<string>();
                foreach (var item in cartItems)
                {
                    try
                    {
                        var product = await _storageService.GetEntityAsync<Product>("Product", item.ProductId);
                        if (product == null)
                        {
                            errors.Add($"Product {item.ProductName} not found.");
                            continue;
                        }

                        if (item.Quantity > product.StockAvailable)
                        {
                            errors.Add($"Insufficient stock for {item.ProductName}. Available: {product.StockAvailable}");
                            continue;
                        }

                        var functionSuccess = await _functionService.CreateOrderAsync(
                            customerId: "",
                            username: username,
                            productId: item.ProductId,
                            productName: item.ProductName,
                            orderDate: DateTimeOffset.UtcNow,
                            quantity: item.Quantity,
                            unitPrice: item.UnitPrice,
                            totalPrice: item.TotalPrice,
                            status: "Submitted"
                        );

                        if (!functionSuccess)
                        {
                            errors.Add($"Failed to create order for {item.ProductName}");
                        }
                        else
                        {
                            product.StockAvailable -= item.Quantity;
                            await _storageService.UpdateEntityAsync(product);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating order for product {ProductId}", item.ProductId);
                        errors.Add($"Error creating order for {item.ProductName}: {ex.Message}");
                    }
                }

                if (errors.Any())
                {
                    return Json(new { success = false, message = $"Failed to create some orders. Errors: {string.Join(", ", errors)}" });
                }

                HttpContext.Session.Remove($"Cart_{username}");

                return Json(new { success = true, message = "Checkout successful! Orders created." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout for user {Username}", username);
                return Json(new { success = false, message = $"Error during checkout: {ex.Message}" });
            }
        }

        // Keep your existing UpdateQuantity, RemoveFromCart, and GetCartItems methods as they are
        // but make sure they use TempData for regular requests and Json for AJAX requests

        private async Task<List<CartItem>> GetCartItems(string username)
        {
            var cartJson = HttpContext.Session.GetString($"Cart_{username}");
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }
    }

    public class CartViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public double TotalAmount { get; set; }
    }

    public class CartItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public double UnitPrice { get; set; }
        public int Quantity { get; set; }
        public double TotalPrice { get; set; }
    }
}