using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using S3Manager.Web.Security;
using S3Manager.Web.Services;

var builder = WebApplication.CreateBuilder(args);

const long maxUploadBytes = 100L * 1024 * 1024; // 100 MB default

// Add services
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddDataProtection();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".S3Manager.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue("SessionIdleMinutes", 20));
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("connect", ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions 
        { 
            PermitLimit = 5, 
            Window = TimeSpan.FromMinutes(1) 
        }));
});

builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = maxUploadBytes);
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = maxUploadBytes);

builder.Services.Configure<ProtectedPathsOptions>(builder.Configuration.GetSection("ProtectedPaths"));

// Register services
builder.Services.AddSingleton<IBucketNameValidator, BucketNameValidator>();
builder.Services.AddSingleton<IS3ErrorMapper, S3ErrorMapper>();
builder.Services.AddSingleton<ICsvExporter, CsvExporter>();
builder.Services.AddSingleton<ICredentialStore, SessionCredentialStore>();
builder.Services.AddSingleton<IS3ClientFactory, S3ClientFactory>();
builder.Services.AddSingleton<IProtectedPathGuard, ProtectedPathGuard>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<IActivityLog, SessionActivityLog>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseSession();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
