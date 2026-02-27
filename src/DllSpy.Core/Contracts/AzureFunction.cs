namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Represents a discovered Azure Functions HTTP-triggered function.
    /// </summary>
    public class AzureFunction : InputSurface
    {
        /// <inheritdoc />
        public override SurfaceType SurfaceType => SurfaceType.AzureFunction;

        /// <summary>Gets or sets the function name from [FunctionName] or [Function].</summary>
        public string FunctionName { get; set; }

        /// <summary>Gets or sets the route template from [HttpTrigger].</summary>
        public string Route { get; set; }

        /// <summary>Gets or sets the HTTP method (GET, POST, etc.) or null for any.</summary>
        public string HttpMethod { get; set; }

        /// <summary>Gets or sets the authorization level (Anonymous, Function, Admin, etc.).</summary>
        public string AuthorizationLevel { get; set; }

        /// <inheritdoc />
        public override string DisplayRoute =>
            $"{HttpMethod ?? "ANY"} {Route ?? FunctionName}";
    }
}
