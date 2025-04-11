namespace SpacyTokenizerSdk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Tokenization request.
    /// </summary>
    public class TokenizationRequest
    {
        /// <summary>
        /// Text.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = null;

        /// <summary>
        /// Texts.
        /// </summary>
        [JsonPropertyName("texts")]
        public List<string> Texts { get; set; } = null;

        /// <summary>
        /// Tokenization request.
        /// </summary>
        public TokenizationRequest()
        {

        }
    }
}
