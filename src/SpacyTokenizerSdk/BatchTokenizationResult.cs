namespace SpacyTokenizerSdk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Batch tokenization result.
    /// </summary>
    public class BatchTokenizationResult
    {
        /// <summary>
        /// Results.
        /// </summary>
        [JsonPropertyName("results")]
        public List<TokenizationResult> Results { get; set; } = new List<TokenizationResult>();

        /// <summary>
        /// Batch tokenization result.
        /// </summary>
        public BatchTokenizationResult()
        {

        }
    }
}
