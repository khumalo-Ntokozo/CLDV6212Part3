using System.Globalization;
using ABCRetailers.Services;
using ABCRetailers.Data;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Add global error handling
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Add services to the container.
                builder.Services.AddControllersWithViews();

                // Add session support
                builder.Services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(30);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });

                // Register DbContext for authentication database
                builder.Services.AddDbContext<AuthDbContext>(options =>
                {
                    var connectionString = builder.Configuration.GetConnectionString("AuthDatabase");
                    Console.WriteLine($"🔍 Database Connection String: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");
                    options.UseSqlServer(connectionString);
                });

                // Register Database Initializer
                builder.Services.AddScoped<DatabaseInitializer>();

                // Register Azure Storage Service
                builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

                // Register Function Service 
                builder.Services.AddHttpClient<IFunctionService, FunctionService>();

                // Add logging
                builder.Services.AddLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                });

                builder.Services.AddHttpClient();

                var app = builder.Build();

                // Set culture for decimal handling - FIX CURRENCY DISPLAY
                var culture = new CultureInfo("en-US");
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                // Initialize database
                using (var scope = app.Services.CreateScope())
                {
                    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                    await initializer.InitializeAsync();
                }

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();
                }
                else
                {
                    // Detailed errors in development
                    app.UseDeveloperExceptionPage();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseAuthorization();

                // Add session middleware
                app.UseSession();

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                Console.WriteLine("✅ Application started successfully!");
                Console.WriteLine("🚀 Server is running...");

                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 CRITICAL ERROR during startup: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Wait for user input so the window doesn't close immediately
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}