namespace SpacyTokenizerSdk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Tokenization result.
    /// </summary>
    public class TokenizationResult
    {
        /// <summary>
        /// Text.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = null;

        /// <summary>
        /// Tokens.
        /// </summary>
        [JsonPropertyName("tokens")]
        public List<string> Tokens { get; set; } = new List<string>();

        /// <summary>
        /// Tokenization result.
        /// </summary>
        public TokenizationResult()
        {

        }
    }
}
