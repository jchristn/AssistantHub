namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Settings;
    using Voltaic;

    internal sealed class DependencyStubServer : IDisposable
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private Task? _listenerTask;

        public DependencyStubServer(int port)
        {
            Port = port;
            BaseUrl = "http://127.0.0.1:" + port + "/";
            _listener.Prefixes.Add(BaseUrl);
        }

        public int Port { get; }

        public string BaseUrl { get; }

        public void Start()
        {
            _listener.Start();
            _listenerTask = Task.Run(() => ListenAsync(_tokenSource.Token));
        }

        public void Dispose()
        {
            _tokenSource.Cancel();

            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch
            {
            }

            try
            {
                _listenerTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
            }

            _tokenSource.Dispose();
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested || !_listener.IsListening)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested || !_listener.IsListening)
                {
                    break;
                }

                _ = Task.Run(() => HandleAsync(context), cancellationToken);
            }
        }

        private static async Task HandleAsync(HttpListenerContext context)
        {
            try
            {
                string path = context.Request.Url?.AbsolutePath ?? "/";
                string responseBody = path.Equals("/api/tags", StringComparison.OrdinalIgnoreCase)
                    ? "{\"models\":[]}"
                    : "{}";

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";

                if (context.Request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.ContentLength64 = 0;
                    context.Response.Close();
                    return;
                }

                byte[] data = Encoding.UTF8.GetBytes(responseBody);
                context.Response.ContentLength64 = data.Length;
                await context.Response.OutputStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            }
            catch
            {
            }
            finally
            {
                try
                {
                    context.Response.OutputStream.Close();
                }
                catch
                {
                }
            }
        }
    }
}
