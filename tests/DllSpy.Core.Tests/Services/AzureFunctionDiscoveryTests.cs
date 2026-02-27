using System.Linq;
using System.Reflection;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;
using DllSpy.Core.Tests.Fixtures;
using Xunit;

namespace DllSpy.Core.Tests.Services
{
    public class AzureFunctionDiscoveryTests
    {
        private readonly AssemblyScanner _scanner;
        private readonly AssemblyReport _report;

        public AzureFunctionDiscoveryTests()
        {
            _scanner = ScannerFactory.Create();
            _report = _scanner.ScanAssembly(typeof(ProductFunctions).Assembly);
        }

        private AzureFunction[] Functions =>
            _report.Surfaces.OfType<AzureFunction>().ToArray();

        [Fact]
        public void Discover_FindsAllHttpTriggeredFunctions()
        {
            // ProductFunctions: 3, OrderFunctions: 2, NotificationFunctions: 2, HealthFunctions: 1
            Assert.Equal(8, Functions.Length);
        }

        [Fact]
        public void Discover_ExcludesNonHttpTriggers()
        {
            Assert.DoesNotContain(Functions, f => f.ClassName == "TimerFunctions");
        }

        [Fact]
        public void Discover_DetectsFunctionNameAttribute()
        {
            var getProducts = Functions.Single(f => f.FunctionName == "GetProducts");
            Assert.Equal("ProductFunctions", getProducts.ClassName);
            Assert.Equal("GetProducts", getProducts.MethodName);
        }

        [Fact]
        public void Discover_DetectsFunctionAttribute()
        {
            var healthCheck = Functions.Single(f => f.FunctionName == "HealthCheck");
            Assert.Equal("HealthFunctions", healthCheck.ClassName);
        }

        [Fact]
        public void Discover_ExtractsRoute()
        {
            var getProducts = Functions.Single(f => f.FunctionName == "GetProducts");
            Assert.Equal("products", getProducts.Route);
        }

        [Fact]
        public void Discover_ExtractsHttpMethod()
        {
            var createProduct = Functions.Single(f => f.FunctionName == "CreateProduct");
            Assert.Equal("POST", createProduct.HttpMethod);
        }

        [Fact]
        public void Discover_ExtractsAuthorizationLevel()
        {
            var getById = Functions.Single(f => f.FunctionName == "GetProductById");
            Assert.Equal("Function", getById.AuthorizationLevel);
        }

        [Fact]
        public void Discover_AnonymousIsUnauthenticated()
        {
            var getProducts = Functions.Single(f => f.FunctionName == "GetProducts");
            Assert.False(getProducts.RequiresAuthorization);
            Assert.False(getProducts.AllowAnonymous);
        }

        [Fact]
        public void Discover_FunctionKeyRequiresAuth()
        {
            var getById = Functions.Single(f => f.FunctionName == "GetProductById");
            Assert.True(getById.RequiresAuthorization);
        }

        [Fact]
        public void Discover_InheritsClassAuthorize()
        {
            var getOrders = Functions.Single(f => f.FunctionName == "GetOrders");
            Assert.True(getOrders.RequiresAuthorization);
            Assert.Contains("Admin", getOrders.Roles);
        }

        [Fact]
        public void Discover_AllowAnonymousOverridesClassAuth()
        {
            var getNotifications = Functions.Single(f => f.FunctionName == "GetNotifications");
            Assert.True(getNotifications.AllowAnonymous);
            Assert.False(getNotifications.RequiresAuthorization);
        }

        [Fact]
        public void Discover_DisplayRoute_Format()
        {
            var getProducts = Functions.Single(f => f.FunctionName == "GetProducts");
            Assert.Equal("GET products", getProducts.DisplayRoute);
        }

        [Fact]
        public void Discover_MethodParameters()
        {
            var getById = Functions.Single(f => f.FunctionName == "GetProductById");
            Assert.Equal(2, getById.Parameters.Count);
            Assert.Contains(getById.Parameters, p => p.Name == "id");
        }
    }
}
