using System;
using System.Configuration;
using System.Net;
using Microsoft.Owin;
using Owin;
using Serraview.LdmConnector.Middleware;

[assembly: OwinStartup(typeof(Serraview.LdmConnector.Startup))]

namespace Serraview.LdmConnector
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var forwardToUri = ConfigurationManager.AppSettings["forwardToUri"];

            // Filter on request method.
            var methodFilterOptions = new MethodFilterMiddlewareOptions
            {
                AcceptableMethods = ConfigurationManager.AppSettings["acceptableMethods"].Split(',')
            };

            // Filter on request path.
            var pathFilterOptions = new PathFilterMiddlewareOptions
            {
                AcceptablePaths = ConfigurationManager.AppSettings["acceptablePaths"].Split(',')
            };

            // Proxy requests.
            var options = new ProxyMiddlewareOptions
            {
                DestinationServer = new Uri(forwardToUri),
                OAuthAuthority = new Uri(ConfigurationManager.AppSettings["oauthAuthority"]),
                OAuthClientId = ConfigurationManager.AppSettings["oauthClientId"],
                OAuthKey = ConfigurationManager.AppSettings["oauthKey"],
                UseOAuth = true
            };

            Configuration(app, methodFilterOptions, pathFilterOptions, options);
        }

        public void Configuration(
            IAppBuilder app,
            MethodFilterMiddlewareOptions methodFilterOptions,
            PathFilterMiddlewareOptions pathFilterOptions,
            ProxyMiddlewareOptions proxyOptions)
        {
            app.Use<FilterMiddleware>(methodFilterOptions);
            app.Use<FilterMiddleware>(pathFilterOptions);
            app.Use<ProxyMiddleware>(proxyOptions);
        }
    }
}