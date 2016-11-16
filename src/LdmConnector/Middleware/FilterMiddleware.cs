using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Serraview.LdmConnector.Middleware
{
    public abstract class FilterMiddlewareOptions
    {
        public virtual int RejectStatusCode { get { return (int) HttpStatusCode.Forbidden; } }
        public abstract bool ShouldReject(IOwinRequest request);
    }

    public class FilterMiddleware : OwinMiddleware
    {
        private readonly FilterMiddlewareOptions m_Options;

        public FilterMiddleware(OwinMiddleware next, FilterMiddlewareOptions options) : base(next)
        {
            m_Options = options;
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (m_Options.ShouldReject(context.Request))
            {
                context.Response.OnSendingHeaders(
                    state =>
                    {
                        var response = (OwinResponse) state;
                        response.StatusCode = m_Options.RejectStatusCode;
                    },
                    context.Response);

                return;
            }

            await Next.Invoke(context);
        }
    }
}