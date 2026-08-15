namespace SpacyTokenizerSdk.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using SpacyTokenizerSdk;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="SpacyTokenizer"/>. HTTP interactions are exercised against
    /// an in-process <see cref="MockHttpServer"/> so no live Docker service is required.
    /// </summary>
    public class SpacyTokenizerTests
    {
        #region Constructor-and-Endpoint

        [Fact]
        public void Constructor_Default_UsesDefaultEndpoint()
        {
            SpacyTokenizer tokenizer = new SpacyTokenizer();
            Assert.Equal("http://localhost:8000/", tokenizer.Endpoint);
        }

        [Fact]
        public void Constructor_NullEndpoint_KeepsDefaultEndpoint()
        {
            // A null/empty endpoint is ignored by the constructor, leaving the default in place.
            SpacyTokenizer tokenizer = new SpacyTokenizer(null);
            Assert.Equal("http://localhost:8000/", tokenizer.Endpoint);
        }

        [Fact]
        public void Endpoint_WithoutTrailingSlash_AppendsSlash()
        {
            SpacyTokenizer tokenizer = new SpacyTokenizer("http://localhost:9000");
            Assert.Equal("http://localhost:9000/", tokenizer.Endpoint);
        }

        [Fact]
        public void Endpoint_WithTrailingSlash_PreservesValue()
        {
            SpacyTokenizer tokenizer = new SpacyTokenizer("http://localhost:9000/");
            Assert.Equal("http://localhost:9000/", tokenizer.Endpoint);
        }

        [Fact]
        public void Endpoint_SetNull_Throws()
        {
            SpacyTokenizer tokenizer = new SpacyTokenizer();
            Assert.Throws<ArgumentNullException>(() => tokenizer.Endpoint = null);
        }

        [Fact]
        public void Endpoint_SetEmpty_Throws()
        {
            SpacyTokenizer tokenizer = new SpacyTokenizer();
            Assert.Throws<ArgumentNullException>(() => tokenizer.Endpoint = String.Empty);
        }

        [Fact]
        public void Endpoint_SetInvalidUri_Throws()
        {
            SpacyTokenizer tokenizer = new SpacyTokenizer();
            Assert.Throws<UriFormatException>(() => tokenizer.Endpoint = "this is not a uri");
        }

        #endregion

        #region ValidateConnectivity

        [Fact]
        public async Task ValidateConnectivity_ServerReturns200_ReturnsTrue()
        {
            using (MockHttpServer server = new MockHttpServer(req =>
            {
                Assert.Equal("HEAD", req.Method);
                return new MockHttpServer.Response { StatusCode = 200 };
            }))
            {
                SpacyTokenizer tokenizer = new SpacyTokenizer(server.Endpoint);
                bool connected = await tokenizer.ValidateConnectivity();
                Assert.True(connected);
            }
        }

        [Fact]
        public async Task ValidateConnectivity_ServerReturns503_ReturnsFalse()
        {
            using (MockHttpServer server = new MockHttpServer(req =>
                new MockHttpServer.Response { StatusCode = 503 }))
            {
                SpacyTokenizer tokenizer = new SpacyTokenizer(server.Endpoint);
                bool connected = await tokenizer.ValidateConnectivity();
                Assert.False(connected);
            }
        }

        #endregion

        #region Tokenize-Single

        [Fact]
        public async Task Tokenize_SingleText_ReturnsTokens()
        {
            using (MockHttpServer server = new MockHttpServer(req =>
            {
                Assert.Equal("POST", req.Method);
                Assert.Equal("/tokenize", req.Path);
                Assert.Contains("Hello world", req.Body);

                return new MockHttpServer.Response
                {
                    StatusCode = 200,
                    Body = "{\"text\":\"Hello world\",\"tokens\":[\"Hello\",\"world\"]}"
                };
            }))
            {
                SpacyTokenizer tokenizer = new SpacyTokenizer(server.Endpoint);
                TokenizationResult result = await tokenizer.Tokenize("Hello world");

                Assert.NotNull(result);
                Assert.Equal("Hello world", result.Text);
                Assert.Equal(2, result.Tokens.Count);
                Assert.Equal("Hello", result.Tokens[0]);
                Assert.Equal("world", result.Tokens[1]);
            }
        }

        [Fact]
        public async Task Tokenize_SingleText_ServerReturnsError_ReturnsNull()
        {
            using (MockHttpServer server = new MockHttpServer(req =>
                new MockHttpServer.Response { StatusCode = 500, Body = String.Empty }))
            {
                SpacyTokenizer tokenizer = new SpacyTokenizer(server.Endpoint);
                TokenizationResult result = await tokenizer.Tokenize("Hello world");
                Assert.Null(result);
            }
        }

        [Fact]
        public async Task Tokenize_NullText_Throws()
        {
            SpacyTokenizer tokenizer = new SpacyTokenizer();
            await Assert.ThrowsAsync<ArgumentNullException>(() => tokenizer.Tokenize((string)null));
        }

        [Fact]
        public async Task Tokenize_EmptyText_Throws()
        {
            SpacyTokenizer tokenizer = new SpacyTokenizer();
            await Assert.ThrowsAsync<ArgumentNullException>(() => tokenizer.Tokenize(String.Empty));
        }

        #endregion

        #region Tokenize-Batch

        [Fact]
        public async Task Tokenize_Batch_ReturnsResults()
        {
            using (MockHttpServer server = new MockHttpServer(req =>
            {
                Assert.Equal("POST", req.Method);
                Assert.Equal("/tokenize", req.Path);

                return new MockHttpServer.Response
                {
                    StatusCode = 200,
                    Body = "{\"results\":[" +
                           "{\"text\":\"one two\",\"tokens\":[\"one\",\"two\"]}," +
                           "{\"text\":\"three\",\"tokens\":[\"three\"]}]}"
                };
            }))
            {
                SpacyTokenizer tokenizer = new SpacyTokenizer(server.Endpoint);
                List<string> texts = new List<string> { "one two", "three" };
                BatchTokenizationResult result = await tokenizer.Tokenize(texts);

                Assert.NotNull(result);
                Assert.NotNull(result.Results);
                Assert.Equal(2, result.Results.Count);
                Assert.Equal(2, result.Results[0].Tokens.Count);
                Assert.Single(result.Results[1].Tokens);
                Assert.Equal("three", result.Results[1].Tokens[0]);
            }
        }

        [Fact]
        public async Task Tokenize_Batch_ServerReturnsError_ReturnsNull()
        {
            using (MockHttpServer server = new MockHttpServer(req =>
                new MockHttpServer.Response { StatusCode = 400, Body = String.Empty }))
            {
                SpacyTokenizer tokenizer = new SpacyTokenizer(server.Endpoint);
                List<string> texts = new List<string> { "one", "two" };
                BatchTokenizationResult result = await tokenizer.Tokenize(texts);
                Assert.Null(result);
            }
        }

        [Fact]
        public async Task Tokenize_NullList_Throws()
        {
            SpacyTokenizer tokenizer = new SpacyTokenizer();
            await Assert.ThrowsAsync<ArgumentNullException>(() => tokenizer.Tokenize((List<string>)null));
        }

        [Fact]
        public async Task Tokenize_EmptyList_Throws()
        {
            SpacyTokenizer tokenizer = new SpacyTokenizer();
            await Assert.ThrowsAsync<ArgumentNullException>(() => tokenizer.Tokenize(new List<string>()));
        }

        [Fact]
        public async Task Tokenize_Cancelled_Throws()
        {
            using (MockHttpServer server = new MockHttpServer(req =>
                new MockHttpServer.Response { StatusCode = 200, Body = "{\"text\":\"x\",\"tokens\":[\"x\"]}" }))
            {
                SpacyTokenizer tokenizer = new SpacyTokenizer(server.Endpoint);
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => tokenizer.Tokenize("Hello", cts.Token));
            }
        }

        #endregion
    }
}
