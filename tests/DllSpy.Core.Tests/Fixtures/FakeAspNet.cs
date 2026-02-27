using System;

namespace DllSpy.Core.Tests.Fixtures
{
    // Base classes that match names DllSpy looks for via reflection
    public class ControllerBase { }

    public class Hub { }

    public class Hub<T> { }

    public class PageModel { }

    public class ComponentBase { }

    // Security attributes
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeAttribute : Attribute
    {
        public string Roles { get; set; }
        public string Policy { get; set; }
        public string AuthenticationSchemes { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AllowAnonymousAttribute : Attribute { }

    // Controller attributes
    [AttributeUsage(AttributeTargets.Class)]
    public class ApiControllerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RouteAttribute : Attribute
    {
        public string Template { get; }
        public RouteAttribute(string template) { Template = template; }
    }

    // HTTP method attributes
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpGetAttribute : Attribute
    {
        public string Template { get; }
        public HttpGetAttribute() { }
        public HttpGetAttribute(string template) { Template = template; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPostAttribute : Attribute
    {
        public string Template { get; }
        public HttpPostAttribute() { }
        public HttpPostAttribute(string template) { Template = template; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPutAttribute : Attribute
    {
        public string Template { get; }
        public HttpPutAttribute() { }
        public HttpPutAttribute(string template) { Template = template; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HttpDeleteAttribute : Attribute
    {
        public string Template { get; }
        public HttpDeleteAttribute() { }
        public HttpDeleteAttribute(string template) { Template = template; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPatchAttribute : Attribute
    {
        public string Template { get; }
        public HttpPatchAttribute() { }
        public HttpPatchAttribute(string template) { Template = template; }
    }

    // Parameter binding attributes
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromBodyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromQueryAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromRouteAttribute : Attribute { }

    // Property binding attributes
    [AttributeUsage(AttributeTargets.Property)]
    public class BindPropertyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class ParameterAttribute : Attribute { }

    // Azure Functions attributes
    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionNameAttribute : Attribute
    {
        public string Name { get; }
        public FunctionNameAttribute(string name) { Name = name; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionAttribute : Attribute
    {
        public string Name { get; }
        public FunctionAttribute(string name) { Name = name; }
    }

    public enum AuthorizationLevel
    {
        Anonymous = 0,
        User = 1,
        Function = 2,
        System = 3,
        Admin = 4
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class HttpTriggerAttribute : Attribute
    {
        public AuthorizationLevel AuthLevel { get; }
        public string[] Methods { get; }
        public string Route { get; set; }

        public HttpTriggerAttribute(AuthorizationLevel authLevel, params string[] methods)
        {
            AuthLevel = authLevel;
            Methods = methods;
        }
    }
}
