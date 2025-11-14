using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetails.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public UploadController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        public ActionResult Index()
        {
            return View(new FileUploadModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                    {
                        // Upload to blob storage
                        var fileName = await _storageService.UploadFileAsync(model.ProofOfPayment, "payment-proofs");

                        // Also upload to file share for contracts
                        await _storageService.UploadToFileShareAsync(model.ProofOfPayment, "contracts", "payments");

                        TempData["Success"] = "File uploaded successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }
            return View(model);
        }
    }
}