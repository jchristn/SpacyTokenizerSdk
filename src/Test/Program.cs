namespace Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SpacyTokenizerSdk;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SpacyTokenizer Test Program");
            Console.WriteLine("==========================");

            // Define the endpoint (use command line arg if provided, otherwise use default)
            string endpoint = args.Length > 0 ? args[0] : "http://localhost:8000/";
            Console.WriteLine($"Using endpoint: {endpoint}");

            try
            {
                // Initialize the tokenizer
                SpacyTokenizer tokenizer = new SpacyTokenizer(endpoint);

                // Check connectivity
                Console.WriteLine("\nValidating connectivity...");
                bool isConnected = await tokenizer.ValidateConnectivity();
                if (!isConnected)
                {
                    Console.WriteLine("ERROR: Could not connect to the spaCy tokenizer service.");
                    Console.WriteLine("Make sure the service is running and the endpoint is correct.");
                    return;
                }
                Console.WriteLine("Connection successful!");

                // Test single text tokenization
                await TestSingleTokenization(tokenizer);

                // Test batch tokenization
                await TestBatchTokenization(tokenizer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestSingleTokenization(SpacyTokenizer tokenizer)
        {
            Console.WriteLine("\n--- Single Text Tokenization Test ---");

            try
            {
                string text = "Hello world! This is a test sentence for the spaCy tokenizer.";
                Console.WriteLine($"Text to tokenize: \"{text}\"");

                // Tokenize single text
                Console.WriteLine("Tokenizing...");
                var result = await tokenizer.Tokenize(text);

                // Display results
                if (result != null)
                {
                    DisplayTokenizationResult(result);
                }
                else
                {
                    Console.WriteLine("Tokenization returned null result.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Single tokenization error: {ex.Message}");
            }
        }

        private static async Task TestBatchTokenization(SpacyTokenizer tokenizer)
        {
            Console.WriteLine("\n--- Batch Tokenization Test ---");

            try
            {
                List<string> texts = new List<string>
                {
                    "This is the first sentence in the batch.",
                    "Here's a second sentence with some punctuation!",
                    "And a third one - with various symbols, numbers (123) and special characters."
                };

                Console.WriteLine($"Number of texts to tokenize: {texts.Count}");
                for (int i = 0; i < texts.Count; i++)
                {
                    Console.WriteLine($"Text {i + 1}: \"{texts[i]}\"");
                }

                // Tokenize batch of texts
                Console.WriteLine("\nTokenizing batch...");
                var batchResult = await tokenizer.Tokenize(texts);

                // Display results
                if (batchResult != null && batchResult.Results != null)
                {
                    Console.WriteLine($"Received {batchResult.Results.Count} tokenization results:");

                    for (int i = 0; i < batchResult.Results.Count; i++)
                    {
                        Console.WriteLine($"\nResult {i + 1}:");
                        DisplayTokenizationResult(batchResult.Results[i]);
                    }
                }
                else
                {
                    Console.WriteLine("Batch tokenization returned null result.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Batch tokenization error: {ex.Message}");
            }
        }

        private static void DisplayTokenizationResult(TokenizationResult result)
        {
            Console.WriteLine($"Original text: \"{result.Text}\"");

            if (result.Tokens != null && result.Tokens.Count > 0)
            {
                Console.WriteLine($"Found {result.Tokens.Count} tokens:");

                // Create a formatted display of the tokens
                for (int i = 0; i < result.Tokens.Count; i++)
                {
                    Console.WriteLine($"  Token {i + 1}: \"{result.Tokens[i]}\"");
                }
            }
            else
            {
                Console.WriteLine("No tokens found in the result.");
            }
        }
    }
}