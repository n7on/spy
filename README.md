# DllSpy

[![CI](https://github.com/n7on/dllspy/actions/workflows/ci.yml/badge.svg)](https://github.com/n7on/dllspy/actions/workflows/ci.yml) [![NuGet Version](https://img.shields.io/nuget/v/DllSpy)](https://www.nuget.org/packages/DllSpy) [![PowerShell Gallery Version](https://img.shields.io/powershellgallery/v/DllSpy)](https://www.powershellgallery.com/packages/DllSpy) [![License](https://img.shields.io/github/license/n7on/dllspy)](https://github.com/n7on/dllspy/blob/main/LICENSE)

Scans compiled .NET assemblies to discover input surfaces (HTTP endpoints, SignalR hubs, WCF services, gRPC services, Razor Pages, Blazor components), check authorization configuration, and flag security issues — all without running the application.

Available as a **CLI tool** and a **PowerShell module**.

![DllSpy demo](docs/dllspy.gif)

## Installation

### CLI

```bash
dotnet tool install -g DllSpy
```

### PowerShell

```powershell
Install-Module -Name DllSpy
```

## Usage

### CLI

```bash
# List all surfaces
dllspy ./MyApi.dll

# Scan for security vulnerabilities
dllspy ./MyApi.dll -s

# Filter by surface type (HttpEndpoint, SignalRMethod, WcfOperation, GrpcOperation, RazorPage, BlazorComponent)
dllspy ./MyApi.dll -t HttpEndpoint

# Filter by HTTP method and class name
dllspy ./MyApi.dll -m DELETE -c User*

# Only authenticated / anonymous surfaces
dllspy ./MyApi.dll --auth
dllspy ./MyApi.dll --anon

# Scan with minimum severity
dllspy ./MyApi.dll -s --min-severity High

# Output format: table (default), tsv, json
dllspy ./MyApi.dll -o json
dllspy ./MyApi.dll -o tsv
```

### PowerShell

```powershell
# All surfaces
Search-DllSpy -Path .\MyApi.dll

# Filter by surface type
Search-DllSpy -Path .\MyApi.dll -Type HttpEndpoint
Search-DllSpy -Path .\MyApi.dll -Type SignalRMethod
Search-DllSpy -Path .\MyApi.dll -Type WcfOperation
Search-DllSpy -Path .\MyApi.dll -Type GrpcOperation
Search-DllSpy -Path .\MyApi.dll -Type RazorPage
Search-DllSpy -Path .\MyApi.dll -Type BlazorComponent

# Filter by HTTP method
Search-DllSpy -Path .\MyApi.dll -HttpMethod DELETE

# Filter by class name (supports wildcards)
Search-DllSpy -Path .\MyApi.dll -Class User*

# Only authenticated / anonymous surfaces
Search-DllSpy -Path .\MyApi.dll -RequiresAuth
Search-DllSpy -Path .\MyApi.dll -AllowAnonymous

# Find security issues
Test-DllSpy -Path .\MyApi.dll

# Only high-severity issues
Test-DllSpy -Path .\MyApi.dll -MinimumSeverity High

# Detailed view
Test-DllSpy -Path .\MyApi.dll | Format-List
```

## Supported Frameworks

| Framework | Detection Method | Surface Type |
|-----------|-----------------|--------------|
| **ASP.NET Core / Web API** | Controller base class, `[ApiController]`, naming convention | `HttpEndpoint` |
| **SignalR** | `Hub` / `Hub<T>` inheritance | `SignalRMethod` |
| **WCF** | `[ServiceContract]` interfaces + `[OperationContract]` methods | `WcfOperation` |
| **gRPC** | Generated base class with `BindService` | `GrpcOperation` |
| **Razor Pages** | `PageModel` inheritance, `On{Verb}[Handler][Async]` methods | `RazorPage` |
| **Blazor** | `ComponentBase` inheritance + `[Route]` attribute | `BlazorComponent` |

## Security Rules

### HTTP Endpoints

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated state-changing endpoint | `DELETE`, `POST`, `PUT`, or `PATCH` without `[Authorize]` |
| **Medium** | Missing authorization declaration | Endpoint has neither `[Authorize]` nor `[AllowAnonymous]` |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

### SignalR Hub Methods

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated hub method | Hub method without `[Authorize]` (directly invocable by clients) |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

### WCF Operations

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated WCF operation | Operation without `[PrincipalPermission]` or `[Authorize]` |
| **Low** | Authorize without role | `[PrincipalPermission]` present but no `Role` specified |

### gRPC Operations

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated gRPC operation | Service method without `[Authorize]` |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

### Razor Page Handlers

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated state-changing handler | `POST`, `PUT`, `DELETE`, or `PATCH` handler without `[Authorize]` |
| **Medium** | Missing authorization declaration | Handler has neither `[Authorize]` nor `[AllowAnonymous]` |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

### Blazor Routable Components

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated routable component | Component with `[Route]` but without `[Authorize]` |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

## License

See [LICENSE](LICENSE).
