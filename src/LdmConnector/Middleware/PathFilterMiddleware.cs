using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;

namespace Serraview.LdmConnector.Middleware
{
    public sealed class PathFilterMiddlewareOptions : FilterMiddlewareOptions
    {
        public IEnumerable<string> AcceptablePaths { get; set; }

        public override bool ShouldReject(IOwinRequest request)
        {
            var acceptablePaths = new HashSet<PathString>(AcceptablePaths.Select(PathString.FromUriComponent));
            return !acceptablePaths.Contains(request.PathBase + request.Path);
        }
    }
}