using System.Web.Http;
using System.Web.Http.Cors;
using System.Configuration;
using SmkcApi.Repositories;
using SmkcApi.Services;
using SmkcApi.App_Start; // add to resolve SimpleDependencyResolver

namespace SmkcApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Configure dependency injection
            ConfigureDependencyInjection(config);
            
            // Enable CORS for all origins (allow all for development/testing)
            var cors = new EnableCorsAttribute(
                origins: "*",           // Allow all origins
                headers: "*",           // Allow all headers
                methods: "*"            // Allow all HTTP methods (GET, POST, PUT, DELETE, etc.)
            );
            config.EnableCors(cors);

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Remove XML formatter - JSON only
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Configure JSON formatting
            config.Formatters.JsonFormatter.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ssZ";
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

            // Register SHA/API-key authentication handler for protected endpoints.
            // Default is enabled to keep DepositManager APIs permanently secured.
            if (IsApiKeyAuthenticationEnabled())
            {
                config.MessageHandlers.Add(new Security.ApiKeyAuthenticationHandler());
            }
            else
            {
                System.Diagnostics.Trace.TraceWarning(
                    "API key authentication is disabled via config. Protected endpoints with [ShaAuthentication] will return 401.");
            }
        }

        private static bool IsApiKeyAuthenticationEnabled()
        {
            var raw = ConfigurationManager.AppSettings["EnableApiKeyAuthentication"];
            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            bool enabled;
            return bool.TryParse(raw, out enabled) ? enabled : true;
        }

        private static void ConfigureDependencyInjection(HttpConfiguration config)
        {
            config.DependencyResolver = new SimpleDependencyResolver();
        }
    }
}