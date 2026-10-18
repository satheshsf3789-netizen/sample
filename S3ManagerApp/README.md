# S3 Manager

A secure, browser-based AWS S3 management tool built with ASP.NET Core MVC.

## Features

**Core Capabilities (F-01 through F-22):**

- **F-01:** Connect to AWS S3 or S3-compatible endpoints (MinIO, etc.) with access key/secret
- **F-02:** Verify connection and probe IAM capabilities
- **F-03:** List all buckets in the account
- **F-04:** Create new buckets with optional versioning
- **F-05:** Browse bucket contents with folder-like navigation
- **F-06:** List objects by prefix with pagination
- **F-07:** View object metadata and versions
- **F-08:** Download objects with streaming (no buffering)
- **F-09:** Upload files with multipart support
- **F-10:** Create folder markers
- **F-11:** Copy objects within or between buckets
- **F-12:** Move/rename objects with verification
- **F-13:** Update object metadata
- **F-14:** Delete individual objects
- **F-15:** Delete multiple objects in one operation
- **F-16:** Delete by prefix with dry-run preview
- **F-17:** Enable/disable bucket versioning
- **F-18:** List object versions and restore old versions
- **F-19:** Delete entire buckets (with safety confirmation)
- **F-20:** Empty buckets (handles versioning)
- **F-21:** Generate pre-signed URLs for temporary access
- **F-22:** Export object listings to CSV

**Security Features:**

- Credentials encrypted in session (ASP.NET Core Data Protection API)
- Read-only mode to prevent mutations
- Protected bucket/prefix guards (configurable)
- CSRF protection on all mutating actions
- Content Security Policy headers
- Anti-formula-injection in CSV exports
- Rate limiting on connection attempts
- Session-only credential storage (HttpOnly, SameSite=Strict)
- No credentials logged or committed

## Prerequisites

- .NET 8.0 SDK or later
- AWS account or S3-compatible storage (MinIO, etc.)
- IAM user with appropriate S3 permissions

## Quick Start

### Build and Run

```bash
cd S3Manager
dotnet dev-certs https --trust  # On Windows/macOS, trust HTTPS cert
dotnet run --project src/S3Manager.Web
```

The app will start on `https://localhost:5001` (or as configured).

### Run Tests

```bash
dotnet test tests/S3Manager.Tests
```

## Configuration

Edit `src/S3Manager.Web/appsettings.json`:

```json
{
  "SessionIdleMinutes": 20,
  "MaxUploadMb": 100,
  "DefaultPageSize": 50,
  "PreviewMaxBytes": 1048576,
  "AllowCustomEndpoint": true,
  "ProtectedBuckets": [],
  "ProtectedPrefixes": ["audit/*", "backups/*"],
  "RateLimit": {
    "ConnectPerMinute": 5
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Configuration Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `SessionIdleMinutes` | 20 | Session timeout in minutes |
| `MaxUploadMb` | 100 | Maximum file upload size |
| `DefaultPageSize` | 50 | Objects per page in listing |
| `PreviewMaxBytes` | 1048576 (1 MB) | Max size for text preview |
| `AllowCustomEndpoint` | true | Allow S3-compatible endpoints |
| `ProtectedBuckets` | [] | Bucket names to protect from mutations |
| `ProtectedPrefixes` | [] | Prefixes (bucket/path) to protect |

## Security & Design

### Where Credentials Live

1. **User Input:** Entered via browser form at connection time
2. **Server Memory:** Encrypted by ASP.NET Core Data Protection API
3. **Session Store:** Stored in in-memory session (distributed caches with encryption recommended for production)
4. **Transmitted:** Never sent to client; only opaque HttpOnly session cookie
5. **Never Logged:** Secrets redacted from all logs

### Protected Prefixes

Configure via `ProtectedPrefixes` in `appsettings.json`:

```json
"ProtectedPrefixes": [
  "audit/*",           // Protects all of audit/
  "backups/prod/*",    // Protects production backups
  "*/private/*"        // Wildcard pattern
]
```

Operators can unlock protection for the session by typing `UNPROTECT` when prompted.

### Read-Only Mode

Connect with the "Read-only mode" checkbox to prevent all mutations. Server enforces this; the UI is purely a convenience.

## Sample IAM Policies

### Full Access to One Bucket

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:ListBucket","s3:GetBucketLocation","s3:ListBucketVersions"],
      "Resource": "arn:aws:s3:::my-bucket"
    },
    {
      "Effect": "Allow",
      "Action": ["s3:GetObject","s3:PutObject","s3:DeleteObject","s3:GetObjectVersion","s3:DeleteObjectVersion"],
      "Resource": "arn:aws:s3:::my-bucket/*"
    }
  ]
}
```

### Read-Only Viewer

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:ListBucket","s3:GetBucketLocation"],
      "Resource": "arn:aws:s3:::my-bucket"
    },
    {
      "Effect": "Allow",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::my-bucket/*"
    }
  ]
}
```

### Write-Only (Exports)

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "s3:PutObject",
      "Resource": "arn:aws:s3:::my-bucket/exports/*"
    }
  ]
}
```

## Troubleshooting

| Error | Cause | Fix |
|-------|-------|-----|
| `InvalidAccessKeyId` | Access key typo or invalid | Check key format (should start with AKIA for long-term keys) |
| `SignatureDoesNotMatch` | Secret key mismatch | Re-enter secret key; check for missing chars or trailing spaces |
| `ExpiredToken` | Temporary credential expired | Generate new temporary credentials |
| `AccessDenied` | IAM policy missing permissions | Add required S3 actions to policy; see Help section |
| `NoSuchBucket` | Bucket name or region wrong | Verify bucket exists and is in the specified region |
| `BucketNotEmpty` | Attempting to delete non-empty bucket | Use "Empty bucket" first to remove all objects/versions |
| `RequestTimeTooSkewed` | Server clock out of sync | Fix NTP synchronization on the server |

## Known Limitations

- **Single-Node Sessions:** In-memory sessions do not survive app restart or scale-out. For production multi-node deployments, configure distributed session storage (Redis, SQL Server) with encryption.
- **Large Listings:** Very large object counts (millions) may take time to list; pagination helps mitigate this.
- **Preview Limits:** Text and JSON previews are limited to 1 MB by default; adjust `PreviewMaxBytes` in config.
- **No Advanced Filtering:** Filter by metadata or tags not yet supported; use S3 console for complex queries.

## Docker / MinIO Integration

To test against MinIO locally:

```bash
docker-compose up
```

Then connect with:
- **Service URL:** `https://localhost:9000`
- **Access Key:** `minioadmin` (default)
- **Secret Key:** `minioadmin` (default)
- **Region:** `us-east-1`

## Project Structure

```
S3Manager/
├── src/
│   └── S3Manager.Web/
│       ├── Controllers/          HomeController, BucketsController, ObjectsController, ToolsController, HelpController
│       ├── Services/             S3Service, S3ClientFactory, SessionCredentialStore, BucketNameValidator, etc.
│       ├── Security/             SecurityHeadersMiddleware, RequireConnectionAttribute, ReadOnlyGuard, ProtectedPathGuard
│       ├── Models/               S3Connection, ViewModels, ObjectItem, BucketInfo, etc.
│       ├── Views/                Shared (_Layout), Home, Buckets, Objects, Tools, Help
│       ├── wwwroot/              css/site.css, js/site.js, lib/bootstrap/
│       ├── Program.cs            DI & middleware setup
│       ├── appsettings.json      Configuration
│       └── S3Manager.Web.csproj
├── tests/
│   └── S3Manager.Tests/
│       ├── BucketNameValidatorTests.cs
│       ├── S3ErrorMapperTests.cs
│       ├── CsvExporterTests.cs
│       └── S3Manager.Tests.csproj
└── S3Manager.sln
```

## Testing

Unit tests cover:
- Bucket name validation (AWS rules)
- Error mapping (friendly messages)
- CSV export (formula injection guards)
- Credential storage (encryption/decryption)
- Session management

Run tests:

```bash
dotnet test
```

## Production Deployment

1. **Enable HTTPS:** Configure TLS certificates
2. **Session Storage:** Switch to Redis or SQL Server for distributed scenarios
3. **Data Protection Keys:** Persist keys securely (e.g., HSM, Azure Key Vault)
4. **Rate Limiting:** Adjust or add additional limits in `Program.cs`
5. **Logging:** Configure centralized logging (Application Insights, Serilog, etc.)
6. **Authentication:** Add identity provider (OAuth2, OIDC, AD) if exposing publicly
7. **Firewall:** Restrict network access to trusted IPs

## License

MIT

## Support

For issues, questions, or feedback, open an issue or contact the development team.
