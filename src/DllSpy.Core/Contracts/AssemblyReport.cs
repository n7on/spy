using System;
using System.Collections.Generic;
using System.Linq;

namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Complete scan report for a .NET assembly.
    /// </summary>
    public class AssemblyReport
    {
        /// <summary>Gets or sets the path to the scanned assembly.</summary>
        public string AssemblyPath { get; set; }

        /// <summary>Gets or sets the assembly name.</summary>
        public string AssemblyName { get; set; }

        /// <summary>Gets or sets the timestamp of the scan.</summary>
        public DateTime ScanTimestamp { get; set; }

        /// <summary>Gets or sets all discovered input surfaces.</summary>
        public List<InputSurface> Surfaces { get; set; } = new List<InputSurface>();

        /// <summary>Gets or sets the security issues found.</summary>
        public List<SecurityIssue> SecurityIssues { get; set; } = new List<SecurityIssue>();

        /// <summary>Gets the total number of input surfaces discovered.</summary>
        public int TotalSurfaces => Surfaces.Count;

        /// <summary>Gets the number of HTTP endpoints discovered.</summary>
        public int TotalHttpEndpoints => Surfaces.Count(s => s.SurfaceType == SurfaceType.HttpEndpoint);

        /// <summary>Gets the number of SignalR methods discovered.</summary>
        public int TotalSignalRMethods => Surfaces.Count(s => s.SurfaceType == SurfaceType.SignalRMethod);

        /// <summary>Gets the number of WCF operations discovered.</summary>
        public int TotalWcfOperations => Surfaces.Count(s => s.SurfaceType == SurfaceType.WcfOperation);

        /// <summary>Gets the number of gRPC operations discovered.</summary>
        public int TotalGrpcOperations => Surfaces.Count(s => s.SurfaceType == SurfaceType.GrpcOperation);

        /// <summary>Gets the number of Razor Page handlers discovered.</summary>
        public int TotalRazorPageHandlers => Surfaces.Count(s => s.SurfaceType == SurfaceType.RazorPage);

        /// <summary>Gets the number of Blazor routes discovered.</summary>
        public int TotalBlazorRoutes => Surfaces.Count(s => s.SurfaceType == SurfaceType.BlazorComponent);

        /// <summary>Gets the number of Azure Functions discovered.</summary>
        public int TotalAzureFunctions => Surfaces.Count(s => s.SurfaceType == SurfaceType.AzureFunction);

        /// <summary>Gets the number of distinct classes found.</summary>
        public int TotalClasses => Surfaces.Select(s => s.ClassName).Distinct().Count();

        /// <summary>Gets the number of surfaces requiring authorization.</summary>
        public int AuthenticatedSurfaces => Surfaces.Count(s => s.RequiresAuthorization);

        /// <summary>Gets the number of surfaces allowing anonymous access.</summary>
        public int AnonymousSurfaces => Surfaces.Count(s => s.AllowAnonymous || !s.RequiresAuthorization);

        /// <summary>Gets the total number of security issues found.</summary>
        public int TotalSecurityIssues => SecurityIssues.Count;

        /// <summary>Gets the number of high-severity security issues.</summary>
        public int HighSeverityIssues => SecurityIssues.Count(e => e.Severity == SecuritySeverity.High);

        /// <summary>Gets the number of medium-severity security issues.</summary>
        public int MediumSeverityIssues => SecurityIssues.Count(e => e.Severity == SecuritySeverity.Medium);

        /// <summary>Gets the number of low-severity security issues.</summary>
        public int LowSeverityIssues => SecurityIssues.Count(e => e.Severity == SecuritySeverity.Low);

        /// <inheritdoc />
        public override string ToString() =>
            $"Assembly: {AssemblyName} | Surfaces: {TotalSurfaces} | Issues: {TotalSecurityIssues}";
    }
}
