using System;
using System.Linq;
using System.Management.Automation;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;

namespace DllSpy.PowerShell.Commands
{
    /// <summary>
    /// Analyzes .NET assemblies for security vulnerabilities in input surfaces.
    /// </summary>
    /// <example>
    /// <code>Test-DllSpy -Path .\MyApi.dll</code>
    /// </example>
    /// <example>
    /// <code>Test-DllSpy -Path .\MyApi.dll -MinimumSeverity High</code>
    /// </example>
    [Cmdlet(VerbsDiagnostic.Test, "DllSpy")]
    [OutputType(typeof(SecurityIssue))]
    public class TestDllSpyCommand : SpyCmdletBase
    {
        /// <summary>
        /// Gets or sets the path to the .NET assembly. Supports wildcards.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("AssemblyPath", "FullName")]
        [ValidateNotNullOrEmpty]
        public string[] Path { get; set; }

        /// <summary>
        /// Gets or sets the minimum severity level to report.
        /// </summary>
        [Parameter]
        public SecuritySeverity MinimumSeverity { get; set; } = SecuritySeverity.Info;

        /// <summary>
        /// Gets or sets whether to only include host (runnable) assemblies, skipping class libraries.
        /// </summary>
        [Parameter]
        public SwitchParameter HostOnly { get; set; }

        /// <summary>
        /// Gets or sets the surface type filter for issues.
        /// </summary>
        [Parameter]
        public SurfaceType? Type { get; set; }

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
                            "VulnerabilityScanError",
                            ErrorCategory.ReadError,
                            resolvedPath));
                    }
                }
            }
        }

        private void ProcessAssembly(string assemblyPath)
        {
            WriteVerbose($"Analyzing security for: {assemblyPath}");

            var report = _scanner.ScanAssembly(assemblyPath);

            if (HostOnly.IsPresent && !report.IsHostAssembly)
                return;

            var issues = report.SecurityIssues.AsEnumerable();

            if (Type.HasValue)
            {
                issues = issues.Where(i => i.SurfaceType == Type.Value);
            }

            issues = issues
                .Where(i => i.Severity >= MinimumSeverity)
                .OrderByDescending(i => i.Severity);

            foreach (var issue in issues)
            {
                WriteObject(issue);
            }

            var highCount = report.SecurityIssues.Count(i => i.Severity >= SecuritySeverity.High);
            if (highCount > 0)
            {
                WriteWarning($"Found {highCount} high-severity issue(s) in {assemblyPath}");
            }

            WriteVerbose($"Found {report.TotalSecurityIssues} security issue(s) in {assemblyPath}");
        }
    }
}
