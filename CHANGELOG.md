## [Unreleased]

## [0.2.8] - 2026-03-04

### Added
- `IsHostAssembly` property on `AssemblyReport` — detects whether a scanned assembly is a runnable host (has an entry point or `.runtimeconfig.json` sidecar) vs. a class library
- `-HostOnly` switch on `Search-DllSpy` and `Test-DllSpy` — silently skips non-host assemblies
- `--host-only` option on CLI — silently skips non-host assemblies

## [0.2.7] - 2026-02-27

### Added
- **Azure Functions discovery** — detects `[FunctionName]` / `[Function]` methods with `[HttpTrigger]` parameters, extracts route, HTTP method, and `AuthorizationLevel`
- `AzureFunction` surface type with `FunctionName`, `Route`, `HttpMethod`, `AuthorizationLevel` properties
- Azure Functions security rules: unauthenticated function (High), authorize without role/policy (Low)
- `TotalAzureFunctions` computed property on `AssemblyReport`
- CLI type label `Func` and `--method` filter support for Azure Functions
- PowerShell formatter views for `AzureFunction` (table + list)
- **OData endpoint discovery** — detects controllers inheriting from `ODataController`, resolves routes from `[ODataRoutePrefix]` / `[Route]` / `odata/{entitySet}` convention, detects `[EnableQuery]`
- `ODataEndpoint` surface type with `Route`, `HttpMethod`, `EntitySetName`, `HasEnableQuery` properties
- OData security rules: unauthenticated state-changing endpoint (High), missing auth declaration (Medium), authorize without role/policy (Low)
- `TotalODataEndpoints` computed property on `AssemblyReport`
- CLI type label `OData` and `--method` filter support for OData endpoints
- PowerShell formatter views for `ODataEndpoint` (table + list)
- PowerShell `-HttpMethod` filter now also matches `ODataEndpoint`, `RazorPageHandler`, and `AzureFunction` (previously only `HttpEndpoint`)
- OData controllers excluded from HTTP endpoint discovery (no duplicates)

## [0.2.6] - 2026-02-26

### Fixed
- Assembly resolution on macOS PowerShell 7 — `Search-DllSpy` returned 0 surfaces because single-file hosts report empty `Assembly.Location`, preventing shared framework discovery. Now uses `RuntimeEnvironment.GetRuntimeDirectory()` as fallback, plus well-known dotnet install paths (including `/opt/homebrew/share/dotnet` for Apple Silicon)

### Added
- Pester integration tests (`tests/PowerShell/DllSpy.Tests.ps1`)

## [0.2.5] - 2026-02-26

### Added
- Unified `InputSurface` PowerShell formatter view so mixed surface types render consistently with `Format-Table` (`Type | Method | Route | Class | Action | Auth`)
- Demo GIF in README

## [0.2.4] - 2026-02-26

### Changed
- CLI surface table now shows separate **METHOD** and **ROUTE** columns instead of combined `DisplayRoute`; renamed the C# method name column from METHOD to **ACTION**
- New column order: `TYPE | METHOD | ROUTE | CLASS | ACTION | AUTH`

### Fixed
- PowerShell formatter TypeNames used wrong namespace (`Spy.Core.Contracts` instead of `DllSpy.Core.Contracts`), causing `Format-Table` to fall back to raw property dump
- Remaining `Spy` references in help XML and cmdlet base class comment updated to `DllSpy`

### Added
- PowerShell formatter views for `RazorPageHandler` (table + list) and `BlazorRoute` (table + list)

## [0.2.3] - 2026-02-26

### Added
- `--output`/`-o` option on CLI with `table`, `tsv`, `json` formats (auto-detects: table for TTY, tsv for piped)
- Table output truncates columns to fit terminal width

### Changed
- `--json` flag replaced by `--output json` / `-o json`

## [0.2.2] - 2026-02-25

### Added
- **Razor Page handler discovery** — detects `PageModel` subclasses, parses `On{Verb}[Handler][Async]` methods, infers routes from namespace segments after `Pages`, scans `[BindProperty]` properties as parameters
- **Blazor routable component discovery** — detects `ComponentBase` subclasses with `[Route]` attributes, extracts `[Parameter]` properties, creates one surface per route (supports multi-route components)
- `RazorPageHandler` surface type with `PageRoute`, `HttpMethod`, `HandlerName`, `PageModelName` properties
- `BlazorRoute` surface type with `RouteTemplate`, `ComponentName` properties
- `RazorPage` and `BlazorComponent` values added to `SurfaceType` enum
- Razor Page security rules: unauthenticated state-changing handler (High), missing auth declaration (Medium), authorize without role/policy (Low)
- Blazor security rules: unauthenticated routable component (High), authorize without role/policy (Low)
- `TotalRazorPageHandlers` and `TotalBlazorRoutes` computed properties on `AssemblyReport`
- CLI type labels: `Razor` and `Blazor` in surface output tables

## [0.2.1] - 2026-02-25

### Added
- **CLI tool** (`DllSpy.Cli`) — standalone `dotnet tool` for scanning assemblies from the command line

### Changed
- Renamed solution and all projects from `Spy` to `DllSpy` (`DllSpy.Core`, `DllSpy.PowerShell`, `DllSpy.Core.Tests`)
- Renamed namespaces from `Spy.*` to `DllSpy.*`

## [0.2.0] - 2026-02-24

### Added
- **WCF service operation discovery** — detects `[ServiceContract]` interfaces, resolves implementing classes via `IsAssignableFrom`, extracts `[OperationContract]` methods
- **gRPC service operation discovery** — detects services inheriting from generated gRPC base classes (via `BindService` detection), identifies all four streaming modes (Unary, ServerStreaming, ClientStreaming, BidiStreaming)
- `WcfOperation` surface type with `ContractName`, `ServiceNamespace`, `IsOneWay` properties
- `GrpcOperation` surface type with `ServiceName`, `MethodType` properties
- `[PrincipalPermission]` recognized as an authorization attribute (WCF security model)
- WCF security rules: unauthenticated operation (High), authorize without role (Low)
- gRPC security rules: unauthenticated operation (High), authorize without role/policy (Low)
- `TotalWcfOperations` and `TotalGrpcOperations` computed properties on `AssemblyReport`
- Custom formatting views for `WcfOperation` and `GrpcOperation` (table and list)
- Contract-only WCF detection (interface without implementation class)

## [0.1.0] - 2026-02-24

### Added
- **Multi-surface discovery architecture** — pluggable `IDiscovery` interface for discovering different input surface types
- **SignalR hub method discovery** — detects hubs inheriting from `Hub` or `Hub<T>`, extracts callable methods, streaming detection
- `SurfaceType` enum (`HttpEndpoint`, `SignalRMethod`) for categorizing discovered surfaces
- `InputSurface` abstract base class with shared properties across all surface types
- `HttpEndpoint` — extends `InputSurface` with route, HTTP method, and route details
- `SignalRMethod` — extends `InputSurface` with hub route, hub name, and streaming flags
- `SignalRDiscovery` service implementing `IDiscovery` for SignalR hubs
- `-Type` parameter on `Search-DllSpy` and `Test-DllSpy` for filtering by surface type
- `-Class` parameter on `Search-DllSpy` (replaces `-Controller`) for filtering by class name with wildcards
- SignalR security rules: unauthenticated hub method (High), authorize without roles (Low)
- Custom formatting views for `SignalRMethod` (table and list)
- Sample SignalR hubs (`ChatHub`, `NotificationHub`) in `samples/SampleApi.cs`

### Changed
- Renamed `Get-SpyEndpoint` to `Search-DllSpy`
- Renamed `EndpointDiscovery` to `HttpEndpointDiscovery`, now implements `IDiscovery`
- `AssemblyScanner` now accepts `params IDiscovery[]` — runs all discoveries and aggregates results
- `AssemblyReport.Endpoints` replaced by `AssemblyReport.Surfaces` with new computed properties (`TotalSurfaces`, `TotalHttpEndpoints`, `TotalSignalRMethods`, `TotalClasses`)
- `SecurityIssue` fields generalized: `EndpointRoute`/`HttpMethod` → `SurfaceRoute`, `ControllerName`/`ActionName` → `ClassName`/`MethodName`, added `SurfaceType`
- Formatting views updated for `HttpEndpoint` (was `EndpointInfo`) and `SecurityIssue`

### Removed
- `EndpointInfo` class (replaced by `HttpEndpoint`)
- `Get-SpyEndpoint` cmdlet (replaced by `Search-DllSpy`)
