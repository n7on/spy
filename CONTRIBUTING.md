# Contributing to DllSpy

If you want to contribute to the source you're highly welcome!

## Prerequisites

* [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
* PowerShell 5.1+ or PowerShell 7+

## Build

```powershell
dotnet build
```

For a release build:

```powershell
dotnet build -c Release
```

## Test

```powershell
dotnet test
```

## Load the Module Locally

```powershell
Import-Module ./out/DllSpy
Search-DllSpy -Path ./MyApi.dll
```

## Project Structure

```
dllspy/
├── src/
│   ├── DllSpy.Core/            # Core library (netstandard2.0)
│   │   ├── Contracts/          # Data models
│   │   ├── Helpers/            # Reflection utilities
│   │   └── Services/           # Discovery and analysis logic
│   ├── DllSpy.PowerShell/      # PowerShell module (netstandard2.0)
│   │   ├── Commands/           # Cmdlets
│   │   └── Formatters/         # ps1xml formatting
│   └── DllSpy.Cli/             # CLI tool (net8.0)
├── tests/
│   └── DllSpy.Core.Tests/      # xUnit tests
│       ├── Fixtures/            # Fake ASP.NET/Razor/Blazor types and sample controllers/hubs/pages/components
│       ├── Helpers/             # ReflectionHelper tests
│       └── Services/            # Discovery, scanner, and analyzer tests
├── docs/                        # Documentation
├── Directory.Build.props        # Shared version for all .NET projects
└── DllSpy.sln
```

## How It Works

DllSpy loads .NET assemblies via `System.Reflection` and scans for types that represent input surfaces:

**HTTP Endpoints** — Classes inheriting from `ControllerBase`, `Controller`, or `ApiController`; classes with `[ApiController]`; or classes named `*Controller` with public action methods. Routes are resolved by combining controller-level and action-level templates, with support for `[controller]` and `[action]` tokens.

**SignalR Hub Methods** — Classes inheriting from `Hub` or `Hub<T>`. Public instance methods are discovered, excluding lifecycle methods like `OnConnectedAsync`. Routes use conventional naming (strip "Hub" suffix) since actual `MapHub<T>("/route")` calls aren't discoverable via reflection.

**Razor Page Handlers** — Classes inheriting from `PageModel`. Public methods matching the `On{Verb}[Handler][Async]` pattern are discovered (e.g. `OnPostDeleteAsync` → POST with handler name "Delete"). Page routes are inferred from namespace segments after `Pages` plus the class name minus the `Model` suffix. Properties with `[BindProperty]` are included as form parameters.

**Blazor Routable Components** — Classes inheriting from `ComponentBase` that have one or more `[Route]` attributes. Each route template produces a separate surface. Properties with `[Parameter]` are included as parameters. Non-routable components (no `[Route]`) are excluded.

**WCF Operations** — Interfaces with `[ServiceContract]` and their `[OperationContract]` methods. Implementing classes are resolved via `IsAssignableFrom`. Supports `[PrincipalPermission]` as an authorization attribute.

**gRPC Operations** — Classes inheriting from generated gRPC base classes (detected via `BindService` method). Identifies all four streaming modes (Unary, ServerStreaming, ClientStreaming, BidiStreaming).

**Azure Functions** — Methods with `[FunctionName]` or `[Function]` attributes that have an `[HttpTrigger]` parameter. Extracts the route, HTTP method(s), and `AuthorizationLevel` from the trigger attribute.

**OData Endpoints** — Classes inheriting from `ODataController`. Routes are resolved from `[ODataRoutePrefix]`, falling back to `[Route]`, then `odata/{entitySet}` convention. Method routes check `[ODataRoute]`, then HTTP attribute templates. The `[EnableQuery]` attribute is tracked per method.

## Adding a New Discovery Type

To add a new surface type (e.g. minimal APIs):

1. Add the type to `SurfaceType` enum in `src/DllSpy.Core/Contracts/SurfaceType.cs`
2. Create a new class extending `InputSurface` in `src/DllSpy.Core/Contracts/`
3. Create a new `IDiscovery` implementation in `src/DllSpy.Core/Services/`
4. Add security analysis rules in `AssemblyScanner.AnalyzeSecurityIssues`
5. Wire it up in `ScannerFactory.Create()`
6. Add formatting views in `DllSpy.Format.ps1xml`

## Releasing a New Version

1. Update version in `Directory.Build.props` (shared by all .NET projects):
   ```xml
   <Version>0.3.0</Version>
   ```

2. Update version in `src/DllSpy.PowerShell/DllSpy.psd1` (can't inherit from MSBuild):
   ```powershell
   ModuleVersion = '0.3.0'
   ```

3. Update release notes in `src/DllSpy.PowerShell/DllSpy.psd1`

4. Update `CHANGELOG.md`

5. Commit and push the changes

6. Create and push a git tag:
   ```bash
   git tag v0.2.0
   git push origin v0.2.0
   ```

GitHub Actions will automatically publish to NuGet and PowerShell Gallery when a tag is pushed.
