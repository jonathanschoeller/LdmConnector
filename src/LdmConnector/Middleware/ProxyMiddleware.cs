using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Owin;
using Serraview.Apis.Auth.OAuth2;
using Serraview.Apis.Core.Http;

namespace Serraview.LdmConnector.Middleware
{
    public sealed class ProxyMiddlewareOptions
    {
        public Uri DestinationServer { get; set; }
        public bool UseProxy { get; set; }

        public Uri OAuthAuthority { get; set; }
        public bool UseOAuth { get; set; }
        public string OAuthClientId { get; set; }
        public string OAuthKey { get; set; }
    }

    public class ProxyMiddleware : OwinMiddleware
    {
        private readonly ConfigurableHttpClient m_Client;

        private readonly ProxyMiddlewareOptions m_Options;

        public ProxyMiddleware(OwinMiddleware next, ProxyMiddlewareOptions options) : base(next)
        {
            m_Options = options;

            IWebProxy proxy = null;
            if (options.UseProxy)
                proxy = WebRequest.DefaultWebProxy;

            var clientFactory = new OAuthClientFactory(
                options.OAuthAuthority.ToString(),
                options.OAuthClientId,
                new OAuthClientFactory.PrivateKeyString(options.OAuthKey),
                proxy);

            var clientArgs = new CreateHttpClientArgs
            {
                GZipEnabled = true,
                ApplicationName = Assembly.GetExecutingAssembly().GetName().Name,

            };

            if (proxy != null)
            {
                clientArgs.ProxyEnabled = true;
                clientArgs.Proxy = proxy;
            }

            m_Client = clientFactory.CreateHttpClient(clientArgs);
            m_Client.DefaultRequestHeaders.Clear();
        }

        public override async Task Invoke(IOwinContext context)
        {
            var body = context.Response.Body;
            context.Response.Body = new MemoryStream();

            try
            {
                var incomingRequest = context.Request;

                var uriBuilder = new UriBuilder(m_Options.DestinationServer)
                {
                    Path = incomingRequest.Uri.AbsolutePath,
                    Query = incomingRequest.QueryString.ToString(),
                };

                var outgoingRequest = new HttpRequestMessage
                {
                    RequestUri = uriBuilder.Uri,
                    Method = new HttpMethod(incomingRequest.Method)
                };

                if (incomingRequest.Body != null && outgoingRequest.Method != HttpMethod.Get)
                    outgoingRequest.Content = new StreamContent(context.Request.Body);

                foreach (var header in incomingRequest.Headers.Where(pair => !pair.Key.Equals("Host", StringComparison.InvariantCultureIgnoreCase)))
                    outgoingRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);

                var response = await m_Client.SendAsync(outgoingRequest, context.Request.CallCancelled);
                await response.Content.CopyToAsync(body);

                foreach (var header in response.Headers.Concat(response.Content.Headers))
                    context.Response.Headers.SetValues(header.Key, header.Value.ToArray());

                context.Response.StatusCode = (int)response.StatusCode;
            }
            finally
            {
                context.Response.Body = body;
            }
        }
    }
}