using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Logging;
using Newtonsoft.Json;
using OmniSharp.Mef;
using OmniSharp.Middleware;
using OmniSharp.Models;
using OmniSharp.Models.v1;
using OmniSharp.Roslyn.CSharp.Services;
using OmniSharp.Services;
using Xunit;

namespace OmniSharp.Tests
{
    public class EndpointMiddlewareFacts
    {
        [OmniSharpEndpoint(typeof(RequestHandler<GotoDefinitionRequest, GotoDefinitionResponse>), LanguageNames.CSharp)]
        public class GotoDefinitionService : RequestHandler<GotoDefinitionRequest, GotoDefinitionResponse>
        {
            [Import]
            public OmnisharpWorkspace Workspace { get; set; }

            public Task<GotoDefinitionResponse> Handle(GotoDefinitionRequest request)
            {
                return Task.FromResult<GotoDefinitionResponse>(null);
            }
        }

        [OmniSharpEndpoint(typeof(Func<FindSymbolsRequest, Task<QuickFixResponse>>), LanguageNames.CSharp)]
        public Func<FindSymbolsRequest, Task<QuickFixResponse>> FindSymbolsDelegate { get; } = (request) => { return Task.FromResult<QuickFixResponse>(null); };

        [OmniSharpEndpoint(typeof(Func<UpdateBufferRequest, Task<object>>), LanguageNames.CSharp)]
        public Func<UpdateBufferRequest, Task<object>> UpdateBuffer { get; } = (request) => { return Task.FromResult<object>(true); };

        class Response { }

        [Export(typeof(IProjectSystem))]
        class FakeProjectSystem : IProjectSystem
        {
            public string Key { get { return "Fake"; } }
            public string Language { get { return LanguageNames.CSharp; } }
            public IEnumerable<string> Extensions { get; } = new[] { ".cs" };

            public Task<object> GetInformationModel(WorkspaceInformationRequest request)
            {
                throw new NotImplementedException();
            }

            public Task<object> GetProjectModel(string path)
            {
                throw new NotImplementedException();
            }

            public void Initalize(IConfiguration configuration) { }
        }

        class LoggerFactory : ILoggerFactory
        {
            public LogLevel MinimumLevel { get; set; }
            public void AddProvider(ILoggerProvider provider) { }
            public ILogger CreateLogger(string categoryName) { return new Logger(); }
            public void Dispose() { }
        }

        class Disposable : IDisposable { public void Dispose() { } }

        class Logger : ILogger
        {
            public IDisposable BeginScopeImpl(object state) { return new Disposable(); }
            public bool IsEnabled(LogLevel logLevel) { return true; }
            public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter) { }
        }

        [Fact]
        public async Task Passes_through_for_invalid_path()
        {
            RequestDelegate _next = (ctx) => Task.Run(() => { throw new NotImplementedException(); });

            var host = TestHelpers.CreatePluginHost(new[] { typeof(EndpointMiddlewareFacts).GetTypeInfo().Assembly });
            var middleware = new EndpointMiddleware(_next, host, new LoggerFactory(), Endpoints.AvailableEndpoints);

            var context = new DefaultHttpContext();
            context.Request.Path = PathString.FromUriComponent("/notvalid");

            await Assert.ThrowsAsync<NotImplementedException>(() => middleware.Invoke(context));
        }

        [Fact]
        public async Task Does_not_throw_for_valid_path()
        {
            RequestDelegate _next = (ctx) => Task.Run(() => { throw new NotImplementedException(); });

            var host = TestHelpers.CreatePluginHost(new[] { typeof(EndpointMiddlewareFacts).GetTypeInfo().Assembly });
            var middleware = new EndpointMiddleware(_next, host, new LoggerFactory(), Endpoints.AvailableEndpoints);

            var context = new DefaultHttpContext();
            context.Request.Path = PathString.FromUriComponent("/gotodefinition");

            var memoryStream = new MemoryStream();

            context.Request.Body = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(new GotoDefinitionRequest
                    {
                        FileName = "bar.cs",
                        Line = 2,
                        Column = 14,
                        Timeout = 60000
                    })
                )
            );

            await middleware.Invoke(context);

            Assert.True(true);
        }

        [Fact]
        public async Task Passes_through_to_services()
        {
            RequestDelegate _next = (ctx) => Task.Run(() => { throw new NotImplementedException(); });

            var host = TestHelpers.CreatePluginHost(new[] { typeof(EndpointMiddlewareFacts).GetTypeInfo().Assembly });
            var middleware = new EndpointMiddleware(_next, host, new LoggerFactory(), Endpoints.AvailableEndpoints);

            var context = new DefaultHttpContext();
            context.Request.Path = PathString.FromUriComponent("/gotodefinition");

            var memoryStream = new MemoryStream();

            context.Request.Body = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(new GotoDefinitionRequest
                    {
                        FileName = "bar.cs",
                        Line = 2,
                        Column = 14,
                        Timeout = 60000
                    })
                )
            );

            await middleware.Invoke(context);

            Assert.True(true);
        }

        [Fact]
        public async Task Passes_through_to_all_services_with_delegate()
        {
            RequestDelegate _next = (ctx) => Task.Run(() => { throw new NotImplementedException(); });

            var host = TestHelpers.CreatePluginHost(new[] { typeof(EndpointMiddlewareFacts).GetTypeInfo().Assembly });
            var middleware = new EndpointMiddleware(_next, host, new LoggerFactory(), Endpoints.AvailableEndpoints);

            var context = new DefaultHttpContext();
            context.Request.Path = PathString.FromUriComponent("/findsymbols");

            var memoryStream = new MemoryStream();

            context.Request.Body = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(new FindSymbolsRequest
                    {

                    })
                )
            );

            await middleware.Invoke(context);

            Assert.True(true);
        }

        [Fact]
        public async Task Passes_through_to_specific_service_with_delegate()
        {
            RequestDelegate _next = (ctx) => Task.Run(() => { throw new NotImplementedException(); });

            var host = TestHelpers.CreatePluginHost(new[] { typeof(EndpointMiddlewareFacts).GetTypeInfo().Assembly });
            var middleware = new EndpointMiddleware(_next, host, new LoggerFactory(), Endpoints.AvailableEndpoints);

            var context = new DefaultHttpContext();
            context.Request.Path = PathString.FromUriComponent("/findsymbols");

            var memoryStream = new MemoryStream();

            context.Request.Body = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(new FindSymbolsRequest
                    {
                        Language = LanguageNames.CSharp
                    })
                )
            );

            await middleware.Invoke(context);

            Assert.True(true);
        }

        public Func<ThrowRequest, Task<ThrowResponse>> ThrowDelegate = (request) =>
        {
            return Task.FromResult<ThrowResponse>(null);
        };

        public class ThrowRequest { }
        public class ThrowResponse { }

        [Fact]
        public async Task Should_throw_if_type_is_not_mergeable()
        {
            RequestDelegate _next = async (ctx) => await Task.Run(() => { throw new NotImplementedException(); });

            var host = TestHelpers.CreatePluginHost(new[] { typeof(EndpointMiddlewareFacts).GetTypeInfo().Assembly });
            var middleware = new EndpointMiddleware(_next, host, new LoggerFactory(), new[] { Endpoints.EndpointMapItem.Create<ThrowRequest, ThrowResponse>("/throw") });

            var context = new DefaultHttpContext();
            context.Request.Path = PathString.FromUriComponent("/throw");

            var memoryStream = new MemoryStream();

            context.Request.Body = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(new ThrowRequest())
                )
            );

            await Assert.ThrowsAsync<NotSupportedException>(async () => await middleware.Invoke(context));
        }
    }
}
