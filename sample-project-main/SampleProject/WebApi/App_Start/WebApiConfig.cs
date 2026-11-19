using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using Common;
using Core;
using Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;
using SimpleInjector.Lifestyles;

namespace WebApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var container = new Container();
            var lifestyle = Lifestyle.Scoped;
            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            var assembly = typeof(WebApiConfig).Assembly;
            InitializeAssemblyInstancesService.Initialize(container, lifestyle, assembly);

            var userDatabaseConnectionString = ConfigurationManager.ConnectionStrings["SampleProject"];
            if (userDatabaseConnectionString == null)
            {
                throw new ConfigurationErrorsException("The SampleProject connection string is missing.");
            }

            DataConfiguration.Initialize(
                container,
                lifestyle,
                userDatabaseConnectionString.ConnectionString,
                SqlClientFactory.Instance);
            CoreConfiguration.Initialize(container, lifestyle);

            container.RegisterWebApiControllers(config);

            container.Verify();

            config.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(container);
            GlobalConfiguration.Configuration.DependencyResolver = config.DependencyResolver;

            var settings = config.Formatters.JsonFormatter.SerializerSettings;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            settings.DateParseHandling = DateParseHandling.DateTime;
            settings.DefaultValueHandling = DefaultValueHandling.Include;
            settings.Converters.Add(new StringEnumConverter());

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                                       "DefaultApi",
                                       "api/{controller}/{id}",
                                       new { id = RouteParameter.Optional }
                                      );
        }
    }
}