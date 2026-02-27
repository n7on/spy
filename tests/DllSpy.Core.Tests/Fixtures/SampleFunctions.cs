namespace DllSpy.Core.Tests.Fixtures
{
    // Anonymous HTTP-triggered functions (no class-level auth)
    public class ProductFunctions
    {
        [FunctionName("GetProducts")]
        public void GetProducts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] object req) { }

        [FunctionName("CreateProduct")]
        public void CreateProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] object req) { }

        [FunctionName("GetProductById")]
        public void GetProductById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{id}")] object req, string id) { }
    }

    // Class-level [Authorize] with roles
    [Authorize(Roles = "Admin")]
    public class OrderFunctions
    {
        [Function("GetOrders")]
        public void GetOrders(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] object req) { }

        [Function("PlaceOrder")]
        public void PlaceOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] object req) { }
    }

    // Class-level [Authorize] without roles + method-level [AllowAnonymous] override
    [Authorize]
    public class NotificationFunctions
    {
        [FunctionName("SendNotification")]
        public void SendNotification(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications")] object req) { }

        [AllowAnonymous]
        [FunctionName("GetNotifications")]
        public void GetNotifications(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notifications")] object req) { }
    }

    // Non-HTTP trigger — should be excluded
    public class TimerFunctions
    {
        [FunctionName("Cleanup")]
        public void Cleanup(object timer) { }
    }

    // Isolated worker model [Function] attribute
    public class HealthFunctions
    {
        [Function("HealthCheck")]
        public void HealthCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] object req) { }
    }
}
