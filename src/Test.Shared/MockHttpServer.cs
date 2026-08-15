namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Minimal in-process HTTP/1.1 server used to exercise the SDK without the live
    /// spaCy tokenizer Docker service. Uses a raw TcpListener bound to 127.0.0.1 on an
    /// OS-assigned port so no admin rights or URL ACL reservations are required.
    ///
    /// The server lives in Test.Shared so every Touchstone host (CLI, xUnit, NUnit, MSTest)
    /// exercises the SDK against identical, deterministic HTTP behavior.
    /// </summary>
    public sealed class MockHttpServer : IDisposable
    {
        /// <summary>
        /// Request captured by the server, exposed to handlers.
        /// </summary>
        public sealed class Request
        {
            /// <summary>HTTP method (e.g. GET, POST, HEAD).</summary>
            public string Method { get; set; }

            /// <summary>Request path (e.g. /tokenize).</summary>
            public string Path { get; set; }

            /// <summary>Request body as a string (empty for methods without a body).</summary>
            public string Body { get; set; }
        }

        /// <summary>
        /// Response produced by a handler.
        /// </summary>
        public sealed class Response
        {
            /// <summary>HTTP status code.</summary>
            public int StatusCode { get; set; } = 200;

            /// <summary>Response body (may be empty).</summary>
            public string Body { get; set; } = String.Empty;

            /// <summary>Content type header value.</summary>
            public string ContentType { get; set; } = "application/json";
        }

        /// <summary>
        /// Base endpoint URL of the form http://127.0.0.1:{port}/.
        /// </summary>
        public string Endpoint
        {
            get { return "http://127.0.0.1:" + _Port + "/"; }
        }

        private readonly TcpListener _Listener;
        private readonly int _Port;
        private readonly Func<Request, Response> _Handler;
        private readonly CancellationTokenSource _Cts = new CancellationTokenSource();
        private readonly Task _AcceptLoop;

        /// <summary>
        /// Start a mock server that dispatches every request to the supplied handler.
        /// </summary>
        /// <param name="handler">Handler that maps a request to a response.</param>
        public MockHttpServer(Func<Request, Response> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _Handler = handler;

            _Listener = new TcpListener(IPAddress.Loopback, 0);
            _Listener.Start();
            _Port = ((IPEndPoint)_Listener.LocalEndpoint).Port;
            _AcceptLoop = Task.Run(() => AcceptLoopAsync(_Cts.Token));
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _Listener.AcceptTcpClientAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    return;
                }

                _ = Task.Run(() => HandleClientAsync(client), token);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    Request req = await ReadRequestAsync(stream).ConfigureAwait(false);
                    if (req == null) return;

                    Response resp;
                    try
                    {
                        resp = _Handler(req) ?? new Response { StatusCode = 500, Body = String.Empty };
                    }
                    catch (Exception)
                    {
                        resp = new Response { StatusCode = 500, Body = String.Empty };
                    }

                    await WriteResponseAsync(stream, req, resp).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                // Client disconnected; ignore.
            }
            catch (ObjectDisposedException)
            {
                // Shutting down; ignore.
            }
        }

        private static async Task<Request> ReadRequestAsync(NetworkStream stream)
        {
            List<byte> headerBytes = new List<byte>();
            byte[] one = new byte[1];

            // Read until end of headers (CRLFCRLF).
            while (true)
            {
                int read = await stream.ReadAsync(one, 0, 1).ConfigureAwait(false);
                if (read == 0) break;
                headerBytes.Add(one[0]);

                int count = headerBytes.Count;
                if (count >= 4 &&
                    headerBytes[count - 4] == (byte)'\r' &&
                    headerBytes[count - 3] == (byte)'\n' &&
                    headerBytes[count - 2] == (byte)'\r' &&
                    headerBytes[count - 1] == (byte)'\n')
                {
                    break;
                }
            }

            if (headerBytes.Count == 0) return null;

            string headerText = Encoding.ASCII.GetString(headerBytes.ToArray());
            string[] lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (lines.Length == 0 || String.IsNullOrEmpty(lines[0])) return null;

            string[] requestLine = lines[0].Split(' ');
            if (requestLine.Length < 2) return null;

            string method = requestLine[0];
            string path = requestLine[1];

            int contentLength = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (String.IsNullOrEmpty(line)) continue;
                int colon = line.IndexOf(':');
                if (colon < 0) continue;
                string name = line.Substring(0, colon).Trim();
                string value = line.Substring(colon + 1).Trim();
                if (String.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    Int32.TryParse(value, out contentLength);
                }
            }

            string body = String.Empty;
            if (contentLength > 0)
            {
                byte[] buffer = new byte[contentLength];
                int total = 0;
                while (total < contentLength)
                {
                    int read = await stream.ReadAsync(buffer, total, contentLength - total).ConfigureAwait(false);
                    if (read == 0) break;
                    total += read;
                }
                body = Encoding.UTF8.GetString(buffer, 0, total);
            }

            return new Request { Method = method, Path = path, Body = body };
        }

        private static async Task WriteResponseAsync(NetworkStream stream, Request req, Response resp)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(resp.Body ?? String.Empty);
            bool isHead = String.Equals(req.Method, "HEAD", StringComparison.OrdinalIgnoreCase);

            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP/1.1 ").Append(resp.StatusCode).Append(' ').Append(ReasonPhrase(resp.StatusCode)).Append("\r\n");
            sb.Append("Content-Type: ").Append(resp.ContentType).Append("\r\n");
            sb.Append("Content-Length: ").Append(bodyBytes.Length).Append("\r\n");
            sb.Append("Connection: close\r\n");
            sb.Append("\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(sb.ToString());
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);

            // A HEAD response carries headers only, never a body.
            if (!isHead && bodyBytes.Length > 0)
            {
                await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length).ConfigureAwait(false);
            }

            await stream.FlushAsync().ConfigureAwait(false);
        }

        private static string ReasonPhrase(int status)
        {
            switch (status)
            {
                case 200: return "OK";
                case 400: return "Bad Request";
                case 404: return "Not Found";
                case 500: return "Internal Server Error";
                case 503: return "Service Unavailable";
                default: return "Status";
            }
        }

        /// <summary>
        /// Stop the server and release the listening socket.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _Cts.Cancel();
                _Listener.Stop();
            }
            catch (Exception)
            {
                // Ignore shutdown races.
            }
            finally
            {
                _Cts.Dispose();
            }
        }
    }
}
