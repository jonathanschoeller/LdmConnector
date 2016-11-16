using System;
using System.Collections.Generic;
using Microsoft.Owin;

namespace Serraview.LdmConnector.Middleware
{
    public class MethodFilterMiddlewareOptions : FilterMiddlewareOptions
    {
        public IEnumerable<string> AcceptableMethods { get; set; } 

        public override bool ShouldReject(IOwinRequest request)
        {
            var acceptableMethods = new HashSet<string>(AcceptableMethods, StringComparer.InvariantCultureIgnoreCase);
            return !acceptableMethods.Contains(request.Method);
        }
    }
}