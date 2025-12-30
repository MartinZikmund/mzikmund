# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Personal website and blog platform built with .NET 10 and Uno Platform. The solution consists of:
- **Web Application** (`src/web/`): ASP.NET Core website with blog, API, and admin features
- **Cross-Platform App** (`src/app/`): Uno Platform mobile/desktop application
- **Shared Libraries** (`src/shared/`): Common code and API client
- **Migration Tools** (`src/tools/`): Legacy content migration utilities

## Build Commands

### Build Solution
```bash
cd src
dotnet restore
dotnet build
dotnet build -c Release  # Release build
```

### Run Web Application
```bash
cd src/web/MZikmund.Web
dotnet run
# Accessible at https://localhost:5001
```

### Run Uno Platform App
```bash
cd src/app/MZikmund.App

# WebAssembly (WASM)
dotnet run -f net10.0-browserwasm

# Windows
dotnet run -f net10.0-windows10.0.26100

# Android (requires emulator/device)
dotnet run -f net10.0-android

# iOS (requires Mac with Xcode)
dotnet run -f net10.0-ios

# Desktop (cross-platform)
dotnet run -f net10.0-desktop
```

### Testing
```bash
cd src
dotnet test

# Run specific test project
cd src/web/MZikmund.Web.Core.Tests
dotnet test
```

### Database Migrations
```bash
cd src/web/MZikmund.Web

# Apply migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add MigrationName

# Remove last migration
dotnet ef migrations remove
```

## Architecture

### Web Application Architecture

The web application follows a **vertical slice architecture** with **CQRS** pattern using MediatR:

**Project Structure:**
- `MZikmund.Web`: ASP.NET Core host, Razor Pages, controllers
- `MZikmund.Web.Core`: Business logic organized by features
- `MZikmund.Web.Data`: Entity Framework Core data access
- `MZikmund.Web.Configuration`: Configuration management
- `MZikmund.Web.Core.Tests`: Unit tests

**Feature Organization:**
Features are organized in `src/web/MZikmund.Web.Core/Features/` by domain:
- `Posts/`: Blog post management (CRUD, preview tokens, status management)
- `Categories/`: Category management with post counts
- `Tags/`: Tag management with post counts
- `Files/`: File upload and management
- `Images/`: Image upload and processing

Each feature uses MediatR with:
- **Commands**: Write operations (Create, Update, Delete)
- **Queries**: Read operations (Get, Count, List)
- **Handlers**: Command/query handlers implementing business logic

**Key Services:**
- `IMarkdownConverter`: Converts Markdown to HTML using Markdig
- `IPostContentProcessor`: Processes post content (image optimization, link handling)
- `IFeedGenerator`: Generates RSS/Atom feeds
- `IBlobStorage`: Azure Blob Storage abstraction for files/images
- `ICache`: In-memory caching for performance

**Data Layer:**
- Entity Framework Core with SQL Server
- Code-first migrations in `MZikmund.Web.Data/Migrations/`
- Entities: `PostEntity`, `CategoryEntity`, `TagEntity`, `PostCategoryEntity`, `PostTagEntity`
- Post statuses: `Draft`, `Published`, `Deleted`

### Uno Platform App Architecture

Built with **Uno Platform Single Project** structure targeting multiple platforms.

**Project Structure:**
- `MZikmund.App`: Main application project with UI (XAML views)
- `MZikmund.App.Core`: Core business logic and services
- `MZikmund.DataContracts`: Shared DTOs with web API

**Uno Features Enabled:**
- Hosting, HttpRefit (API client generation)
- Configuration, Serialization
- Mvvm (CommunityToolkit.Mvvm)
- Navigation, Localization
- ThemeService, SkiaRenderer
- Lottie animations, WebView

**Target Frameworks:**
- `net10.0-android`: Android
- `net10.0-ios`: iOS
- `net10.0-browserwasm`: WebAssembly
- `net10.0-desktop`: Cross-platform desktop (Mac, Linux)
- `net10.0-windows10.0.26100`: Windows with WinUI 3

### Authentication Architecture

**Web API:** JWT Bearer tokens via Auth0
- JWT validation in `Program.cs` using `Microsoft.AspNetCore.Authentication.JwtBearer`
- Admin policy requires `permissions` claim with `admin:all` value
- Configuration: `Auth0:Domain` and `Auth0:Audience` in appsettings

**Uno Platform App:** Platform-native OAuth2 with PKCE
- Windows: Uses `WebAuthenticationBroker` (OAuth2Manager API)
- Other platforms: Uses Uno Platform's `WebAuthenticator`
- Callback URI: `https://localhost/callback` (Windows), `dev.mzikmund.MZikmund.App://callback` (mobile)
- See `docs/AUTH0_MIGRATION.md` for detailed setup

### Configuration Management

**Central Package Management (CPM):**
All NuGet package versions are defined in `src/Directory.Packages.props`. Project files use `<PackageReference>` without version numbers.

**Build Configuration:**
- `src/Directory.Build.props`: Shared MSBuild properties
  - Nullable reference types enabled
  - C# preview language features enabled
  - Implicit usings enabled
  - Nerdbank.GitVersioning for automatic versioning

**Application Configuration:**
- Web: `appsettings.json`, User Secrets for local dev
- App: `appsettings.json` embedded in the app
- Sample configuration: `solution-config.props.sample`

### Versioning

Uses **Nerdbank.GitVersioning** for automatic semantic versioning:
- Version based on git height and branch
- Configuration in `version.json` at repository root
- Public releases from `main` branch only
- Version embedded in assemblies and application manifests

## Development Guidelines

### Adding New Features

**Web Application:**
1. Create feature folder in `src/web/MZikmund.Web.Core/Features/{FeatureName}/`
2. Add commands/queries with corresponding handlers
3. Register handlers automatically via MediatR's assembly scanning
4. Add controller in `src/web/MZikmund.Web/Controllers/` if API endpoint needed
5. Add Razor page in `src/web/MZikmund.Web/Pages/` if UI needed

**Uno Platform App:**
1. Add view in `src/app/MZikmund.App/Views/`
2. Add ViewModel in `src/app/MZikmund.App.Core/ViewModels/`
3. Use CommunityToolkit.Mvvm attributes: `[ObservableProperty]`, `[RelayCommand]`
4. Configure navigation in `App.xaml.cs`

### Database Changes

1. Modify entities in `src/web/MZikmund.Web.Data/Entities/`
2. Update `DatabaseContext.cs` if adding new DbSet
3. Create migration: `dotnet ef migrations add MigrationName -p MZikmund.Web.Data -s MZikmund.Web`
4. Review migration in `Migrations/` folder
5. Apply: `dotnet ef database update`

### Testing Patterns

- Use xUnit for all tests
- Mock dependencies with Moq
- Test project: `src/web/MZikmund.Web.Core.Tests/`
- Focus on testing MediatR handlers and business logic
- Integration tests should use `Microsoft.AspNetCore.TestHost`

### Code Style

Enforced via `.editorconfig` at repository root:
- File-scoped namespaces for new files
- Implicit usings enabled globally
- Nullable reference types required
- Use `var` when type is obvious
- Async/await for all I/O operations

## Common Scenarios

### Adding a New Blog Post Field

1. Add property to `PostEntity` in `MZikmund.Web.Data/Entities/PostEntity.cs`
2. Update `PostConfiguration` in `Entities/Configurations/PostConfiguration.cs`
3. Create EF migration
4. Add mapping in `MZikmund.Web.Core/Mappings/` if needed
5. Update commands/queries in `Features/Posts/` to handle new field
6. Update Razor pages or API responses as needed

### Adding New API Endpoint

1. Create command/query in `Features/{Domain}/`
2. Create handler implementing `IRequestHandler`
3. Add controller action in `Controllers/` and decorate with `[Authorize(Policy = "AdminPolicy")]` if admin-only
4. Test with Swagger at `/swagger` in development mode

### Platform-Specific Code in Uno App

Use conditional compilation or platform-specific folders:
```csharp
#if __ANDROID__
// Android-specific code
#elif __IOS__
// iOS-specific code
#elif __WASM__
// WebAssembly-specific code
#endif
```

Or use `Platforms/` folders with appropriate target framework conditions.

### Working with Auth0

**Web API Configuration:**
- Set `Auth0:Domain` (e.g., `your-tenant.auth0.com`) and `Auth0:Audience` in appsettings or User Secrets
- Do not commit actual Auth0 values to source control
- Use environment variables or Azure App Configuration for production

**Uno Platform App:**
- Update `AuthenticationConstants.cs` in app core project with ClientId and Domain
- Ensure redirect URIs are registered in Auth0 application settings
- Windows uses `https://localhost/callback`, mobile uses app scheme

## Important Notes

### Build Warnings Suppressed

The following warnings are globally suppressed in `Directory.Build.props`:
- `NU1507`: Multiple package sources with CPM
- `NETSDK1201`: RID without self-contained app
- `PRI257`: Default language not in included resources
- `CA1873`: .NET 10 structured logging analyzer
- `Uno0001`: Uno Platform warnings

### Temporary Workarounds

- `AllowUnsafeBlocks` enabled for Uno Platform issue #18464
- Custom `UnoGenerateAssetsManifestDependsOn` for Uno.Wasm.Bootstrap compatibility
- Windows startup object override for WinUI 3 targets

### Required SDK and Tools

- .NET 10 SDK (`global.json` specifies exact version: 10.0.100)
- Uno.Sdk 6.5.0-dev.31 or later (specified in `global.json`)
- For mobile: Android SDK 21+, Xcode for iOS/Mac
- For Windows: Windows 10/11 SDK (10.0.26100)
