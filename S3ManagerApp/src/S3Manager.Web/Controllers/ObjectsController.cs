using Microsoft.AspNetCore.Mvc;
using S3Manager.Web.Models;
using S3Manager.Web.Security;
using S3Manager.Web.Services;

namespace S3Manager.Web.Controllers;

[RequireConnection]
public class ObjectsController : Controller
{
    private readonly IS3Service _s3Service;
    private readonly ILogger<ObjectsController> _logger;

    public ObjectsController(IS3Service s3Service, ILogger<ObjectsController> logger)
    {
        _s3Service = s3Service;
        _logger = logger;
    }

    private S3Connection GetConnection()
    {
        return (S3Connection)HttpContext.Items["conn"]!;
    }

    /// <summary>
    /// List objects in a bucket with pagination and folder navigation
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(string bucket, string? prefix, string? token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(bucket))
            return RedirectToAction("Buckets", "Buckets");

        var conn = GetConnection();

        try
        {
            var objectPage = await _s3Service.ListObjectsAsync(conn, bucket, prefix ?? "", token, 50, ct);
            
            ViewData["Bucket"] = bucket;
            ViewData["Prefix"] = prefix ?? "";
            
            return View(objectPage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list objects");
            return RedirectToAction("Buckets", "Buckets", new { flash = "Failed to list objects: " + ex.Message });
        }
    }

    /// <summary>
    /// Get object details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(string bucket, string key, string? versionId, CancellationToken ct)
    {
        var conn = GetConnection();

        try
        {
            var details = await _s3Service.GetObjectDetailsAsync(conn, bucket, key, versionId, ct);
            ViewData["Bucket"] = bucket;
            return View(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get object details");
            return RedirectToAction("List", new { bucket, flash = "Failed to get object details" });
        }
    }

    /// <summary>
    /// Download object
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Download(string bucket, string key, string? versionId, CancellationToken ct)
    {
        var conn = GetConnection();

        try
        {
            var response = await _s3Service.OpenObjectAsync(conn, bucket, key, versionId, ct);
            var fileName = Path.GetFileName(key);
            
            return File(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download object");
            return RedirectToAction("List", new { bucket, flash = "Failed to download object: " + ex.Message });
        }
    }

    /// <summary>
    /// Upload file form
    /// </summary>
    [HttpGet]
    public IActionResult Upload(string bucket, string? prefix)
    {
        var conn = GetConnection();
        ReadOnlyGuard.EnsureWritable(conn);

        ViewData["Bucket"] = bucket;
        ViewData["Prefix"] = prefix ?? "";
        return View();
    }

    /// <summary>
    /// Handle file upload
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(string bucket, string? prefix, IFormFile? file, CancellationToken ct)
    {
        var conn = GetConnection();
        ReadOnlyGuard.EnsureWritable(conn);

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Please select a file to upload");
            ViewData["Bucket"] = bucket;
            ViewData["Prefix"] = prefix ?? "";
            return View();
        }

        try
        {
            var key = string.IsNullOrWhiteSpace(prefix) ? file.FileName : $"{prefix.TrimEnd('/')}/{file.FileName}";

            using var stream = file.OpenReadStream();
            var opts = new UploadOptions { ContentType = file.ContentType };

            await _s3Service.UploadAsync(conn, bucket, key, stream, opts, ct);

            return RedirectToAction("List", new { bucket, prefix = prefix ?? "", flash = $"File '{file.FileName}' uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file");
            ModelState.AddModelError("", "Upload failed: " + ex.Message);
            ViewData["Bucket"] = bucket;
            ViewData["Prefix"] = prefix ?? "";
            return View();
        }
    }

    /// <summary>
    /// Create folder
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFolder(string bucket, string? prefix, string folderName, CancellationToken ct)
    {
        var conn = GetConnection();
        ReadOnlyGuard.EnsureWritable(conn);

        if (string.IsNullOrWhiteSpace(folderName))
            return RedirectToAction("List", new { bucket, prefix, flash = "Folder name is required" });

        try
        {
            var folderKey = string.IsNullOrWhiteSpace(prefix) 
                ? $"{folderName}/" 
                : $"{prefix.TrimEnd('/')}/{folderName}/";

            await _s3Service.CreateFolderAsync(conn, bucket, folderKey, ct);

            return RedirectToAction("List", new { bucket, prefix = prefix ?? "", flash = $"Folder '{folderName}' created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder");
            return RedirectToAction("List", new { bucket, prefix, flash = "Failed to create folder: " + ex.Message });
        }
    }

    /// <summary>
    /// Delete object
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string bucket, string key, string? prefix, CancellationToken ct)
    {
        var conn = GetConnection();
        ReadOnlyGuard.EnsureWritable(conn);

        try
        {
            await _s3Service.DeleteObjectsAsync(conn, bucket, new[] { key }, ct);
            return RedirectToAction("List", new { bucket, prefix = prefix ?? "", flash = $"Object '{Path.GetFileName(key)}' deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object");
            return RedirectToAction("List", new { bucket, prefix, flash = "Failed to delete object: " + ex.Message });
        }
    }

    /// <summary>
    /// Get pre-signed URL for object
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetPresignedUrl(string bucket, string key, int expiryMinutes, CancellationToken ct)
    {
        var conn = GetConnection();

        try
        {
            var url = await _s3Service.CreatePresignedUrlAsync(conn, bucket, key, TimeSpan.FromMinutes(expiryMinutes), ct);
            
            return Json(new { success = true, url = url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create presigned URL");
            return Json(new { success = false, message = "Failed to create presigned URL: " + ex.Message });
        }
    }

    /// <summary>
    /// List object versions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Versions(string bucket, string key, CancellationToken ct)
    {
        var conn = GetConnection();

        try
        {
            var versions = await _s3Service.ListVersionsAsync(conn, bucket, key, ct);
            ViewData["Bucket"] = bucket;
            ViewData["Key"] = key;
            return View(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list versions");
            return RedirectToAction("Details", new { bucket, key, flash = "Failed to list versions" });
        }
    }
}
