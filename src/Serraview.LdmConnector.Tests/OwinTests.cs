using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using Moq;
using Nancy;
using Nancy.Bootstrapper;
using NUnit.Framework;
using Serraview.LdmConnector.Middleware;
using Serraview.LdmConnector.Tests.Helpers;
using Serraview.TestDoubles;
using Serraview.TestDoubles.Helpers;
using Serraview.TestDoubles.Nancy;
using Serraview.TestDoubles.Nancy.Hosting;

namespace Serraview.LdmConnector.Tests
{
    [TestFixture]
    public class OwinTests
    {
        private static readonly X509Certificate2 s_OAuthCert = OAuth.Cert.Load(typeof(OwinTests).Assembly, "Serraview.LdmConnector.Tests.OAuthServer.pfx", "S3rrav13w");
        private static readonly IPAddress s_IpAddress = IPAddress.Parse("127.0.0.1");
        private static readonly int s_Port = s_IpAddress.GetRandomPort();

        private readonly TestDoubleHost m_TestDoubleHost = new TestDoubleHost(new UriBuilder("http", s_IpAddress.ToString(), s_Port).Uri);
        private Func<dynamic, NancyContext, dynamic> m_PostHandler;

        [SetUp]
        public void Setup()
        {
            var oauthServer = new OAuthServerMock(m_TestDoubleHost.Uri);
            oauthServer.SetupWellKnownDocuments(m_TestDoubleHost.Uri, s_OAuthCert);
            oauthServer.SetupAcceptServiceAccounts(s_OAuthCert);

            var postHandlers = new Dictionary<string, Func<dynamic, NancyContext, dynamic>>
            {
                {"/", PostHandler}
            };

            var serverDouble = new Mock<IServerDouble>();
            serverDouble
                .Setup(d => d.ModuleRegistration)
                .Returns(new ModuleRegistration(typeof(TestModule)));

            serverDouble
                .Setup(d => d.TypeRegistrations)
                .Returns(new[]
                {
                    new Tuple<Type, object>(typeof (IDictionary<string, Func<dynamic, NancyContext, dynamic>>), postHandlers)
                });

            m_TestDoubleHost.AttachTestDoubles(
                new OAuthServerDouble(oauthServer.Advanced.Object),
                serverDouble.Object);

            m_TestDoubleHost.Start();
        }

        [Test]
        public async Task Test()
        {
            var methodFilterOptions = new MethodFilterMiddlewareOptions
            {
                AcceptableMethods = new[] {"POST"}
            };

            var pathFilterOptions = new PathFilterMiddlewareOptions
            {
                AcceptablePaths = new[]{"/"}
            };

            var proxyOptions = new ProxyMiddlewareOptions
            {
                OAuthAuthority = m_TestDoubleHost.Uri,
                OAuthClientId = "client_id",
                OAuthKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEogIBAAKCAQEAh+FsFtn7+GQr1xWVhuYYf2Spy0T0t7IlduySUS+sk7Ywu51p
CVI4ZOJacmmyCfGLoIDRPtjDEy3TWJ2WQvIvl71FaU6QeBjfvNd1wZP260TxBzio
z9x3ETQCfThQr0sV09nzKuC9aNfglwGfSunS6966qRV0ZRUKEzcxF2IljKHMMHOL
rGu3cIWSE66RQIPT9/OZtBlGN8PD56FfFjXh6jqnAUTdIRW613rO6WvaS+jUOy9F
bWpJ5u9bRhhFCJIcOiWMEhZ6hDKD/pKW9UwgYvmSCEqGECNRrJud59T/9bs3+YeS
CXhL06MzE5b4yJlMDFukMw23hqBzj7a5ydOgeQIDAQABAoIBABPswDbRnBseRTdy
2MxBLVJw5l0CLYhKPSglJId3IC1AyACa4m4VemOAtZaVXSAMJVdRzF0U1/YWACm8
Ye5LKSNtA6KffJu/uf8s8P2Dvu9c2qMecdKZF3SUVdEa0uoBbp/0E64z6lJXZX1/
JWaMxvsDXAezN7Ai16ldrHsrTypBz745LsBdI9tyYIsg93HbN7kC1SWNsp4JGpqX
gAK3JYQCjXQekYj/TVZV+iI9FDuJCzMxPpUtO7wTKrbXDwTdor/TrONo3mkdXccj
h7LYnhNMdDnaEm6oBP/Nwye6y6PntdD6lWZHrxEaR2E889FwZj/TbbgajsmFPina
CZ213IkCgYEA4nBxdyEEtNWbPw3W/3lCK65PsqafeIBlm46i31n6K0bKzYqL6/lp
Rd7YHvYKxIpP+wclwhkhGa5/YzCpKnmfYshS/m0kXmG+x6BuF8AkVz6xHamOAYOQ
exOxGqcKECPNF0e/qM0zRvww+d3ww9Zw+WJm9+jOOHoEgq8OOdrhKfcCgYEAmZ6G
ZXxT4KbKQFXg4LbUSgh3dvIfLGXp1cZsjg+wVRvzFkoFkJLmTsfZ7IWYNwnccD1Z
VEYQy89nGpuKVRzc5AcmII5Bf6hzqKQ4qrnBahing8//RG+8/X6u3ogUbHi1BXN9
C67lB5dRLalA2CAhZ7PfvzdTt9diP9wEhlVcbQ8CgYAjouKYhv/AneVi1QDDEAhT
64jrasGqKzrScm47jGOMsAV2t5kxt/zTXDDTHpGvQL05mnRcyaul6QpvR9c3shBd
cX1uQSr6F5P4wszQvBJ8EIe7TVXl8xin5f93XFZ/F8NNKKOHI1Qwlbv3dvBPQc5h
0RS3a1IZHUrcbkRk4oeRHQKBgHjwZ6hAEBzd0n6B3a+r4EeEkOCwzz/53/Tv6QiS
a2UlwuO6VNU0AWLmTbe6mVJDTiuC8O+61YBPAUHeUDKfrXtL8YVR2VjyOlP7La2i
3hVz4XWRa8rqGSSM9oi1IzcedI0dFcX7481tIHjNNgKwkPv+jVkR6rPiOjRCN8G2
NDSbAoGACu1+7YdYYvc+5KRknFA8cUmnNRICigHhDG7F1e6Nyz/w6GWNuWqCt8J4
xGOF+dOj/0BYLp4/6hRq5akbgHbilKiEPwmEaPKPYUD1HOsufKQzPYrmz5g/RJaK
kVTLIKI5Mx8Jrt6581aJbCoNFCchGc+H27wiViD3yuK9OlE/3ck=
-----END RSA PRIVATE KEY-----",
                DestinationServer = m_TestDoubleHost.Uri
            };

            const string responseBody = "response body";
            const string postBody = "post body";

            // Set up the handler that gets called by our the mock server that the LdmConnector is forwarding requests to.
            var postHandlerCallCount = 0;
            m_PostHandler = (o, context) =>
            {
                postHandlerCallCount++;
                var requestBody = context.Request.Body;
                var buffer = new byte[requestBody.Length];
                requestBody.Read(buffer, 0, (int)requestBody.Length);
                Assert.AreEqual(postBody, Encoding.UTF8.GetString(buffer));

                var headers = context.Request.Headers;
                var authorizationHeader = headers.Authorization;
                Assert.IsNotNull(authorizationHeader);

                var authScheme = authorizationHeader.Split(new []{' '}, 2)[0];
                Assert.AreEqual("Bearer", authScheme);

                var token = authorizationHeader.Split(new[] {' '}, 2)[1];
                Assert.IsNotNullOrEmpty(token);

                return responseBody;
            };

            using(var server = TestServer.Create(app => new Startup().Configuration(app, methodFilterOptions, pathFilterOptions, proxyOptions)))
            {
                var request = server.CreateRequest("/");
                request.And(message => message.Content = new StringContent(postBody));
                var response = await request.PostAsync();

                Assert.AreEqual(responseBody, await response.Content.ReadAsStringAsync());
            }

            Assert.AreEqual(1, postHandlerCallCount);
        }

        private dynamic PostHandler(dynamic arg, NancyContext context)
        {
            return m_PostHandler(arg, context);
        }
    }
}
