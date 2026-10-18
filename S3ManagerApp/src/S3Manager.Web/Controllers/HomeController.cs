using Microsoft.AspNetCore.Mvc;
using S3Manager.Web.Models;
using S3Manager.Web.Security;
using S3Manager.Web.Services;

namespace S3Manager.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICredentialStore _credentialStore;
    private readonly IBucketNameValidator _bucketValidator;
    private readonly IS3Service _s3Service;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ICredentialStore credentialStore,
        IBucketNameValidator bucketValidator,
        IS3Service s3Service,
        ILogger<HomeController> logger)
    {
        _credentialStore = credentialStore;
        _bucketValidator = bucketValidator;
        _s3Service = s3Service;
        _logger = logger;
    }

    public IActionResult Index(string? flash)
    {
        var model = new ConnectViewModel();
        if (!string.IsNullOrEmpty(flash))
            ViewData["Flash"] = flash;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Connect(ConnectViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.AccessKey) || string.IsNullOrWhiteSpace(model.SecretKey))
        {
            ModelState.AddModelError("", "Access key and secret key are required.");
            model.SecretKey = ""; // Clear secret
            return View("Index", model);
        }

        try
        {
            var connection = new S3Connection(
                model.AccessKey,
                model.SecretKey,
                model.SessionToken,
                model.Region,
                model.ServiceUrl,
                model.ReadOnly,
                model.DefaultBucket);

            // Verify connection
            var probe = await _s3Service.VerifyConnectionAsync(connection, ct);
            if (!probe.Success)
            {
                ModelState.AddModelError("", $"Connection failed: {probe.Message}");
                model.SecretKey = "";
                return View("Index", model);
            }

            // Save to session
            HttpContext.Session.Clear();
            _credentialStore.Save(HttpContext.Session, connection);

            return RedirectToAction("Buckets", "Buckets");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection attempt failed");
            ModelState.AddModelError("", "An error occurred. Please check your credentials.");
            model.SecretKey = "";
            return View("Index", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Disconnect()
    {
        var credStore = _credentialStore.Get(HttpContext.Session);
        if (credStore != null)
        {
            _credentialStore.Clear(HttpContext.Session);
        }
        return RedirectToAction("Index");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
