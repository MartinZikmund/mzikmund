<!--
  Sync Impact Report
  ===================
  Version change: N/A (initial) → 1.0.0
  Modified principles: N/A (initial creation)
  Added sections:
    - Core Principles (5 principles)
    - Technology Stack & Constraints
    - Development Workflow
    - Governance
  Removed sections: N/A
  Templates requiring updates:
    - .specify/templates/plan-template.md — ✅ compatible (Constitution Check
      section already references constitution file generically)
    - .specify/templates/spec-template.md — ✅ compatible (no constitution-
      specific references to update)
    - .specify/templates/tasks-template.md — ✅ compatible (phase structure
      aligns with principles)
    - .specify/templates/checklist-template.md — ✅ compatible (generic
      template, no constitution refs)
  Follow-up TODOs: None
-->

# MZikmund Constitution

## Core Principles

### I. Vertical Slice Architecture

All web application features MUST be organized as vertical slices under
`Features/{Domain}/` in `MZikmund.Web.Core`. Each slice contains its own
commands, queries, and handler implementations registered via MediatR
assembly scanning. Controllers MUST delegate to MediatR handlers and
MUST NOT contain business logic directly.

**Rationale**: Vertical slices keep related code co-located, reduce
coupling between features, and make each domain independently
understandable and testable.

### II. Cross-Platform Shared Contracts

All data transfer objects (DTOs) MUST reside in `MZikmund.DataContracts`.
API client interfaces MUST reside in `MZikmund.Api` using Refit. The Uno
Platform app and web API MUST share these contracts — no duplicated
models across projects. Platform-specific code MUST be isolated to the
platform-specific project and MUST NOT leak into shared libraries.

**Rationale**: A single source of truth for API contracts prevents
drift between client and server, reduces bugs from serialization
mismatches, and enables type-safe API consumption across all targets.

### III. Security by Default

All admin endpoints MUST use `[Authorize(Policy = "AdminPolicy")]`.
Authentication MUST use JWT Bearer tokens via Auth0 on the web API and
platform-native OAuth2 with PKCE on the Uno app. Secrets and credentials
MUST NOT be committed to source control. CORS MUST be explicitly
configured per consumer (e.g., `WasmAppPolicy`).

**Rationale**: Security is a non-negotiable baseline. Explicit
authorization policies and industry-standard auth flows protect user
data and prevent accidental exposure of admin functionality.

### IV. Code Quality & Consistency

All code MUST conform to the `.editorconfig` rules: Allman braces,
file-scoped namespaces, tab indentation, and the established naming
conventions (`_camelCase` for private fields, `IPascalCase` for
interfaces). Performance analyzer warnings MUST be treated as errors.
All package versions MUST be managed centrally via
`Directory.Packages.props`.

**Rationale**: Uniform style eliminates formatting debates, central
package management prevents version conflicts, and strict analyzer
enforcement catches performance issues at compile time rather than
in production.

### V. Simplicity & Pragmatism

New features MUST start with the simplest viable implementation. Avoid
abstractions, patterns, or infrastructure that serve hypothetical future
requirements (YAGNI). Database changes MUST use EF Core migrations with
Fluent API configuration. Build complexity MUST be justified — prefer
`solution-config.props` overrides over complex MSBuild logic.

**Rationale**: This is a personal project. Over-engineering increases
maintenance burden without proportional benefit. Every layer of
indirection MUST earn its keep by solving a concrete, present problem.

## Technology Stack & Constraints

- **Runtime**: .NET 10 (exact SDK version pinned in `global.json`)
- **Web Framework**: ASP.NET Core with Razor Pages and Web API
- **ORM**: Entity Framework Core with SQL Server
- **App Framework**: Uno Platform (Uno.Sdk 6.5.0-dev.31) targeting
  Android, iOS, macOS, Linux, Windows, and WebAssembly
- **MVVM Toolkit**: CommunityToolkit.Mvvm (`[ObservableProperty]`,
  `[RelayCommand]`)
- **API Client**: Refit with shared `MZikmund.DataContracts`
- **Authentication**: Auth0 (JWT on server, OAuth2/PKCE on client)
- **CI/CD**: GitHub Actions (build, test, Azure deploy, MSIX packaging)
- **Versioning**: Nerdbank.GitVersioning (SemVer from `version.json`)
- **Hosting**: Azure App Service (web), Azure Static Web Apps (WASM),
  Azure Blob Storage (media files)

New dependencies MUST be added to `Directory.Packages.props`. Framework
or SDK version changes MUST update `global.json` and be validated by CI.

## Development Workflow

- **Branching**: Feature branches off `main`. Release branches follow
  `release/v{version}` pattern. Public releases from `main` only.
- **Testing**: xUnit with Moq. Tests reside in dedicated test projects
  (e.g., `MZikmund.Web.Core.Tests`). New business logic SHOULD have
  corresponding unit tests.
- **Database changes**: Migrations MUST be created from the
  `MZikmund.Web` directory with `-p ../MZikmund.Web.Data -s .` flags.
  Auto-migration runs at startup in development.
- **Code review**: All changes to `main` and `release/*` branches MUST
  pass the `fullbuild.yml` CI workflow (build + test).
- **Commit discipline**: Commits MUST be atomic and descriptive. Avoid
  bundling unrelated changes in a single commit.

## Governance

This constitution defines the authoritative engineering principles for
the MZikmund project. All feature specifications, implementation plans,
and task lists produced by speckit tooling MUST be consistent with these
principles.

**Amendment procedure**:
1. Propose changes with rationale in a constitution update PR.
2. Version MUST be incremented per semantic versioning: MAJOR for
   principle removals/redefinitions, MINOR for new principles or
   material expansions, PATCH for clarifications and wording fixes.
3. All dependent templates MUST be reviewed for consistency after
   amendment (see Sync Impact Report at top of this file).

**Compliance**: The plan template's "Constitution Check" section MUST
validate that each feature plan aligns with the principles defined here.
Violations MUST be documented in the plan's Complexity Tracking table
with justification.

**Version**: 1.0.0 | **Ratified**: 2026-02-11 | **Last Amended**: 2026-02-11
