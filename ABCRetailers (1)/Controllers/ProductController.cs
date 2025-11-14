using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionService _functionService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, IFunctionService functionService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _functionService = functionService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {

            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    string imageUrl = string.Empty;
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        product.ImageUrl = imageUrl;
                    }

                    // Call Functions instead of local storage
                    var functionSuccess = await _functionService.CreateProductAsync(
                        product.ProductName,
                        product.Description,
                        product.Price,
                        product.StockAvailable,
                        imageUrl
                    );

                    if (functionSuccess)
                    {
                        TempData["Success"] = $"Product '{product.ProductName}' created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to create product via Functions.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original product to preserve ETag
                    var originalProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                    if (originalProduct == null)
                    {
                        return NotFound();
                    }

                    // Update properties but keep the original ETag
                    originalProduct.ProductName = product.ProductName;
                    originalProduct.Description = product.Description;
                    originalProduct.Price = product.Price; // This should work with double
                    originalProduct.StockAvailable = product.StockAvailable;

                    // Upload new image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        originalProduct.ImageUrl = imageUrl;
                    }

                    await _storageService.UpdateEntityAsync(originalProduct);
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                await _storageService.DeleteEntityAsync<Product>("Product", id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        

    }
}