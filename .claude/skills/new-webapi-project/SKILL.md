---
name: new-webapi-project
description: Full setup guide for creating a new ASP.NET Core Web API project. Covers dotnet new options, Program.cs structure, ElmahCore, CORS, appsettings, and NuGet security check. Use when creating a new .NET Web API project or setting up a fresh backend project.
---

# New .NET Web API Project – Setup Guide

## Step 1 – Repository Environment Files

### `.gitignore` (repository root)

```
## Visual Studio
.vs/
*.user
*.suo

## Build outputs
bin/
obj/
*.dll
*.exe
*.pdb

## .NET
project.lock.json
project.fragment.lock.json
artifacts/
TestResults/

## IDE
.vscode/*
!.vscode/settings.json
.idea/

## Node / Frontend
node_modules/
dist/
```

### `.vscode/settings.json` (repository root)

```json
{
  "files.exclude": {
    "**/TestResults": true,
    "**/.vs": true,
    "**/dist": true,
    "**/node_modules": true,
    "**/*.sln": true
  }
}
```

This file is tracked in Git (exception in `.gitignore`) so the team shares the same Explorer view.

---

## Step 2 – Create the Project

```bash
dotnet new webapi -n ProjectName -o path --use-controllers --no-openapi
```

Options:
| Option | Value |
|--------|-------|
| Configure HTTPS | Yes |
| Top-level statements | No (use explicit `Program` class with `Main`) |
| OpenAPI/Swagger | No |
| Use controllers | Yes |

---

## Step 2 – Program.cs Structure

Do not use top-level statements. Use explicit `class Program` with `static void Main`.

Extract into two methods:
1. `ConfigureServices(WebApplicationBuilder builder)` — all service registration
2. `Configure(WebApplication app)` — all middleware pipeline

```csharp
static void Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    ConfigureServices(builder);
    var app = builder.Build();
    Configure(app);
    app.Run();
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddControllers();
    // ... other registrations
}

static void Configure(WebApplication app)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseElmah();
    app.MapControllers();
}
```

---

## Step 3 – ElmahCore (Error Logging)

```bash
dotnet add package ElmahCore
```

```csharp
// ConfigureServices
builder.Services.AddElmah(options =>
{
    options.OnPermissionCheck = ctx => ctx.User.Identity?.IsAuthenticated == true;
});

// Configure (after UseCors, before MapControllers)
app.UseElmah();
```

Enable in all environments — ElmahCore logs to memory by default.

---

## Step 4 – CORS

Only register CORS when `AllowedOrigins` is present in config. Never hardcode fallback origins.

```csharp
// ConfigureServices
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
if (allowedOrigins?.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
    });
}

// Configure
if (app.Configuration.GetSection("AllowedOrigins").Get<string[]>()?.Length > 0)
{
    app.UseCors();
}
```

---

## Step 5 – appsettings

Always commit `appsettings.Development.json` — contains only local dev placeholders:

```json
{
  "Jwt": { "Secret": "dev-secret-key-min-32-chars-long!!" },
  "AllowedOrigins": ["http://localhost:4200"]
}
```

- `appsettings.json` — non-secret defaults (issuer, audience, expiry)
- `appsettings.Development.json` — local dev values (committed to Git)
- Production secrets → environment variables or secrets manager, never in files

---

## Step 6 – NuGet Security Check

After adding any NuGet packages, always run:

```powershell
dotnet list <solution>.slnx package --vulnerable --include-transitive
```

- Fix all vulnerabilities before continuing
- For transitive vulnerabilities: add the affected package directly at a safe version
- Run again after fixing to confirm 0 vulnerable packages

---

## Step 7 – Project Structure

Default: single project with folders. Do not split into multiple projects unless multiple C# projects need to share the same logic.

```
API/
  Controllers/
  Models/         ← ResultData<T>
Logic/
  {DomainName}/   ← DTOs, enums per domain
  Services/       ← interfaces + implementations
Data/
  AppDbContext.cs
  Entities/
```
