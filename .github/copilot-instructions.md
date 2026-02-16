# GitHub Copilot Instructions

## Project Overview
This is Martin Zikmund's personal website and blog, built as a multi-platform solution using modern .NET technologies. The project consists of a web application, cross-platform mobile/desktop apps, and shared libraries.

## Architecture & Technology Stack

### Core Technologies
- **.NET 10** - Latest version with preview language features enabled
- **ASP.NET Core** - Web application framework
- **Uno Platform** - Cross-platform UI framework for mobile/desktop apps
- **Entity Framework Core** - Data access layer
- **Microsoft Identity Platform** - Authentication and authorization
- **TypeScript/JavaScript** - Client-side development
- **GitHub Actions** - CI/CD workflows

### Target Platforms
- Web (ASP.NET Core)
- Android (via Uno Platform)
- iOS (via Uno Platform)
- WebAssembly (via Uno Platform)
- Desktop (Windows, Mac, Linux via Uno Platform)
- Windows 10/11 (WinUI 3 via Uno Platform)

## Project Structure

### Main Components
- `src/web/` - ASP.NET Core web application and related projects
- `src/app/` - Uno Platform cross-platform application
- `src/shared/` - Shared libraries and APIs
- `src/tools/` - Utility tools and migration scripts
- `.github/` - GitHub Actions workflows and repository configuration

### Key Files
- `global.json` - .NET SDK version configuration
- `Directory.Build.props` - Centralized MSBuild properties
- `Directory.Packages.props` - Central Package Management (CPM)
- `MZikmund.slnx` - Solution file (XML format, in repository root)

## Coding Standards & Conventions

### General Guidelines
- **Nullable reference types** are enabled globally
- Use **implicit usings** for common namespaces
- **C# preview language features** are enabled
- Follow **async/await** patterns consistently
- Use **dependency injection** for service registration

### Code Style
- Use meaningful, descriptive names for variables, methods, and classes
- Follow C# naming conventions (PascalCase for public members, camelCase for private fields)
- Prefer `var` when the type is obvious from context
- Use file-scoped namespaces for new files
- Keep methods focused and follow single responsibility principle

### Package Management
- All package versions are managed centrally via `Directory.Packages.props`
- When adding new packages, add them to the central package management file
- Use `PackageReference` without version numbers in project files

## Project-Specific Patterns

### Web Application (`src/web/`)
- Uses **MediatR** for CQRS pattern implementation
- **AutoMapper** for object mapping
- **Application Insights** for telemetry
- **JWT Bearer** and **OpenID Connect** for authentication
- **Entity Framework Core** with design-time support
- **Swagger/OpenAPI** for API documentation

### Cross-Platform App (`src/app/`)
- Built with **Uno Platform** using single project structure
- Uses **Uno Features** for capability management:
  - Hosting, HttpRefit, Configuration
  - Serialization, MVVM, Navigation
  - Authentication (MSAL), Localization
  - Theme Service, Skia Renderer, Lottie animations

### Authentication
- Microsoft Identity Platform integration
- Use `Microsoft.Identity.Web` packages
- JWT Bearer tokens for API authentication
- OpenID Connect for web authentication

## Testing Guidelines

### Test Structure
- Use xUnit as the testing framework
- Mock external dependencies using appropriate mocking frameworks

### Test Coverage
- Focus on business logic and critical paths
- Test both positive and negative scenarios
- Include integration tests for API endpoints
- Test cross-platform compatibility when applicable

## Build & Deployment

### Local Development
- Ensure .NET 10 SDK is installed
- Use `dotnet restore` to restore packages
- Use `dotnet build` to build the solution
- Use `dotnet run` for local development

### GitHub Actions
- **Continuous Integration** - Automated builds and tests
- **Azure Deployment** - Web app deployment to Azure
- **Package Creation** - Windows app packaging
- **Static Web Apps** - Deployment for static assets

### Configuration
- Use `appsettings.json` for configuration
- Leverage User Secrets for local development
- Environment-specific settings in `appsettings.{Environment}.json`
- Azure App Configuration for production settings

## Security Considerations

### Authentication & Authorization
- Always use Microsoft Identity Platform for authentication
- Implement proper authorization policies
- Use secure token handling practices
- Validate all user inputs

### Data Protection
- Use HTTPS for all communications
- Implement proper CORS policies
- Follow OWASP security guidelines
- Use Entity Framework's built-in SQL injection protection

## Development Workflow

### Branching Strategy
- Create feature branches for new development
- Use descriptive branch names
- Ensure all changes are tested before merging

### Code Reviews
- All changes should go through pull request reviews
- Ensure code follows established patterns
- Verify that new dependencies are appropriate
- Check for security implications

## Common Operations

### Adding New Features
1. Create appropriate project structure if needed
2. Add necessary package references to `Directory.Packages.props`
3. Implement using established patterns (MediatR, DI, etc.)
4. Add appropriate tests
5. Update documentation if needed

### Cross-Platform Considerations
- Test on multiple platforms when using Uno Platform features
- Consider platform-specific implementations when needed
- Use platform-conditional compilation when necessary
- Leverage Uno Platform's adaptive UI capabilities

### Performance
- Use async/await for I/O operations
- Implement proper caching strategies
- Consider memory usage in mobile scenarios
- Use efficient data access patterns with EF Core

## Troubleshooting

### Common Issues
- **Build Errors**: Check .NET 10 SDK installation and version compatibility
- **Package Issues**: Verify Central Package Management configuration
- **Authentication**: Ensure proper Microsoft Identity Platform setup
- **Cross-Platform**: Check Uno Platform documentation for platform-specific issues

### Debug Configuration
- Use portable debug symbols
- Enable source link for better debugging experience
- Leverage Application Insights for production troubleshooting