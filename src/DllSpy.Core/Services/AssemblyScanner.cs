using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DllSpy.Core.Contracts;

namespace DllSpy.Core.Services
{
    /// <summary>
    /// Orchestrates assembly scanning, surface discovery, and security analysis.
    /// </summary>
    public class AssemblyScanner
    {
        private readonly IDiscovery[] _discoveries;

        /// <summary>
        /// Initializes a new instance of <see cref="AssemblyScanner"/>.
        /// </summary>
        internal AssemblyScanner(params IDiscovery[] discoveries)
        {
            if (discoveries == null || discoveries.Length == 0)
                throw new ArgumentException("At least one discovery implementation is required.", nameof(discoveries));
            _discoveries = discoveries;
        }

        /// <inheritdoc />
        public AssemblyReport ScanAssembly(string assemblyPath)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
                throw new ArgumentException("Assembly path cannot be null or empty.", nameof(assemblyPath));

            var fullPath = Path.GetFullPath(assemblyPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Assembly not found: {fullPath}", fullPath);

            var assemblyDir = Path.GetDirectoryName(fullPath);
            var probePaths = GetProbePaths(assemblyDir);

            ResolveEventHandler resolver = (sender, args) => ResolveAssembly(args.Name, probePaths);
            AppDomain.CurrentDomain.AssemblyResolve += resolver;

            try
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(fullPath);
                }
                catch (BadImageFormatException ex)
                {
                    throw new InvalidOperationException($"The file is not a valid .NET assembly: {fullPath}", ex);
                }
                catch (FileLoadException ex)
                {
                    throw new InvalidOperationException($"Failed to load assembly: {fullPath}", ex);
                }

                var report = ScanAssembly(assembly);
                report.AssemblyPath = fullPath;
                return report;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
            }
        }

        private static Assembly ResolveAssembly(string fullName, List<string> probePaths)
        {
            var dllName = new AssemblyName(fullName).Name + ".dll";
            foreach (var dir in probePaths)
            {
                var candidate = Path.Combine(dir, dllName);
                if (File.Exists(candidate))
                {
                    try { return Assembly.LoadFrom(candidate); }
                    catch { /* continue probing */ }
                }
            }
            return null;
        }

        private static List<string> GetProbePaths(string assemblyDir)
        {
            var paths = new List<string> { assemblyDir };

            try
            {
                var sharedDir = FindSharedFrameworkDir();
                if (sharedDir != null)
                {
                    foreach (var framework in Directory.GetDirectories(sharedDir))
                    {
                        try
                        {
                            paths.AddRange(Directory.GetDirectories(framework));
                        }
                        catch { /* skip inaccessible directories */ }
                    }
                }
            }
            catch { /* shared framework discovery is best-effort */ }

            return paths;
        }

        private static string FindSharedFrameworkDir()
        {
            // Strategy 1: Navigate from typeof(object).Assembly.Location
            // e.g. .../shared/Microsoft.NETCore.App/8.0.x/System.Private.CoreLib.dll
            var coreLib = typeof(object).Assembly.Location;
            if (!string.IsNullOrEmpty(coreLib))
            {
                var result = NavigateToSharedDir(coreLib);
                if (result != null) return result;
            }

            // Strategy 2: RuntimeEnvironment.GetRuntimeDirectory()
            // Works even in single-file hosts (e.g. PowerShell 7 on macOS)
            // where typeof(object).Assembly.Location is empty.
            // Returns e.g. /usr/local/share/dotnet/shared/Microsoft.NETCore.App/8.0.x/
            try
            {
                var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                if (!string.IsNullOrEmpty(runtimeDir))
                {
                    var result = NavigateToSharedDir(runtimeDir.TrimEnd(Path.DirectorySeparatorChar));
                    if (result != null) return result;
                }
            }
            catch { /* best-effort */ }

            // Strategy 3: DOTNET_ROOT environment variable
            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (!string.IsNullOrEmpty(dotnetRoot))
            {
                var shared = Path.Combine(dotnetRoot, "shared");
                if (Directory.Exists(shared))
                    return shared;
            }

            // Strategy 4: Well-known install locations
            var candidates = new[]
            {
                "/usr/local/share/dotnet",
                "/usr/share/dotnet",
                "/opt/homebrew/share/dotnet",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet")
            };

            foreach (var candidate in candidates)
            {
                var shared = Path.Combine(candidate, "shared");
                if (Directory.Exists(shared))
                    return shared;
            }

            return null;
        }

        private static string NavigateToSharedDir(string pathInsideShared)
        {
            // Walk up from a path like .../shared/Microsoft.NETCore.App/8.0.x/Something
            // looking for a directory named "shared" that contains framework subdirectories
            var dir = Directory.Exists(pathInsideShared) ? pathInsideShared : Path.GetDirectoryName(pathInsideShared);
            while (dir != null)
            {
                if (Path.GetFileName(dir) == "shared" && Directory.Exists(dir))
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        /// <inheritdoc />
        public AssemblyReport ScanAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var surfaces = new List<InputSurface>();
            foreach (var discovery in _discoveries)
            {
                surfaces.AddRange(discovery.Discover(assembly));
            }

            var securityIssues = AnalyzeSecurityIssues(surfaces);

            return new AssemblyReport
            {
                AssemblyPath = assembly.Location,
                AssemblyName = assembly.GetName().Name,
                ScanTimestamp = DateTime.UtcNow,
                Surfaces = surfaces,
                SecurityIssues = securityIssues
            };
        }

        /// <summary>
        /// Analyzes all input surfaces for security issues.
        /// </summary>
        public List<SecurityIssue> AnalyzeSecurityIssues(List<InputSurface> surfaces)
        {
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));

            var issues = new List<SecurityIssue>();

            foreach (var surface in surfaces)
            {
                switch (surface)
                {
                    case HttpEndpoint http:
                        issues.AddRange(AnalyzeHttpEndpoint(http));
                        break;
                    case SignalRMethod signalr:
                        issues.AddRange(AnalyzeSignalRMethod(signalr));
                        break;
                    case WcfOperation wcf:
                        issues.AddRange(AnalyzeWcfOperation(wcf));
                        break;
                    case GrpcOperation grpc:
                        issues.AddRange(AnalyzeGrpcOperation(grpc));
                        break;
                    case RazorPageHandler razor:
                        issues.AddRange(AnalyzeRazorPageHandler(razor));
                        break;
                    case BlazorRoute blazor:
                        issues.AddRange(AnalyzeBlazorRoute(blazor));
                        break;
                    case AzureFunction func:
                        issues.AddRange(AnalyzeAzureFunction(func));
                        break;
                }
            }

            return issues;
        }

        private static List<SecurityIssue> AnalyzeHttpEndpoint(HttpEndpoint endpoint)
        {
            var issues = new List<SecurityIssue>();

            // HIGH: State-changing endpoints (DELETE, POST, PUT, PATCH) without [Authorize]
            if (IsStateChangingMethod(endpoint.HttpMethod) && !endpoint.RequiresAuthorization && !endpoint.AllowAnonymous)
            {
                issues.Add(new SecurityIssue
                {
                    Title = $"Unauthenticated {endpoint.HttpMethod} endpoint",
                    Description = $"The {endpoint.HttpMethod} endpoint '{endpoint.Route}' on {endpoint.ClassName}.{endpoint.MethodName} " +
                                  $"does not require authentication. State-changing operations should be protected.",
                    Severity = SecuritySeverity.High,
                    SurfaceRoute = endpoint.DisplayRoute,
                    SurfaceType = SurfaceType.HttpEndpoint,
                    ClassName = endpoint.ClassName,
                    MethodName = endpoint.MethodName,
                    Recommendation = $"Add [Authorize] attribute to the {endpoint.MethodName} action or the {endpoint.ClassName}Controller class."
                });
            }

            // MEDIUM: Endpoints without [Authorize] or [AllowAnonymous] (unclear intent)
            if (!endpoint.RequiresAuthorization && !endpoint.AllowAnonymous && !IsStateChangingMethod(endpoint.HttpMethod))
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Missing authorization declaration",
                    Description = $"The endpoint '{endpoint.Route}' on {endpoint.ClassName}.{endpoint.MethodName} " +
                                  $"has neither [Authorize] nor [AllowAnonymous]. Security intent is unclear.",
                    Severity = SecuritySeverity.Medium,
                    SurfaceRoute = endpoint.DisplayRoute,
                    SurfaceType = SurfaceType.HttpEndpoint,
                    ClassName = endpoint.ClassName,
                    MethodName = endpoint.MethodName,
                    Recommendation = "Add [Authorize] or [AllowAnonymous] to explicitly declare the security intent."
                });
            }

            // LOW: [Authorize] without roles or policies (broad access)
            if (endpoint.RequiresAuthorization && !endpoint.AllowAnonymous &&
                endpoint.Roles.Count == 0 && endpoint.Policies.Count == 0)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Authorize without role or policy restriction",
                    Description = $"The endpoint '{endpoint.Route}' on {endpoint.ClassName}.{endpoint.MethodName} " +
                                  $"requires authentication but does not specify roles or policies. Any authenticated user can access it.",
                    Severity = SecuritySeverity.Low,
                    SurfaceRoute = endpoint.DisplayRoute,
                    SurfaceType = SurfaceType.HttpEndpoint,
                    ClassName = endpoint.ClassName,
                    MethodName = endpoint.MethodName,
                    Recommendation = "Consider adding Roles or Policy to the [Authorize] attribute to restrict access."
                });
            }

            return issues;
        }

        private static List<SecurityIssue> AnalyzeSignalRMethod(SignalRMethod method)
        {
            var issues = new List<SecurityIssue>();

            // HIGH: Unauthenticated hub method (direct invocation surface)
            if (!method.RequiresAuthorization && !method.AllowAnonymous)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Unauthenticated SignalR hub method",
                    Description = $"The hub method '{method.HubRoute}/{method.MethodName}' on {method.HubName} " +
                                  $"does not require authentication. Hub methods are directly invocable by clients.",
                    Severity = SecuritySeverity.High,
                    SurfaceRoute = method.DisplayRoute,
                    SurfaceType = SurfaceType.SignalRMethod,
                    ClassName = method.ClassName,
                    MethodName = method.MethodName,
                    Recommendation = $"Add [Authorize] attribute to the {method.MethodName} method or the {method.HubName} class."
                });
            }

            // LOW: [Authorize] without roles or policies
            if (method.RequiresAuthorization && !method.AllowAnonymous &&
                method.Roles.Count == 0 && method.Policies.Count == 0)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Authorize without role or policy restriction",
                    Description = $"The hub method '{method.HubRoute}/{method.MethodName}' on {method.HubName} " +
                                  $"requires authentication but does not specify roles or policies. Any authenticated user can invoke it.",
                    Severity = SecuritySeverity.Low,
                    SurfaceRoute = method.DisplayRoute,
                    SurfaceType = SurfaceType.SignalRMethod,
                    ClassName = method.ClassName,
                    MethodName = method.MethodName,
                    Recommendation = "Consider adding Roles or Policy to the [Authorize] attribute to restrict access."
                });
            }

            return issues;
        }

        private static List<SecurityIssue> AnalyzeWcfOperation(WcfOperation operation)
        {
            var issues = new List<SecurityIssue>();

            // HIGH: Unauthenticated WCF operation (direct invocation surface)
            if (!operation.RequiresAuthorization && !operation.AllowAnonymous)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Unauthenticated WCF operation",
                    Description = $"The WCF operation '{operation.ContractName}/{operation.MethodName}' on {operation.ClassName} " +
                                  $"does not require authentication. WCF operations are directly invocable by clients.",
                    Severity = SecuritySeverity.High,
                    SurfaceRoute = operation.DisplayRoute,
                    SurfaceType = SurfaceType.WcfOperation,
                    ClassName = operation.ClassName,
                    MethodName = operation.MethodName,
                    Recommendation = $"Add [PrincipalPermission] attribute to the {operation.ClassName} class or its methods."
                });
            }

            // LOW: [PrincipalPermission] / [Authorize] without roles or policies
            if (operation.RequiresAuthorization && !operation.AllowAnonymous &&
                operation.Roles.Count == 0 && operation.Policies.Count == 0)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Authorize without role or policy restriction",
                    Description = $"The WCF operation '{operation.ContractName}/{operation.MethodName}' on {operation.ClassName} " +
                                  $"requires authentication but does not specify roles or policies. Any authenticated user can invoke it.",
                    Severity = SecuritySeverity.Low,
                    SurfaceRoute = operation.DisplayRoute,
                    SurfaceType = SurfaceType.WcfOperation,
                    ClassName = operation.ClassName,
                    MethodName = operation.MethodName,
                    Recommendation = "Consider adding Role to the [PrincipalPermission] attribute to restrict access."
                });
            }

            return issues;
        }

        private static List<SecurityIssue> AnalyzeGrpcOperation(GrpcOperation operation)
        {
            var issues = new List<SecurityIssue>();

            // HIGH: Unauthenticated gRPC operation (direct invocation surface)
            if (!operation.RequiresAuthorization && !operation.AllowAnonymous)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Unauthenticated gRPC operation",
                    Description = $"The gRPC operation '{operation.ServiceName}/{operation.MethodName}' on {operation.ClassName} " +
                                  $"does not require authentication. gRPC operations are directly invocable by clients.",
                    Severity = SecuritySeverity.High,
                    SurfaceRoute = operation.DisplayRoute,
                    SurfaceType = SurfaceType.GrpcOperation,
                    ClassName = operation.ClassName,
                    MethodName = operation.MethodName,
                    Recommendation = $"Add [Authorize] attribute to the {operation.ClassName} class or its methods."
                });
            }

            // LOW: [Authorize] without roles or policies
            if (operation.RequiresAuthorization && !operation.AllowAnonymous &&
                operation.Roles.Count == 0 && operation.Policies.Count == 0)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Authorize without role or policy restriction",
                    Description = $"The gRPC operation '{operation.ServiceName}/{operation.MethodName}' on {operation.ClassName} " +
                                  $"requires authentication but does not specify roles or policies. Any authenticated user can invoke it.",
                    Severity = SecuritySeverity.Low,
                    SurfaceRoute = operation.DisplayRoute,
                    SurfaceType = SurfaceType.GrpcOperation,
                    ClassName = operation.ClassName,
                    MethodName = operation.MethodName,
                    Recommendation = "Consider adding Roles or Policy to the [Authorize] attribute to restrict access."
                });
            }

            return issues;
        }

        private static List<SecurityIssue> AnalyzeRazorPageHandler(RazorPageHandler handler)
        {
            var issues = new List<SecurityIssue>();

            // HIGH: State-changing handlers (POST, PUT, DELETE, PATCH) without [Authorize]
            if (IsStateChangingMethod(handler.HttpMethod) && !handler.RequiresAuthorization && !handler.AllowAnonymous)
            {
                issues.Add(new SecurityIssue
                {
                    Title = $"Unauthenticated {handler.HttpMethod} Razor Page handler",
                    Description = $"The {handler.HttpMethod} handler '{handler.DisplayRoute}' on {handler.PageModelName} " +
                                  $"does not require authentication. State-changing operations should be protected.",
                    Severity = SecuritySeverity.High,
                    SurfaceRoute = handler.DisplayRoute,
                    SurfaceType = SurfaceType.RazorPage,
                    ClassName = handler.ClassName,
                    MethodName = handler.MethodName,
                    Recommendation = $"Add [Authorize] attribute to the {handler.PageModelName} class or the handler method."
                });
            }

            // MEDIUM: Handlers without [Authorize] or [AllowAnonymous] (unclear intent)
            if (!handler.RequiresAuthorization && !handler.AllowAnonymous && !IsStateChangingMethod(handler.HttpMethod))
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Missing authorization declaration",
                    Description = $"The Razor Page handler '{handler.DisplayRoute}' on {handler.PageModelName} " +
                                  $"has neither [Authorize] nor [AllowAnonymous]. Security intent is unclear.",
                    Severity = SecuritySeverity.Medium,
                    SurfaceRoute = handler.DisplayRoute,
                    SurfaceType = SurfaceType.RazorPage,
                    ClassName = handler.ClassName,
                    MethodName = handler.MethodName,
                    Recommendation = "Add [Authorize] or [AllowAnonymous] to explicitly declare the security intent."
                });
            }

            // LOW: [Authorize] without roles or policies (broad access)
            if (handler.RequiresAuthorization && !handler.AllowAnonymous &&
                handler.Roles.Count == 0 && handler.Policies.Count == 0)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Authorize without role or policy restriction",
                    Description = $"The Razor Page handler '{handler.DisplayRoute}' on {handler.PageModelName} " +
                                  $"requires authentication but does not specify roles or policies. Any authenticated user can access it.",
                    Severity = SecuritySeverity.Low,
                    SurfaceRoute = handler.DisplayRoute,
                    SurfaceType = SurfaceType.RazorPage,
                    ClassName = handler.ClassName,
                    MethodName = handler.MethodName,
                    Recommendation = "Consider adding Roles or Policy to the [Authorize] attribute to restrict access."
                });
            }

            return issues;
        }

        private static List<SecurityIssue> AnalyzeBlazorRoute(BlazorRoute route)
        {
            var issues = new List<SecurityIssue>();

            // HIGH: Unauthenticated routable component (direct navigation surface)
            if (!route.RequiresAuthorization && !route.AllowAnonymous)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Unauthenticated Blazor routable component",
                    Description = $"The Blazor component '{route.ComponentName}' at '{route.RouteTemplate}' " +
                                  $"does not require authentication. Routable components are directly navigable by users.",
                    Severity = SecuritySeverity.High,
                    SurfaceRoute = route.DisplayRoute,
                    SurfaceType = SurfaceType.BlazorComponent,
                    ClassName = route.ClassName,
                    MethodName = route.MethodName,
                    Recommendation = $"Add [Authorize] attribute to the {route.ComponentName} component."
                });
            }

            // LOW: [Authorize] without roles or policies
            if (route.RequiresAuthorization && !route.AllowAnonymous &&
                route.Roles.Count == 0 && route.Policies.Count == 0)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Authorize without role or policy restriction",
                    Description = $"The Blazor component '{route.ComponentName}' at '{route.RouteTemplate}' " +
                                  $"requires authentication but does not specify roles or policies. Any authenticated user can access it.",
                    Severity = SecuritySeverity.Low,
                    SurfaceRoute = route.DisplayRoute,
                    SurfaceType = SurfaceType.BlazorComponent,
                    ClassName = route.ClassName,
                    MethodName = route.MethodName,
                    Recommendation = "Consider adding Roles or Policy to the [Authorize] attribute to restrict access."
                });
            }

            return issues;
        }

        private static List<SecurityIssue> AnalyzeAzureFunction(AzureFunction func)
        {
            var issues = new List<SecurityIssue>();

            // HIGH: Anonymous auth level and no ASP.NET Core authorization
            if (string.Equals(func.AuthorizationLevel, "Anonymous", StringComparison.OrdinalIgnoreCase)
                && !func.RequiresAuthorization && !func.AllowAnonymous)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Unauthenticated Azure Function",
                    Description = $"The Azure Function '{func.FunctionName}' at '{func.Route ?? func.FunctionName}' " +
                                  $"uses AuthorizationLevel.Anonymous and has no [Authorize] attribute. Anyone can invoke it.",
                    Severity = SecuritySeverity.High,
                    SurfaceRoute = func.DisplayRoute,
                    SurfaceType = SurfaceType.AzureFunction,
                    ClassName = func.ClassName,
                    MethodName = func.MethodName,
                    Recommendation = "Set a higher AuthorizationLevel or add [Authorize] attribute."
                });
            }

            // LOW: Has auth but no roles/policies
            if (func.RequiresAuthorization && !func.AllowAnonymous &&
                func.Roles.Count == 0 && func.Policies.Count == 0)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Authorize without role or policy restriction",
                    Description = $"The Azure Function '{func.FunctionName}' at '{func.Route ?? func.FunctionName}' " +
                                  $"requires authentication but does not specify roles or policies. Any authenticated caller can invoke it.",
                    Severity = SecuritySeverity.Low,
                    SurfaceRoute = func.DisplayRoute,
                    SurfaceType = SurfaceType.AzureFunction,
                    ClassName = func.ClassName,
                    MethodName = func.MethodName,
                    Recommendation = "Consider adding Roles or Policy to the [Authorize] attribute to restrict access."
                });
            }

            return issues;
        }

        private static bool IsStateChangingMethod(string httpMethod)
        {
            return httpMethod == "POST" || httpMethod == "PUT" || httpMethod == "DELETE" || httpMethod == "PATCH";
        }
    }
}
