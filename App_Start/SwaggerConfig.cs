using System.Web.Http;
using WebActivatorEx;
using SmkcApi;
using Swashbuckle.Application;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace SmkcApi
{
    /// <summary>
    /// Configures Swagger / Swashbuckle for the SMKC Web API.
    /// UI is accessible at: /swagger
    /// JSON spec is at:     /swagger/docs/v1
    /// </summary>
    public class SwaggerConfig
    {
        public static void Register()
        {
            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "SMKC ERP API")
                        .Description("SMKC Municipal Corporation ERP REST API")
                        .Contact(cc => cc
                            .Name("SMKC IT Department")
                        );

                    // Enable XML comments if the file exists (enable in project properties first)
                    var xmlFile = System.IO.Path.Combine(
                        System.AppDomain.CurrentDomain.BaseDirectory,
                        "bin", "SmkcApi.xml");
                    if (System.IO.File.Exists(xmlFile))
                    {
                        c.IncludeXmlComments(xmlFile);
                    }

                    // Describe the API-Key / SHA authentication scheme so Swagger UI shows
                    // the "Authorize" button and lets testers supply the required headers.
                    c.ApiKey("X-Api-Key")
                        .Description("API Key for SMKC API (header: X-Api-Key)")
                        .Name("X-Api-Key")
                        .In("header");

                    // Group operations by controller name (default behaviour, explicit here)
                    c.GroupActionsBy(apiDesc => apiDesc.ActionDescriptor.ControllerDescriptor.ControllerName);

                    // Ignore obsolete actions
                    c.IgnoreObsoleteActions();
                })
                .EnableSwaggerUi(u =>
                {
                    // Enable the "Authorize" input box so testers can supply the API key
                    u.EnableApiKeySupport("X-Api-Key", "header");

                    // Disable the validator badge (no external network call from browser)
                    u.DisableValidator();
                });
        }
    }
}
