using System;
using System.CommandLine;
using System.Linq;
using System.Text.RegularExpressions;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;

namespace DllSpy.Cli
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var path = new Argument<string>("path") { Description = "Path to the .NET assembly to scan" };
            var scan = new Option<bool>("--scan", "-s") { Description = "Scan for security vulnerabilities" };
            var type = new Option<SurfaceType?>("--type", "-t") { Description = "Filter by surface type" };
            var method = new Option<string>("--method", "-m") { Description = "Filter HTTP endpoints by verb (GET, POST, PUT, DELETE, etc.)" };
            var cls = new Option<string>("--class", "-c") { Description = "Filter by class name (supports * wildcards)" };
            var auth = new Option<bool>("--auth") { Description = "Only show surfaces requiring authorization" };
            var anon = new Option<bool>("--anon") { Description = "Only show surfaces allowing anonymous access" };
            var hostOnly = new Option<bool>("--host-only") { Description = "Only scan host (runnable) assemblies, skip class libraries" };
            var minSev = new Option<SecuritySeverity>("--min-severity") { Description = "Minimum severity for scan mode", DefaultValueFactory = _ => SecuritySeverity.Info };
            var output = new Option<OutputFormat?>("--output", "-o") { Description = "Output format: table, tsv, json (default: table for TTY, tsv for piped)" };

            var root = new RootCommand("Discover input surfaces and security issues in .NET assemblies")
            {
                path, scan, type, method, cls, auth, anon, hostOnly, minSev, output
            };

            root.SetAction(r =>
            {
                try
                {
                    var report = ScannerFactory.Create().ScanAssembly(r.GetValue(path));

                    if (r.GetValue(hostOnly) && !report.IsHostAssembly)
                        return 0;

                    var fmt = r.GetValue(output);

                    return r.GetValue(scan)
                        ? RunScan(report, r.GetValue(type), r.GetValue(minSev), fmt)
                        : RunList(report, r.GetValue(type), r.GetValue(method), r.GetValue(cls),
                            r.GetValue(auth), r.GetValue(anon), fmt);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    return 1;
                }
            });

            if (args.Length == 0)
            {
                PrintLogo();
                args = ["--help"];
            }

            return root.Parse(args).Invoke();
        }

        private static void PrintLogo()
        {
            var useColor = !Console.IsOutputRedirected &&
                           Environment.GetEnvironmentVariable("NO_COLOR") is null;
            var c = useColor ? "\x1B[36m" : "";
            var r = useColor ? "\x1B[0m" : "";

            Console.WriteLine();
            Console.WriteLine("░▒▓███████▓▒░░▒▓█▓▒░      ░▒▓█▓▒░       ░▒▓███████▓▒░▒▓███████▓▒░░▒▓█▓▒░░▒▓█▓▒░");
            Console.WriteLine("░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░      ░▒▓█▓▒░      ░▒▓█▓▒░      ░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░░▒▓█▓▒░");
            Console.WriteLine("░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░      ░▒▓█▓▒░      ░▒▓█▓▒░      ░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░░▒▓█▓▒░");
            Console.WriteLine("░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░      ░▒▓█▓▒░       ░▒▓██████▓▒░░▒▓███████▓▒░ ░▒▓██████▓▒░");
            Console.WriteLine("░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░      ░▒▓█▓▒░             ░▒▓█▓▒░▒▓█▓▒░         ░▒▓█▓▒░");
            Console.WriteLine("░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░      ░▒▓█▓▒░             ░▒▓█▓▒░▒▓█▓▒░         ░▒▓█▓▒░");
            Console.WriteLine("░▒▓███████▓▒░░▒▓████████▓▒░▒▓████████▓▒░▒▓███████▓▒░░▒▓█▓▒░         ░▒▓█▓▒░");
            Console.WriteLine();
        }

        private static int RunList(AssemblyReport report, SurfaceType? typeFilter,
            string methodFilter, string classFilter, bool authOnly, bool anonOnly, OutputFormat? format)
        {
            var surfaces = report.Surfaces.AsEnumerable();

            if (typeFilter.HasValue)
                surfaces = surfaces.Where(s => s.SurfaceType == typeFilter.Value);

            if (methodFilter != null)
                surfaces = surfaces.Where(s =>
                    (s is HttpEndpoint http && string.Equals(http.HttpMethod, methodFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s is ODataEndpoint odata && string.Equals(odata.HttpMethod, methodFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s is AzureFunction func && string.Equals(func.HttpMethod, methodFilter, StringComparison.OrdinalIgnoreCase)));

            if (classFilter != null)
            {
                var regex = "^" + Regex.Escape(classFilter).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                surfaces = surfaces.Where(s => Regex.IsMatch(s.ClassName, regex, RegexOptions.IgnoreCase));
            }

            if (authOnly)
                surfaces = surfaces.Where(s => s.RequiresAuthorization);

            if (anonOnly)
                surfaces = surfaces.Where(s => s.AllowAnonymous);

            OutputWriter.PrintSurfaces(surfaces.ToList(), format);
            return 0;
        }

        private static int RunScan(AssemblyReport report, SurfaceType? typeFilter,
            SecuritySeverity minSeverity, OutputFormat? format)
        {
            var issues = report.SecurityIssues.AsEnumerable();

            if (typeFilter.HasValue)
                issues = issues.Where(i => i.SurfaceType == typeFilter.Value);

            issues = issues.Where(i => i.Severity >= minSeverity);
            var list = issues.OrderByDescending(i => i.Severity).ToList();

            OutputWriter.PrintIssues(list, format);

            var highCount = list.Count(i => i.Severity >= SecuritySeverity.High);
            if (highCount > 0)
            {
                Console.Error.WriteLine($"\nFound {highCount} high-severity issue(s).");
                return 2;
            }

            return 0;
        }
    }
}
