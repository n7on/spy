using System;
using System.IO;
using System.Linq;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;
using DllSpy.Core.Tests.Fixtures;
using Xunit;

namespace DllSpy.Core.Tests.Services
{
    public class AssemblyScannerTests
    {
        private readonly AssemblyScanner _scanner;
        private readonly AssemblyReport _report;

        public AssemblyScannerTests()
        {
            _scanner = ScannerFactory.Create();
            _report = _scanner.ScanAssembly(typeof(UsersController).Assembly);
        }

        [Fact]
        public void ScanAssembly_ReturnsAllSurfaceTypes()
        {
            Assert.Contains(_report.Surfaces, s => s.SurfaceType == SurfaceType.HttpEndpoint);
            Assert.Contains(_report.Surfaces, s => s.SurfaceType == SurfaceType.SignalRMethod);
            Assert.Contains(_report.Surfaces, s => s.SurfaceType == SurfaceType.WcfOperation);
            Assert.Contains(_report.Surfaces, s => s.SurfaceType == SurfaceType.GrpcOperation);
            Assert.Contains(_report.Surfaces, s => s.SurfaceType == SurfaceType.RazorPage);
            Assert.Contains(_report.Surfaces, s => s.SurfaceType == SurfaceType.BlazorComponent);
            Assert.Contains(_report.Surfaces, s => s.SurfaceType == SurfaceType.AzureFunction);
        }

        [Fact]
        public void TotalSurfaces_IsCorrect()
        {
            // 12 HTTP + 5 SignalR + 6 WCF + 6 gRPC + 11 Razor + 6 Blazor + 8 AzureFunc = 54
            Assert.Equal(54, _report.TotalSurfaces);
        }

        [Fact]
        public void TotalHttpEndpoints_IsCorrect()
        {
            Assert.Equal(12, _report.TotalHttpEndpoints);
        }

        [Fact]
        public void TotalSignalRMethods_IsCorrect()
        {
            Assert.Equal(5, _report.TotalSignalRMethods);
        }

        [Fact]
        public void TotalClasses_IsCorrect()
        {
            // Users, Admin, Public, Plain, ChatHub, NotificationHub, LifecycleHub, OrderService, SecureService, IAuditService, GreeterService, OrderGrpcService
            // + IndexModel, ContactModel, DetailsModel, EditModel, DashboardModel, LoginModel
            // + Counter, WeatherForecast, AdminSettings, UserProfile, PublicInfo
            // + ProductFunctions, OrderFunctions, NotificationFunctions, HealthFunctions
            Assert.Equal(27, _report.TotalClasses);
        }

        [Fact]
        public void AuthenticatedSurfaces_CountIsCorrect()
        {
            // Update, Delete, GetDashboard, CreateSetting, Subscribe, Broadcast, GetStatus, UpdateConfig,
            // gRPC: OrderGrpcService.GetOrder, PlaceOrder, StreamOrders, Chat
            // Razor: DashboardModel.OnGet, DashboardModel.OnPostExportAsync, LoginModel.OnPostAsync
            // Blazor: AdminSettings, UserProfile
            // AzureFunc: GetProductById, GetOrders, PlaceOrder, SendNotification
            Assert.Equal(21, _report.AuthenticatedSurfaces);
        }

        [Fact]
        public void AnonymousSurfaces_CountIsCorrect()
        {
            // AllowAnonymous or !RequiresAuthorization = 33
            Assert.Equal(33, _report.AnonymousSurfaces);
        }

        [Fact]
        public void HighSeverityIssues_ForUnauthenticatedStateChanging()
        {
            var highIssues = _report.SecurityIssues.Where(i => i.Severity == SecuritySeverity.High).ToList();
            // HTTP: UsersController.Create (POST), PublicController.Submit (POST)
            // SignalR: ChatHub.SendMessage, ChatHub.JoinRoom, LifecycleHub.SendPing
            // WCF: IOrderService.GetOrder, IOrderService.PlaceOrder, IOrderService.NotifyShipped, IAuditService.LogEvent
            // gRPC: GreeterService.SayHello, GreeterService.SayHellos
            // Razor: ContactModel.OnPostAsync, EditModel.OnPostAsync, EditModel.OnPostDeleteAsync
            // Blazor: Counter, WeatherForecast(/weather), WeatherForecast(/forecast)
            // AzureFunc: GetProducts, CreateProduct, HealthCheck
            Assert.Equal(20, highIssues.Count);
        }

        [Fact]
        public void MediumSeverityIssues_ForMissingAuthDeclaration()
        {
            var mediumIssues = _report.SecurityIssues.Where(i => i.Severity == SecuritySeverity.Medium).ToList();
            // UsersController.GetAll, UsersController.GetById, PlainController.Index, PlainController.Details
            // Razor: IndexModel.OnGet, ContactModel.OnGet, DetailsModel.OnGet, EditModel.OnGet
            Assert.Equal(8, mediumIssues.Count);
        }

        [Fact]
        public void LowSeverityIssues_ForAuthWithoutRoles()
        {
            var lowIssues = _report.SecurityIssues.Where(i => i.Severity == SecuritySeverity.Low).ToList();
            // UsersController.Update, NotificationHub.Subscribe, SecureService.GetStatus,
            // gRPC: OrderGrpcService.GetOrder, OrderGrpcService.StreamOrders, OrderGrpcService.Chat
            // Razor: LoginModel.OnPostAsync
            // Blazor: UserProfile
            // AzureFunc: GetProductById, SendNotification
            Assert.Equal(10, lowIssues.Count);
        }

        [Fact]
        public void TotalSecurityIssues_IsCorrect()
        {
            // 20 HIGH + 8 MEDIUM + 10 LOW = 38
            Assert.Equal(38, _report.TotalSecurityIssues);
        }

        [Fact]
        public void TotalWcfOperations_IsCorrect()
        {
            Assert.Equal(6, _report.TotalWcfOperations);
        }

        [Fact]
        public void TotalGrpcOperations_IsCorrect()
        {
            Assert.Equal(6, _report.TotalGrpcOperations);
        }

        [Fact]
        public void ScanAssembly_WithNullPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _scanner.ScanAssembly((string)null));
        }

        [Fact]
        public void ScanAssembly_WithEmptyPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _scanner.ScanAssembly(""));
        }

        [Fact]
        public void ScanAssembly_WithNonexistentPath_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => _scanner.ScanAssembly("/nonexistent/path.dll"));
        }

        [Fact]
        public void ScanAssembly_WithNonAssemblyFile_ThrowsInvalidOperationException()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "not a .NET assembly");
                Assert.Throws<InvalidOperationException>(() => _scanner.ScanAssembly(tempFile));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Report_HasAssemblyName()
        {
            Assert.False(string.IsNullOrEmpty(_report.AssemblyName));
        }

        [Fact]
        public void Report_HasScanTimestamp()
        {
            Assert.True(_report.ScanTimestamp > DateTime.MinValue);
            Assert.True(_report.ScanTimestamp <= DateTime.UtcNow.AddSeconds(1));
        }
    }
}
