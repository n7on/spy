namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Identifies the kind of input surface discovered in an assembly.
    /// </summary>
    public enum SurfaceType
    {
        /// <summary>An HTTP endpoint on an ASP.NET Core / Web API controller.</summary>
        HttpEndpoint,
        /// <summary>A callable method on a SignalR hub.</summary>
        SignalRMethod,
        /// <summary>An operation on a WCF service contract.</summary>
        WcfOperation,
        /// <summary>An operation on a gRPC service.</summary>
        GrpcOperation,
        /// <summary>A handler on a Razor Page.</summary>
        RazorPage,
        /// <summary>A routable Blazor component.</summary>
        BlazorComponent,
        /// <summary>An Azure Functions HTTP-triggered function.</summary>
        AzureFunction
    }
}
