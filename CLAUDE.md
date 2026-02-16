# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Personal website and blog platform built with .NET 10 and Uno Platform. The solution (`MZikmund.slnx`, XML solution format, in repository root) consists of:
- **Web Application** (`src/web/`): ASP.NET Core website with blog, API, and admin features
- **Cross-Platform App** (`src/app/`): Uno Platform mobile/desktop application
- **Shared Libraries** (`src/shared/`): Refit API client (`MZikmund.Api`), shared utilities (`MZikmund.Shared`), DTOs (`MZikmund.DataContracts`)
- **Migration Tools** (`src/tools/`): WinUI 3 legacy content migration tool with OpenAI integration

## Build Commands

### Build Solution
```bash
dotnet restore
dotnet build
```

### Run Web Application
```bash
cd src/web/MZikmund.Web
npm install   # Required first time for frontend dependencies
dotnet run
# Accessible at https://localhost:5001
```

### Run Uno Platform App
```bash
cd src/app/MZikmund.App
dotnet run -f net10.0-desktop              # Desktop (Windows, Mac, Linux)
dotnet run -f net10.0-windows10.0.26100    # Windows (WinUI 3)
dotnet run -f net10.0-browserwasm          # WebAssembly
dotnet run -f net10.0-android              # Android
dotnet run -f net10.0-ios                  # iOS (requires Mac)
```

### Testing
```bash
dotnet test                                # All tests
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"  # Single test

# Or run specific test project
cd src/web/MZikmund.Web.Core.Tests
dotnet test
```

### Verifying Web App Changes
After making changes to the web application, run the app locally and use the Playwright MCP to verify the changes visually in the browser (navigate to `https://localhost:5001`, take snapshots/screenshots, and confirm the UI renders correctly).

### Database Migrations
```bash
cd src/web/MZikmund.Web

# Apply migrations
dotnet ef database update

# Create new migration (must specify both projects)
dotnet ef migrations add MigrationName -p ../MZikmund.Web.Data -s .

# Remove last migration
dotnet ef migrations remove -p ../MZikmund.Web.Data -s .
```

### Faster Local Builds
Copy `solution-config.props.sample` to `solution-config.props` and uncomment overrides to limit which platforms Visual Studio/CLI builds (e.g., build only Windows or only WASM).

## Architecture

### Web Application (CQRS with MediatR)

Vertical slice architecture organized by feature domain:

- `MZikmund.Web`: ASP.NET Core host, Razor Pages, controllers, middleware
- `MZikmund.Web.Core`: Business logic in `Features/` (Posts, Categories, Tags, Files, Images)
- `MZikmund.Web.Data`: Entity Framework Core with SQL Server
- `MZikmund.Web.Configuration`: Configuration POCO classes
- `MZikmund.Web.Core.Tests`: xUnit tests with Moq

**Feature pattern** (each domain in `Features/{Domain}/`):
- Commands: `CreatePost`, `UpdatePost`, `DeletePost` with `IRequestHandler` implementations
- Queries: `GetPosts`, `GetPostById`, `GetPostByRouteName`, etc.
- Handlers registered automatically via MediatR assembly scanning

**Controllers** are split into public and admin:
- `Controllers/`: Public endpoints (PostsController, CategoriesController, TagsController, SyndicationController)
- `Controllers/Admin/`: Admin endpoints with `[Authorize(Policy = "AdminPolicy")]` (PostsAdminController, CategoriesAdminController, TagsAdminController, FilesAdminController, ImagesAdminController)

**Data layer entities**: `PostEntity`, `CategoryEntity`, `TagEntity`, `PostCategoryEntity`, `PostTagEntity`, `PostRevisionEntity`. Post statuses: `Draft`, `Published`, `Deleted`. EF Core Fluent API configurations in `Entities/Configurations/`.

**Key services**: `IMarkdownConverter` (Markdig), `IPostContentProcessor` (image optimization, link handling), `IFeedGenerator` (RSS/Atom/OPML via `Syndication/`), `IBlobStorage` (Azure Blob Storage), `ICache` (in-memory), `IDateProvider` (testable DateTime abstraction).

**Middleware**: `ReallySimpleDiscoveryMiddleware` (RSD endpoint), `LegacyUrlRedirectRule` (URL rewriting for old blog URLs).

**Startup** (`Program.cs`): Auto-migrates database, configures CORS policy `"WasmAppPolicy"` for WASM app, JWT Bearer auth with Auth0, Swagger in Development mode.

### Uno Platform App (MVVM + Host-based)

Single project structure targeting Android, iOS, WASM, Desktop, Windows.

- `MZikmund.App`: UI layer (XAML views in `Views/`), uses `WindowShell` pattern for main window
- `MZikmund.App.Core`: ViewModels (CommunityToolkit.Mvvm with `[ObservableProperty]`, `[RelayCommand]`), services
- `MZikmund.DataContracts`: Shared DTOs between web API and app

Use Uno APP MCP to test the desktop app.

**App initialization** (`App.xaml.cs`): Host-based architecture using Uno.Extensions. Configures embedded `appsettings.json`, logging, Refit HTTP client with JWT token refresh, and dependency injection for all ViewModels and services.

**API client**: `MZikmund.Api` project uses Refit to generate typed HTTP client from interface definitions, shared via `MZikmund.DataContracts`.

**UnoFeatures**: Hosting, HttpRefit, Configuration, Serialization, Mvvm, Navigation, Localization, ThemeService, SkiaRenderer, Lottie, WebView, Logging.

**Windows-specific**: Custom `StartupObject` (`MZikmundProgram`), OAuth2Manager callback handling via `OnUriCallback`.

### Authentication

**Web API**: JWT Bearer tokens via Auth0. Admin policy requires `permissions` claim with `admin:all`. Config keys: `Auth0:Domain`, `Auth0:Audience`.

**Uno App**: Platform-native OAuth2 with PKCE. Windows uses `OAuth2Manager`, other platforms use Uno's `WebAuthenticator`. Callback URIs: `https://localhost/callback` (Windows), `dev.mzikmund.MZikmund.App://callback` (mobile). See `docs/AUTH0_MIGRATION.md`.

### Package and Build Configuration

- **Central Package Management**: All versions in `src/Directory.Packages.props`. Project files use `<PackageReference>` without version numbers.
- **Shared properties** (`src/Directory.Build.props`): Nullable enabled, C# preview language, implicit usings, Nerdbank.GitVersioning.
- **Versioning** (`version.json`): Nerdbank.GitVersioning, base version 1.0, public releases from `main` only, release branches: `release/v{version}`.
- **SDK**: .NET 10.0.100 exact (`global.json`), Uno.Sdk 6.5.0-dev.31.

### CI/CD Workflows (`.github/workflows/`)

- `fullbuild.yml`: Builds and tests full solution on push/PR to main/release branches
- `azure-publish.yml`: Deploys web app to Azure App Service (main branch, OIDC auth)
- `package-windows.yml`: Creates MSIX packages (manual trigger, x86/x64/ARM64 matrix)
- `static-web-apps-deploy.yml`: Deploys Uno WASM app to Azure Static Web Apps
- `todo.yml`: Converts TODO comments to GitHub issues

## Code Style

Enforced via `.editorconfig`:
- **Indentation**: Tabs (width 4) for code; spaces for CSS/YAML (width 2)
- **Braces**: Allman style (`csharp_new_line_before_open_brace = all`)
- **Namespaces**: File-scoped (`csharp_style_namespace_declarations = file_scoped:warning`)
- **var**: Preferred when type is obvious
- **Naming**: Interfaces `IPascalCase`, type params `TPascalCase`, private fields `_camelCase`, constants `PascalCase`, params/locals `camelCase`
- **Performance analyzers**: Set to error level
- **Line endings**: CRLF

## Adding New Features

**Web Application:**
1. Create feature folder in `src/web/MZikmund.Web.Core/Features/{FeatureName}/`
2. Add commands/queries with `IRequestHandler` implementations (auto-registered by MediatR)
3. Add controller in `Controllers/` (or `Controllers/Admin/` with `[Authorize(Policy = "AdminPolicy")]`)
4. Add Razor page in `Pages/` if UI needed

**Database changes:**
1. Modify entities in `MZikmund.Web.Data/Entities/`
2. Add Fluent API configuration in `Entities/Configurations/`
3. Update `DatabaseContext.cs` if adding new DbSet
4. Create migration: `dotnet ef migrations add MigrationName -p ../MZikmund.Web.Data -s .` (from `MZikmund.Web/`)

**Uno Platform App:**
1. Add view in `src/app/MZikmund.App/Views/`
2. Add ViewModel in `src/app/MZikmund.App.Core/ViewModels/`
3. Register ViewModel in `App.xaml.cs` DI container
4. Configure navigation

## Final Steps

Always run `dotnet format MZikmund.slnx` at the end of your work to ensure consistent code formatting.

## Important Notes

### Suppressed Warnings
Globally suppressed in `Directory.Build.props`: `NU1507`, `NETSDK1201`, `PRI257`, `XAOBS001`, `CA1873`. `Uno0001` set as non-error.

### Workarounds
- `AllowUnsafeBlocks` enabled for Uno Platform issue #18464
- Custom `UnoGenerateAssetsManifestDependsOn` for Uno.Wasm.Bootstrap compatibility
- Windows startup object override for WinUI 3 targets
