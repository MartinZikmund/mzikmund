# MZikmund.Web Development Guide

## Frontend Asset Build System

This project uses **Vite** for building frontend assets (TypeScript and SCSS). Vite is a modern, cross-platform build tool that works on Windows, Linux, and macOS.

### Prerequisites

- Node.js 20.x or later
- npm (comes with Node.js)

### Building Assets

#### Production Build
To build assets for production (generates both regular and minified files):

```bash
npm run build
```

This will:
- Compile TypeScript to JavaScript (`site.js` and `site.min.js`)
- Compile SCSS to CSS (`site.css` and `site.min.css`)
- Generate source maps for debugging
- Minify the output for optimal performance
- Output files to `wwwroot/js/` and `wwwroot/css/`

#### Integration with .NET Build
The frontend assets are automatically built when you run:

```bash
dotnet build
```

or

```bash
dotnet publish
```

The build process is integrated into the MSBuild pipeline via the `BuildFrontend` target in the `.csproj` file.

### Development with Hot Reload

For development with hot module replacement (HMR), use the Vite dev server:

```bash
npm run dev
```

This will:
- Start a development server on `http://localhost:5173`
- Watch for changes to TypeScript and SCSS files
- Automatically reload the browser when files change
- Provide fast rebuild times

#### Using Hot Reload with ASP.NET Core

1. Start the Vite dev server:
   ```bash
   npm run dev
   ```

2. In a separate terminal, start the ASP.NET Core application:
   ```bash
   dotnet run
   ```

3. The ASP.NET Core app will serve the application, while Vite provides hot reload for frontend assets.

**Note:** For hot reload to work with the ASP.NET Core app, you would need to configure the app to proxy requests to the Vite dev server. Currently, the setup is optimized for production builds during the .NET build process.

### File Structure

```
src/web/MZikmund.Web/
├── Scripts/              # TypeScript source files
│   ├── index.ts         # Main entry point
│   ├── ThemeSwitchManager.ts
│   └── ReadingProgressBar.ts
├── Styles/              # SCSS source files
│   ├── site.scss        # Main stylesheet
│   └── GistTheming.scss
├── wwwroot/             # Output directory (git-ignored)
│   ├── css/
│   │   ├── site.css     # Development CSS
│   │   └── site.min.css # Production CSS
│   └── js/
│       ├── site.js      # Development JavaScript
│       ├── site.js.map  # Source map
│       └── site.min.js  # Production JavaScript
├── vite.config.ts       # Vite configuration
├── build-assets.js      # Custom build script
├── package.json         # Node.js dependencies and scripts
└── tsconfig.json        # TypeScript configuration
```

### What Was Replaced

Previously, this project used:
- **BuildWebCompiler2022** - Windows-only SCSS compiler
- **Microsoft.TypeScript.MSBuild** - TypeScript compiler

These have been replaced with **Vite** which:
- ✅ Works on Windows, Linux, and macOS
- ✅ Provides hot module replacement (HMR)
- ✅ Has built-in minification and optimization
- ✅ Generates source maps for debugging
- ✅ Is actively maintained with modern features
- ✅ Compiles both TypeScript and SCSS in a single tool

### Troubleshooting

#### Build fails with "npm not found"
Make sure Node.js is installed and `npm` is available in your PATH.

#### Assets not updating after changes
Run `npm run build` manually to regenerate the assets, or use `npm run dev` for automatic recompilation.

#### Deprecation warnings from Bootstrap
The warnings about deprecated SCSS features come from Bootstrap's SCSS files and can be safely ignored. They will be addressed in future Bootstrap versions.

### Advanced Configuration

The build configuration can be customized in `vite.config.ts`. Key configuration options:

- **Input files**: Defined in `build.rollupOptions.input`
- **Output paths**: Defined in `build.rollupOptions.output`
- **Minification**: Controlled by `build.minify`
- **Source maps**: Controlled by `build.sourcemap`
- **Dev server port**: Defined in `server.port`

For more information, see the [Vite documentation](https://vitejs.dev/).
