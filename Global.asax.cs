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
            // Redirect root to Swagger UI
            if (Request.Path == "/" || Request.Path == "")
            {
                Response.Redirect("/swagger", endResponse: true);
                return;
            }

            // Add security headers
            Response.Headers.Add("X-Frame-Options", "DENY");
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
            // Content-Security-Policy: relax for Swagger UI paths, strict for everything else
            if (Request.Path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                Response.Headers.Add("Content-Security-Policy",
                    "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:");
            }
            else
            {
                Response.Headers.Add("Content-Security-Policy", "default-src 'none'");
            }
            
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