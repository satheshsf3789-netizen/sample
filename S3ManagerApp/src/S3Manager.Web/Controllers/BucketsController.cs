using Microsoft.AspNetCore.Mvc;
using S3Manager.Web.Models;
using S3Manager.Web.Security;
using S3Manager.Web.Services;

namespace S3Manager.Web.Controllers;

[RequireConnection]
public class BucketsController : Controller
{
    private readonly IS3Service _s3Service;
    private readonly ILogger<BucketsController> _logger;

    public BucketsController(IS3Service s3Service, ILogger<BucketsController> logger)
    {
        _s3Service = s3Service;
        _logger = logger;
    }

    private S3Connection GetConnection()
    {
        return (S3Connection)HttpContext.Items["conn"]!;
    }

    public async Task<IActionResult> Buckets(CancellationToken ct)
    {
        var conn = GetConnection();
        try
        {
            var buckets = await _s3Service.ListBucketsAsync(conn, ct);
            return View(buckets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list buckets");
            return RedirectToAction("Index", "Home", new { flash = "Failed to list buckets" });
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        var conn = GetConnection();
        ReadOnlyGuard.EnsureWritable(conn);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string region, bool versioning, CancellationToken ct)
    {
        var conn = GetConnection();
        ReadOnlyGuard.EnsureWritable(conn);

        try
        {
            await _s3Service.CreateBucketAsync(conn, name, region, versioning, ct);
            return RedirectToAction("Buckets", new { flash = $"Bucket '{name}' created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bucket");
            return RedirectToAction("Buckets", new { flash = "Failed to create bucket: " + ex.Message });
        }
    }
}
