namespace DllSpy.Core.Services
{
    /// <summary>
    /// Creates a fully wired <see cref="AssemblyScanner"/> with all discovery implementations.
    /// </summary>
    public static class ScannerFactory
    {
        /// <summary>
        /// Creates an <see cref="AssemblyScanner"/> configured with all built-in discovery types.
        /// </summary>
        public static AssemblyScanner Create()
        {
            var analyzer = new AttributeAnalyzer();
            return new AssemblyScanner(
                new HttpEndpointDiscovery(analyzer),
                new SignalRDiscovery(analyzer),
                new WcfDiscovery(analyzer),
                new GrpcDiscovery(analyzer),
                new RazorPageDiscovery(analyzer),
                new BlazorDiscovery(analyzer),
                new AzureFunctionDiscovery(analyzer));
        }
    }
}
