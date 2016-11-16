using System;
using System.Collections.Generic;
using Nancy;

namespace Serraview.LdmConnector.Tests.Helpers
{
    internal class TestModule : NancyModule
    {
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        public TestModule(IDictionary<string, Func<dynamic, NancyContext, dynamic>> postHandlers)
        {
            foreach(var postHandler in postHandlers)
            {
                var handler = postHandler;
                Post[postHandler.Key] = o => handler.Value(o, Context);
            }
        }
    }
}