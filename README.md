# Martin Zikmund

Personal website and blog built with modern .NET technologies and Uno Platform.

## 📖 About

This is a full-featured personal website and blog platform built with .NET 9, ASP.NET Core, and Uno Platform. The project includes:

- **Web Application**: A full-featured ASP.NET Core website and blog with authentication, content management, and API endpoints
- **Cross-Platform App**: Native mobile and desktop applications for Android, iOS, Windows, Mac, Linux, and WebAssembly using Uno Platform
- **Shared Libraries**: Reusable components and APIs shared across all platforms
- **Migration Tools**: Utilities for content migration and data management

## 🚀 Technology Stack

### Core Technologies
- **.NET 9** - Latest .NET framework with C# preview language features
- **ASP.NET Core** - Web application framework
- **Uno Platform** - Cross-platform UI framework for building native mobile, desktop, and web apps
- **Entity Framework Core** - Object-relational mapper for database access
- **Microsoft Identity Platform** - Authentication and authorization (via Auth0)
- **MediatR** - Mediator pattern implementation for CQRS
- **AutoMapper** - Object-to-object mapping
- **Azure Services** - Application Insights, Blob Storage

### Frontend Technologies
- **TypeScript/JavaScript**
- **Markdown** - Content authoring with Markdig parser
- **Swagger/OpenAPI** - API documentation

### Supported Platforms
- 🌐 Web (ASP.NET Core)
- 🤖 Android
- 🍎 iOS
- 🖥️ Desktop (Windows, Mac, Linux)
- 🪟 Windows 10/11 (WinUI 3)
- 🌍 WebAssembly

## 📋 Prerequisites

Before setting up the project, ensure you have the following installed:

- **.NET 9 SDK** or later - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Visual Studio 2022** (17.12+) or **Visual Studio Code** with C# Dev Kit extension
  - For Uno Platform development, install the Uno Platform workload
- **Node.js and npm** - For TypeScript compilation and frontend tooling
- **SQL Server** or **SQL Server Express** - For the database backend
- **(Optional) Docker** - For containerized development

### Platform-Specific Requirements

For mobile and cross-platform development:
- **Android**: Android SDK 21+ (installed via Visual Studio or Android Studio)
- **iOS/Mac**: macOS with Xcode (for iOS/Mac development)
- **Windows**: Windows 10/11 SDK (for WinUI development)

## 🛠️ Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/MartinZikmund/mzikmund.git
cd mzikmund
```

### 2. Restore Dependencies

```bash
cd src
dotnet restore
```

### 3. Configure Application Settings

The web application requires configuration for database connection, authentication, and Azure services.

1. Copy the sample configuration file:
   ```bash
   cp solution-config.props.sample solution-config.props
   ```

2. Update `solution-config.props` with your configuration values

3. For local development, use User Secrets for sensitive data:
   ```bash
   cd web/MZikmund.Web
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
   ```

### 4. Database Setup

The project uses Entity Framework Core with SQL Server:

```bash
cd src/web/MZikmund.Web
dotnet ef database update
```

This will create the necessary database schema based on the migrations.

## 🏃‍♂️ Running the Application

### Web Application

To run the web application locally:

```bash
cd src/web/MZikmund.Web
dotnet run
```

The application will be available at `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP).

### Cross-Platform App

To run the Uno Platform application:

```bash
cd src/app/MZikmund.App

# For WebAssembly
dotnet run -f net10.0-browserwasm

# For Windows
dotnet run -f net10.0-windows10.0.26100

# For Android (requires Android emulator or device)
dotnet run -f net10.0-android

# For iOS (requires Mac with Xcode)
dotnet run -f net10.0-ios
```

Alternatively, open the solution in Visual Studio and select the desired target framework and platform.

## 🏗️ Building the Application

### Build All Projects

```bash
cd src
dotnet build
```

### Build for Specific Configuration

```bash
# Debug build
dotnet build -c Debug

# Release build
dotnet build -c Release
```

### Build Specific Platform

```bash
cd src/app/MZikmund.App
dotnet build -f net10.0-browserwasm -c Release
```

## 🧪 Running Tests

The solution includes unit tests for the web core functionality:

```bash
cd src
dotnet test
```

## 📁 Project Structure

```
mzikmund/
├── .github/              # GitHub Actions CI/CD workflows
├── assets/               # Static assets and resources
├── docs/                 # Documentation files
│   ├── AUTH0_MIGRATION.md
│   └── AzureNotes.md
├── src/
│   ├── app/              # Uno Platform cross-platform application
│   │   ├── MZikmund.App/           # Main application project
│   │   ├── MZikmund.App.Core/      # Core application logic
│   │   └── MZikmund.DataContracts/ # Shared data contracts
│   ├── shared/           # Shared libraries
│   │   ├── MZikmund.Api/           # API client library
│   │   └── MZikmund.Shared/        # Shared utilities
│   ├── tools/            # Utility tools
│   │   └── MZikmund.LegacyMigration/ # Content migration tool
│   ├── web/              # ASP.NET Core web application
│   │   ├── MZikmund.Web/             # Main web project
│   │   ├── MZikmund.Web.Configuration/ # Configuration management
│   │   ├── MZikmund.Web.Core/        # Business logic
│   │   ├── MZikmund.Web.Core.Tests/  # Unit tests
│   │   └── MZikmund.Web.Data/        # Data access layer
│   ├── Directory.Build.props      # Shared MSBuild properties
│   ├── Directory.Packages.props   # Central package management
│   └── MZikmund.slnx             # Solution file
├── .editorconfig         # Code style configuration
├── global.json           # .NET SDK version
├── LICENSE               # MIT License
└── README.md             # This file
```

## 🚀 Deployment

The project includes GitHub Actions workflows for automated deployment:

- **azure-publish.yml** - Deploys the web application to Azure App Service
- **package-windows.yml** - Builds and packages the Windows application
- **static-web-apps-deploy.yml** - Deploys static assets to Azure Static Web Apps
- **fullbuild.yml** - Runs full build validation

## 🤝 Contributing

Contributions are welcome! This is a personal project, but if you find bugs or have suggestions:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please follow the existing code style and conventions defined in `.editorconfig`.

## 📝 Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md).

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

```
Copyright (c) 2022 Martin Zikmund
```

## 👤 Author

**Martin Zikmund**

- Website: [mzikmund.dev](https://mzikmund.dev) *(powered by this project)*
- GitHub: [@MartinZikmund](https://github.com/MartinZikmund)

## 🙏 Acknowledgments

- Built with [Uno Platform](https://platform.uno/)
- Powered by [.NET](https://dotnet.microsoft.com/)
- Hosted on [Azure](https://azure.microsoft.com/)

## 📚 Additional Resources

- [Uno Platform Documentation](https://platform.uno/docs/articles/intro.html)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core/)

---

*This README was last updated: November 2025*
