using System;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace SmkcApi
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // Suppress default route registration for MVC
            RouteTable.Routes.RouteExistingFiles = false;
            
            // Configure Web API first
            GlobalConfiguration.Configure(WebApiConfig.Register);
            
            // Prevent default MVC routes from interfering
            RouteTable.Routes.Ignore("{resource}.axd/{*pathInfo}");
        }

        protected void Application_BeginRequest()
        {
            // Add security headers
            Response.Headers.Add("X-Frame-Options", "DENY");
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
            Response.Headers.Add("Content-Security-Policy", "default-src 'none'");
            
            // Remove server information
            Response.Headers.Remove("Server");
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            
            // Log the error (implement your logging mechanism)
            System.Diagnostics.Trace.TraceError($"Unhandled exception: {exception?.ToString()}");
            
            // Don't reveal internal error details
            Response.Clear();
            Response.StatusCode = 500;
            Response.Write("Internal Server Error");
            Server.ClearError();
        }
    }
}