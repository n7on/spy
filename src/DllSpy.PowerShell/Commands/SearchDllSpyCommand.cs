using System;
using System.Linq;
using System.Management.Automation;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;

namespace DllSpy.PowerShell.Commands
{
    /// <summary>
    /// Discovers input surfaces (HTTP endpoints, SignalR hub methods, etc.) in compiled .NET assemblies.
    /// </summary>
    /// <example>
    /// <code>Search-DllSpy -Path .\MyApi.dll</code>
    /// </example>
    /// <example>
    /// <code>Search-DllSpy -Path .\MyApi.dll -Type HttpEndpoint -HttpMethod GET -Class Users</code>
    /// </example>
    [Cmdlet("Search", "DllSpy")]
    [OutputType(typeof(InputSurface))]
    public class SearchDllSpyCommand : SpyCmdletBase
    {
        /// <summary>
        /// Gets or sets the path to the .NET assembly. Supports wildcards.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("AssemblyPath", "FullName")]
        [ValidateNotNullOrEmpty]
        public string[] Path { get; set; }

        /// <summary>
        /// Gets or sets the surface type filter.
        /// </summary>
        [Parameter]
        public SurfaceType? Type { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method filter (GET, POST, PUT, DELETE, etc.). Applies only to HTTP endpoints.
        /// </summary>
        [Parameter]
        [ValidateSet("GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS")]
        public string HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets whether to filter by surfaces requiring authorization.
        /// </summary>
        [Parameter]
        public SwitchParameter RequiresAuth { get; set; }

        /// <summary>
        /// Gets or sets whether to filter by surfaces allowing anonymous access.
        /// </summary>
        [Parameter]
        public SwitchParameter AllowAnonymous { get; set; }

        /// <summary>
        /// Gets or sets whether to only include host (runnable) assemblies, skipping class libraries.
        /// </summary>
        [Parameter]
        public SwitchParameter HostOnly { get; set; }

        /// <summary>
        /// Gets or sets the class name filter. Supports wildcards.
        /// </summary>
        [Parameter]
        [SupportsWildcards]
        public string Class { get; set; }

        private AssemblyScanner _scanner;

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            _scanner = ScannerFactory.Create();
        }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            foreach (var inputPath in Path)
            {
                var resolvedPaths = ResolvePaths(inputPath);

                foreach (var resolvedPath in resolvedPaths)
                {
                    try
                    {
                        ProcessAssembly(resolvedPath);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(
                            ex,
                            "AssemblyScanError",
                            ErrorCategory.ReadError,
                            resolvedPath));
                    }
                }
            }
        }

        private void ProcessAssembly(string assemblyPath)
        {
            WriteVerbose($"Scanning assembly: {assemblyPath}");

            var report = _scanner.ScanAssembly(assemblyPath);

            if (HostOnly.IsPresent && !report.IsHostAssembly)
                return;

            var surfaces = report.Surfaces.AsEnumerable();

            if (Type.HasValue)
            {
                surfaces = surfaces.Where(s => s.SurfaceType == Type.Value);
            }

            if (!string.IsNullOrEmpty(HttpMethod))
            {
                surfaces = surfaces.Where(s =>
                    (s is HttpEndpoint http && string.Equals(http.HttpMethod, HttpMethod, StringComparison.OrdinalIgnoreCase)) ||
                    (s is ODataEndpoint odata && string.Equals(odata.HttpMethod, HttpMethod, StringComparison.OrdinalIgnoreCase)) ||
                    (s is RazorPageHandler razor && string.Equals(razor.HttpMethod, HttpMethod, StringComparison.OrdinalIgnoreCase)) ||
                    (s is AzureFunction func && string.Equals(func.HttpMethod, HttpMethod, StringComparison.OrdinalIgnoreCase)));
            }

            if (RequiresAuth.IsPresent)
            {
                surfaces = surfaces.Where(s => s.RequiresAuthorization);
            }

            if (AllowAnonymous.IsPresent)
            {
                surfaces = surfaces.Where(s => s.AllowAnonymous);
            }

            if (!string.IsNullOrEmpty(Class))
            {
                var pattern = new WildcardPattern(Class, WildcardOptions.IgnoreCase);
                surfaces = surfaces.Where(s => pattern.IsMatch(s.ClassName));
            }

            foreach (var surface in surfaces)
            {
                WriteObject(surface);
            }

            WriteVerbose($"Found {report.TotalSurfaces} surfaces in {assemblyPath}");
        }
    }
}
