using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Services;

public interface IFunctionService
{
    Task<bool> CreateCustomerAsync(string name, string surname, string username, string email, string shippingAddress);
    Task<bool> CreateProductAsync(string productName, string description, double price, int stockAvailable, string imageUrl = "");
    Task<bool> CreateOrderAsync(string customerId, string username, string productId, string productName,
        DateTimeOffset orderDate, int quantity, double unitPrice, double totalPrice, string status);
}

public class FunctionService : IFunctionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FunctionService> _logger;
    private readonly string _functionsBaseUrl = "http://localhost:7015/api";

    public FunctionService(HttpClient httpClient, ILogger<FunctionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> CreateCustomerAsync(string name, string surname, string username, string email, string shippingAddress)
    {
        try
        {
            var customerData = new
            {
                name,
                surname,
                username,
                email,
                shippingAddress
            };
            var response = await _httpClient.PostAsJsonAsync($"{_functionsBaseUrl}/CreateCustomer", customerData);

            _logger.LogInformation("CreateCustomer function call - Status: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Function call failed for customer {Username}", username);
            return false;
        }
    }

    public async Task<bool> CreateProductAsync(string productName, string description, double price, int stockAvailable, string imageUrl = "")
    {
        try
        {
            var productData = new
            {
                productName,
                description,
                price,
                stockAvailable,
                imageUrl
            };
            var response = await _httpClient.PostAsJsonAsync($"{_functionsBaseUrl}/CreateProduct", productData);

            _logger.LogInformation("CreateProduct function call - Status: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product Function call failed for {ProductName}", productName);
            return false;
        }
    }

    public async Task<bool> CreateOrderAsync(string customerId, string username, string productId, string productName,
        DateTimeOffset orderDate, int quantity, double unitPrice, double totalPrice, string status)
    {
        try
        {
            var orderData = new
            {
                customerId,
                username,
                productId,
                productName,
                orderDate,
                quantity,
                unitPrice,
                totalPrice,
                status
            };
            var response = await _httpClient.PostAsJsonAsync($"{_functionsBaseUrl}/CreateOrder", orderData);

            _logger.LogInformation("CreateOrder function call - Status: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order Function call failed for product {ProductName}", productName);
            return false;
        }
    }
}