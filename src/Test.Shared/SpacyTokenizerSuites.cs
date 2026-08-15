namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using SpacyTokenizerSdk;

    using Touchstone.Core;

    /// <summary>
    /// Central, runner-agnostic source of truth for the SpacyTokenizerSdk test suite.
    ///
    /// Every test is expressed once as a Touchstone <see cref="TestCaseDescriptor"/> and is
    /// executed identically by the CLI runner (Test.Automated), the xUnit host (Test.Xunit),
    /// and the NUnit host (Test.Nunit). HTTP behavior is served by an in-process
    /// <see cref="MockHttpServer"/> so no live spaCy tokenizer Docker service is required.
    /// </summary>
    public static class SpacyTokenizerSuites
    {
        private const string DefaultEndpoint = "http://localhost:8000/";

        /// <summary>
        /// All suites exposed to every Touchstone host.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                return new List<TestSuiteDescriptor>
                {
                    EndpointSuite(),
                    ValidateConnectivitySuite(),
                    TokenizeSingleSuite(),
                    TokenizeBatchSuite(),
                    ModelSuite()
                };
            }
        }

        #region Endpoint-And-Constructor

        private static TestSuiteDescriptor EndpointSuite()
        {
            const string suite = "Endpoint";

            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                Case(suite, "Default_UsesDefaultEndpoint",
                    "Default constructor uses http://localhost:8000/",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        Check.Equal(DefaultEndpoint, t.Endpoint);
                        return Task.CompletedTask;
                    }),

                Case(suite, "NullEndpoint_KeepsDefault",
                    "Constructor with null endpoint keeps the default",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer(null);
                        Check.Equal(DefaultEndpoint, t.Endpoint);
                        return Task.CompletedTask;
                    }),

                Case(suite, "EmptyEndpoint_KeepsDefault",
                    "Constructor with empty endpoint keeps the default",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer(String.Empty);
                        Check.Equal(DefaultEndpoint, t.Endpoint);
                        return Task.CompletedTask;
                    }),

                Case(suite, "Constructor_NoTrailingSlash_AppendsSlash",
                    "Constructor appends trailing slash when missing",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer("http://localhost:9000");
                        Check.Equal("http://localhost:9000/", t.Endpoint);
                        return Task.CompletedTask;
                    }),

                Case(suite, "Constructor_TrailingSlash_Preserved",
                    "Constructor preserves an existing trailing slash",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer("http://localhost:9000/");
                        Check.Equal("http://localhost:9000/", t.Endpoint);
                        return Task.CompletedTask;
                    }),

                Case(suite, "Constructor_InvalidUri_Throws",
                    "Constructor with an invalid URI throws UriFormatException",
                    _ =>
                    {
                        Check.Throws<UriFormatException>(() => new SpacyTokenizer("this is not a uri"));
                        return Task.CompletedTask;
                    }),

                Case(suite, "Setter_NoTrailingSlash_AppendsSlash",
                    "Endpoint setter appends trailing slash when missing",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        t.Endpoint = "http://localhost:9000";
                        Check.Equal("http://localhost:9000/", t.Endpoint);
                        return Task.CompletedTask;
                    }),

                Case(suite, "Setter_TrailingSlash_Preserved",
                    "Endpoint setter preserves an existing trailing slash",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        t.Endpoint = "http://localhost:9000/";
                        Check.Equal("http://localhost:9000/", t.Endpoint);
                        return Task.CompletedTask;
                    }),

                Case(suite, "Setter_HttpsScheme_Preserved",
                    "Endpoint setter preserves the https scheme and appends slash",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        t.Endpoint = "https://tokenizer.example.com:8443";
                        Check.Equal("https://tokenizer.example.com:8443/", t.Endpoint);
                        return Task.CompletedTask;
                    }),

                Case(suite, "Setter_WithPath_AppendsSlash",
                    "Endpoint setter appends a slash after a path segment",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        t.Endpoint = "http://localhost:8000/api";
                        Check.Equal("http://localhost:8000/api/", t.Endpoint);
                        return Task.CompletedTask;
                    }),

                Case(suite, "Setter_Null_Throws",
                    "Endpoint setter with null throws ArgumentNullException",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        Check.Throws<ArgumentNullException>(() => t.Endpoint = null);
                        return Task.CompletedTask;
                    }),

                Case(suite, "Setter_Empty_Throws",
                    "Endpoint setter with empty string throws ArgumentNullException",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        Check.Throws<ArgumentNullException>(() => t.Endpoint = String.Empty);
                        return Task.CompletedTask;
                    }),

                Case(suite, "Setter_InvalidUri_Throws",
                    "Endpoint setter with an invalid URI throws UriFormatException",
                    _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        Check.Throws<UriFormatException>(() => t.Endpoint = "not a uri at all");
                        return Task.CompletedTask;
                    })
            };

            return new TestSuiteDescriptor(suite, "Endpoint & Constructor", cases);
        }

        #endregion

        #region ValidateConnectivity

        private static TestSuiteDescriptor ValidateConnectivitySuite()
        {
            const string suite = "ValidateConnectivity";

            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                Case(suite, "Returns200_ReturnsTrue",
                    "ValidateConnectivity returns true on HTTP 200",
                    async ct =>
                    {
                        MockHttpServer.Request captured = null;
                        using (MockHttpServer server = new MockHttpServer(req =>
                        {
                            captured = req;
                            return new MockHttpServer.Response { StatusCode = 200 };
                        }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            bool connected = await t.ValidateConnectivity(ct);
                            Check.True(connected, "Expected connectivity to succeed.");
                            Check.NotNull(captured, "Server should have received a request.");
                            Check.Equal("HEAD", captured.Method);
                            Check.Equal("/", captured.Path);
                        }
                    }),

                Case(suite, "Returns503_ReturnsFalse",
                    "ValidateConnectivity returns false on HTTP 503",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response { StatusCode = 503 }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            bool connected = await t.ValidateConnectivity(ct);
                            Check.False(connected, "Expected connectivity to fail on 503.");
                        }
                    }),

                Case(suite, "Returns404_ReturnsFalse",
                    "ValidateConnectivity returns false on HTTP 404",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response { StatusCode = 404 }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            bool connected = await t.ValidateConnectivity(ct);
                            Check.False(connected, "Expected connectivity to fail on 404.");
                        }
                    }),

                Case(suite, "Returns500_ReturnsFalse",
                    "ValidateConnectivity returns false on HTTP 500",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response { StatusCode = 500 }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            bool connected = await t.ValidateConnectivity(ct);
                            Check.False(connected, "Expected connectivity to fail on 500.");
                        }
                    }),

                Case(suite, "Cancelled_Throws",
                    "ValidateConnectivity throws when the token is already cancelled",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response { StatusCode = 200 }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            using (CancellationTokenSource cts = new CancellationTokenSource())
                            {
                                cts.Cancel();
                                await Check.ThrowsAsync<OperationCanceledException>(
                                    () => t.ValidateConnectivity(cts.Token));
                            }
                        }
                    })
            };

            return new TestSuiteDescriptor(suite, "Validate Connectivity", cases);
        }

        #endregion

        #region Tokenize-Single

        private static TestSuiteDescriptor TokenizeSingleSuite()
        {
            const string suite = "TokenizeSingle";

            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                Case(suite, "ValidText_ReturnsTokens",
                    "Tokenize(string) returns parsed tokens and issues POST /tokenize",
                    async ct =>
                    {
                        MockHttpServer.Request captured = null;
                        using (MockHttpServer server = new MockHttpServer(req =>
                        {
                            captured = req;
                            return new MockHttpServer.Response
                            {
                                StatusCode = 200,
                                Body = "{\"text\":\"Hello world\",\"tokens\":[\"Hello\",\"world\"]}"
                            };
                        }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            TokenizationResult result = await t.Tokenize("Hello world", ct);

                            Check.NotNull(result);
                            Check.Equal("Hello world", result.Text);
                            Check.Count(2, result.Tokens);
                            Check.Equal("Hello", result.Tokens[0]);
                            Check.Equal("world", result.Tokens[1]);

                            Check.NotNull(captured, "Server should have received a request.");
                            Check.Equal("POST", captured.Method);
                            Check.Equal("/tokenize", captured.Path);
                            Check.Contains("\"text\"", captured.Body);
                            Check.Contains("Hello world", captured.Body);
                        }
                    }),

                Case(suite, "SingleTokenResult",
                    "Tokenize(string) returns a single-token result",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response
                            {
                                StatusCode = 200,
                                Body = "{\"text\":\"cat\",\"tokens\":[\"cat\"]}"
                            }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            TokenizationResult result = await t.Tokenize("cat", ct);

                            Check.NotNull(result);
                            Check.Count(1, result.Tokens);
                            Check.Equal("cat", result.Tokens[0]);
                        }
                    }),

                Case(suite, "UnicodeText_RoundTripsInRequest",
                    "Tokenize(string) transmits unicode text in the request body",
                    async ct =>
                    {
                        MockHttpServer.Request captured = null;
                        string text = "café naïve 你好";
                        using (MockHttpServer server = new MockHttpServer(req =>
                        {
                            captured = req;
                            return new MockHttpServer.Response
                            {
                                StatusCode = 200,
                                Body = "{\"text\":\"x\",\"tokens\":[\"x\"]}"
                            };
                        }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            TokenizationResult result = await t.Tokenize(text, ct);

                            Check.NotNull(result);
                            Check.NotNull(captured);

                            // The serializer may JSON-escape non-ASCII (e.g. café), which is
                            // valid JSON. Parse the captured body and compare the decoded value so
                            // the assertion proves the unicode survives the round-trip regardless
                            // of escaping.
                            using (JsonDocument doc = JsonDocument.Parse(captured.Body))
                            {
                                string sent = doc.RootElement.GetProperty("text").GetString();
                                Check.Equal(text, sent);
                            }
                        }
                    }),

                Case(suite, "EmptyTokensArray_ReturnsEmptyList",
                    "Tokenize(string) returns a non-null empty token list when none are returned",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response
                            {
                                StatusCode = 200,
                                Body = "{\"text\":\"\",\"tokens\":[]}"
                            }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            TokenizationResult result = await t.Tokenize("anything", ct);

                            Check.NotNull(result);
                            Check.NotNull(result.Tokens);
                            Check.Count(0, result.Tokens);
                        }
                    }),

                Case(suite, "Server500_ReturnsNull",
                    "Tokenize(string) returns null on HTTP 500",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response { StatusCode = 500, Body = String.Empty }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            TokenizationResult result = await t.Tokenize("Hello world", ct);
                            Check.Null(result);
                        }
                    }),

                Case(suite, "Server400_ReturnsNull",
                    "Tokenize(string) returns null on HTTP 400",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response { StatusCode = 400, Body = String.Empty }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            TokenizationResult result = await t.Tokenize("Hello world", ct);
                            Check.Null(result);
                        }
                    }),

                Case(suite, "Server200EmptyBody_ReturnsNull",
                    "Tokenize(string) returns null when body is empty despite HTTP 200",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response { StatusCode = 200, Body = String.Empty }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            TokenizationResult result = await t.Tokenize("Hello world", ct);
                            Check.Null(result);
                        }
                    }),

                Case(suite, "NullText_Throws",
                    "Tokenize(string) throws ArgumentNullException for null text",
                    async _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        await Check.ThrowsAsync<ArgumentNullException>(() => t.Tokenize((string)null));
                    }),

                Case(suite, "EmptyText_Throws",
                    "Tokenize(string) throws ArgumentNullException for empty text",
                    async _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        await Check.ThrowsAsync<ArgumentNullException>(() => t.Tokenize(String.Empty));
                    }),

                Case(suite, "Cancelled_Throws",
                    "Tokenize(string) throws when the token is already cancelled",
                    async _ =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response
                            {
                                StatusCode = 200,
                                Body = "{\"text\":\"x\",\"tokens\":[\"x\"]}"
                            }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            using (CancellationTokenSource cts = new CancellationTokenSource())
                            {
                                cts.Cancel();
                                await Check.ThrowsAsync<OperationCanceledException>(
                                    () => t.Tokenize("Hello", cts.Token));
                            }
                        }
                    })
            };

            return new TestSuiteDescriptor(suite, "Tokenize (single)", cases);
        }

        #endregion

        #region Tokenize-Batch

        private static TestSuiteDescriptor TokenizeBatchSuite()
        {
            const string suite = "TokenizeBatch";

            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                Case(suite, "ValidList_ReturnsResults",
                    "Tokenize(list) returns ordered batch results and issues POST /tokenize",
                    async ct =>
                    {
                        MockHttpServer.Request captured = null;
                        using (MockHttpServer server = new MockHttpServer(req =>
                        {
                            captured = req;
                            return new MockHttpServer.Response
                            {
                                StatusCode = 200,
                                Body = "{\"results\":[" +
                                       "{\"text\":\"one two\",\"tokens\":[\"one\",\"two\"]}," +
                                       "{\"text\":\"three\",\"tokens\":[\"three\"]}]}"
                            };
                        }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            List<string> texts = new List<string> { "one two", "three" };
                            BatchTokenizationResult result = await t.Tokenize(texts, ct);

                            Check.NotNull(result);
                            Check.NotNull(result.Results);
                            Check.Count(2, result.Results);
                            Check.Count(2, result.Results[0].Tokens);
                            Check.Equal("one", result.Results[0].Tokens[0]);
                            Check.Equal("two", result.Results[0].Tokens[1]);
                            Check.Count(1, result.Results[1].Tokens);
                            Check.Equal("three", result.Results[1].Tokens[0]);

                            Check.NotNull(captured, "Server should have received a request.");
                            Check.Equal("POST", captured.Method);
                            Check.Equal("/tokenize", captured.Path);
                            Check.Contains("\"texts\"", captured.Body);
                            Check.Contains("one two", captured.Body);
                            Check.Contains("three", captured.Body);
                        }
                    }),

                Case(suite, "SingleElementList_ReturnsOneResult",
                    "Tokenize(list) handles a single-element batch",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response
                            {
                                StatusCode = 200,
                                Body = "{\"results\":[{\"text\":\"solo\",\"tokens\":[\"solo\"]}]}"
                            }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            BatchTokenizationResult result = await t.Tokenize(new List<string> { "solo" }, ct);

                            Check.NotNull(result);
                            Check.Count(1, result.Results);
                            Check.Equal("solo", result.Results[0].Text);
                        }
                    }),

                Case(suite, "EmptyResults_ReturnsEmptyList",
                    "Tokenize(list) returns a non-null empty results list when none are returned",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response
                            {
                                StatusCode = 200,
                                Body = "{\"results\":[]}"
                            }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            BatchTokenizationResult result = await t.Tokenize(new List<string> { "x" }, ct);

                            Check.NotNull(result);
                            Check.NotNull(result.Results);
                            Check.Count(0, result.Results);
                        }
                    }),

                Case(suite, "Server400_ReturnsNull",
                    "Tokenize(list) returns null on HTTP 400",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response { StatusCode = 400, Body = String.Empty }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            BatchTokenizationResult result = await t.Tokenize(new List<string> { "one", "two" }, ct);
                            Check.Null(result);
                        }
                    }),

                Case(suite, "Server500_ReturnsNull",
                    "Tokenize(list) returns null on HTTP 500",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response { StatusCode = 500, Body = String.Empty }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            BatchTokenizationResult result = await t.Tokenize(new List<string> { "one", "two" }, ct);
                            Check.Null(result);
                        }
                    }),

                Case(suite, "Server200EmptyBody_ReturnsNull",
                    "Tokenize(list) returns null when body is empty despite HTTP 200",
                    async ct =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response { StatusCode = 200, Body = String.Empty }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            BatchTokenizationResult result = await t.Tokenize(new List<string> { "one", "two" }, ct);
                            Check.Null(result);
                        }
                    }),

                Case(suite, "NullList_Throws",
                    "Tokenize(list) throws ArgumentNullException for a null list",
                    async _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        await Check.ThrowsAsync<ArgumentNullException>(() => t.Tokenize((List<string>)null));
                    }),

                Case(suite, "EmptyList_Throws",
                    "Tokenize(list) throws ArgumentNullException for an empty list",
                    async _ =>
                    {
                        SpacyTokenizer t = new SpacyTokenizer();
                        await Check.ThrowsAsync<ArgumentNullException>(() => t.Tokenize(new List<string>()));
                    }),

                Case(suite, "Cancelled_Throws",
                    "Tokenize(list) throws when the token is already cancelled",
                    async _ =>
                    {
                        using (MockHttpServer server = new MockHttpServer(req =>
                            new MockHttpServer.Response
                            {
                                StatusCode = 200,
                                Body = "{\"results\":[]}"
                            }))
                        {
                            SpacyTokenizer t = new SpacyTokenizer(server.Endpoint);
                            using (CancellationTokenSource cts = new CancellationTokenSource())
                            {
                                cts.Cancel();
                                await Check.ThrowsAsync<OperationCanceledException>(
                                    () => t.Tokenize(new List<string> { "Hello" }, cts.Token));
                            }
                        }
                    })
            };

            return new TestSuiteDescriptor(suite, "Tokenize (batch)", cases);
        }

        #endregion

        #region Model-And-Serialization

        private static TestSuiteDescriptor ModelSuite()
        {
            const string suite = "Model";

            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                Case(suite, "TokenizationRequest_Defaults",
                    "TokenizationRequest defaults Text and Texts to null",
                    _ =>
                    {
                        TokenizationRequest req = new TokenizationRequest();
                        Check.Null(req.Text);
                        Check.Null(req.Texts);
                        return Task.CompletedTask;
                    }),

                Case(suite, "TokenizationResult_Defaults",
                    "TokenizationResult defaults Tokens to a non-null empty list",
                    _ =>
                    {
                        TokenizationResult result = new TokenizationResult();
                        Check.Null(result.Text);
                        Check.NotNull(result.Tokens);
                        Check.Count(0, result.Tokens);
                        return Task.CompletedTask;
                    }),

                Case(suite, "BatchTokenizationResult_Defaults",
                    "BatchTokenizationResult defaults Results to a non-null empty list",
                    _ =>
                    {
                        BatchTokenizationResult result = new BatchTokenizationResult();
                        Check.NotNull(result.Results);
                        Check.Count(0, result.Results);
                        return Task.CompletedTask;
                    }),

                Case(suite, "TokenizationRequest_SerializesTextField",
                    "TokenizationRequest serializes the Text value under the \"text\" property",
                    _ =>
                    {
                        string json = JsonSerializer.Serialize(new TokenizationRequest { Text = "hello" });
                        Check.Contains("\"text\"", json);
                        Check.Contains("hello", json);
                        return Task.CompletedTask;
                    }),

                Case(suite, "TokenizationRequest_SerializesTextsField",
                    "TokenizationRequest serializes the Texts value under the \"texts\" property",
                    _ =>
                    {
                        string json = JsonSerializer.Serialize(
                            new TokenizationRequest { Texts = new List<string> { "a", "b" } });
                        Check.Contains("\"texts\"", json);
                        Check.Contains("\"a\"", json);
                        Check.Contains("\"b\"", json);
                        return Task.CompletedTask;
                    }),

                Case(suite, "TokenizationResult_RoundTrips",
                    "TokenizationResult deserializes from the microservice wire format",
                    _ =>
                    {
                        TokenizationResult result = JsonSerializer.Deserialize<TokenizationResult>(
                            "{\"text\":\"a b\",\"tokens\":[\"a\",\"b\"]}");
                        Check.NotNull(result);
                        Check.Equal("a b", result.Text);
                        Check.Count(2, result.Tokens);
                        Check.Equal("a", result.Tokens[0]);
                        Check.Equal("b", result.Tokens[1]);
                        return Task.CompletedTask;
                    }),

                Case(suite, "BatchTokenizationResult_RoundTrips",
                    "BatchTokenizationResult deserializes from the microservice wire format",
                    _ =>
                    {
                        BatchTokenizationResult result = JsonSerializer.Deserialize<BatchTokenizationResult>(
                            "{\"results\":[{\"text\":\"a\",\"tokens\":[\"a\"]}," +
                            "{\"text\":\"b c\",\"tokens\":[\"b\",\"c\"]}]}");
                        Check.NotNull(result);
                        Check.Count(2, result.Results);
                        Check.Equal("a", result.Results[0].Text);
                        Check.Count(1, result.Results[0].Tokens);
                        Check.Count(2, result.Results[1].Tokens);
                        Check.Equal("c", result.Results[1].Tokens[1]);
                        return Task.CompletedTask;
                    })
            };

            return new TestSuiteDescriptor(suite, "Model & Serialization", cases);
        }

        #endregion

        #region Helpers

        private static TestCaseDescriptor Case(
            string suiteId,
            string caseId,
            string displayName,
            Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(suiteId, caseId, displayName, executeAsync);
        }

        #endregion
    }
}
