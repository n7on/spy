using System;
using System.Collections.Generic;
using System.Reflection;
using DllSpy.Core.Contracts;
using DllSpy.Core.Helpers;

namespace DllSpy.Core.Services
{
    /// <summary>
    /// Discovers Azure Functions HTTP-triggered functions by scanning for [FunctionName] or [Function] attributes.
    /// </summary>
    internal class AzureFunctionDiscovery : IDiscovery
    {
        private readonly AttributeAnalyzer _analyzer;
        private readonly SecurityResolver _security;

        public AzureFunctionDiscovery(AttributeAnalyzer attributeAnalyzer)
        {
            _analyzer = attributeAnalyzer ?? throw new ArgumentNullException(nameof(attributeAnalyzer));
            _security = new SecurityResolver(attributeAnalyzer);
        }

        /// <inheritdoc />
        public SurfaceType SurfaceType => SurfaceType.AzureFunction;

        /// <inheritdoc />
        public List<InputSurface> Discover(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var surfaces = new List<InputSurface>();

            foreach (var type in ReflectionHelper.GetTypesSafe(assembly))
            {
                if (type == null || !type.IsClass || !type.IsPublic)
                    continue;

                var methods = type.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

                foreach (var method in methods)
                {
                    var functionName = GetFunctionName(method);
                    if (functionName == null) continue;

                    var trigger = GetHttpTriggerInfo(method);
                    if (trigger == null) continue;

                    var classSec = _security.ReadClass(type);
                    var merged = _security.Merge(classSec, method);

                    var authLevel = trigger.Value.AuthLevel;
                    var requiresAuth = !merged.AllowAnonymous &&
                        (merged.RequiresAuthorization || !string.Equals(authLevel, "Anonymous", StringComparison.OrdinalIgnoreCase));
                    var allowAnon = merged.AllowAnonymous;

                    var parameters = ReflectionHelper.GetParameters(method, _analyzer);
                    var returnType = ReflectionHelper.GetFriendlyTypeName(method.ReturnType);
                    var isAsync = ReflectionHelper.IsAsyncMethod(method);

                    var httpMethods = trigger.Value.Methods;
                    if (httpMethods == null || httpMethods.Length == 0)
                        httpMethods = new string[] { null };

                    foreach (var httpMethod in httpMethods)
                    {
                        surfaces.Add(new AzureFunction
                        {
                            FunctionName = functionName,
                            Route = trigger.Value.Route,
                            HttpMethod = httpMethod?.ToUpperInvariant(),
                            AuthorizationLevel = authLevel,
                            ClassName = type.Name,
                            MethodName = method.Name,
                            RequiresAuthorization = requiresAuth,
                            AllowAnonymous = allowAnon,
                            Roles = merged.Roles,
                            Policies = merged.Policies,
                            Parameters = parameters,
                            ReturnType = returnType,
                            IsAsync = isAsync,
                            SecurityAttributes = merged.SecurityAttributes
                        });
                    }
                }
            }

            return surfaces;
        }

        private static string GetFunctionName(MethodInfo method)
        {
            try
            {
                foreach (var attr in Attribute.GetCustomAttributes(method, true))
                {
                    var name = attr.GetType().Name;
                    if (name == "FunctionNameAttribute" || name == "FunctionAttribute")
                    {
                        var nameProp = attr.GetType().GetProperty("Name");
                        return nameProp?.GetValue(attr) as string;
                    }
                }
            }
            catch { }

            return null;
        }

        private static HttpTriggerInfo? GetHttpTriggerInfo(MethodInfo method)
        {
            foreach (var param in method.GetParameters())
            {
                try
                {
                    foreach (var attr in Attribute.GetCustomAttributes(param, true))
                    {
                        if (attr.GetType().Name == "HttpTriggerAttribute")
                        {
                            var authLevelProp = attr.GetType().GetProperty("AuthLevel");
                            var authLevel = authLevelProp?.GetValue(attr)?.ToString() ?? "Anonymous";

                            var methodsProp = attr.GetType().GetProperty("Methods");
                            var methods = methodsProp?.GetValue(attr) as string[];

                            var routeProp = attr.GetType().GetProperty("Route");
                            var route = routeProp?.GetValue(attr) as string;

                            return new HttpTriggerInfo
                            {
                                AuthLevel = authLevel,
                                Methods = methods,
                                Route = route
                            };
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        private struct HttpTriggerInfo
        {
            public string AuthLevel;
            public string[] Methods;
            public string Route;
        }
    }
}
