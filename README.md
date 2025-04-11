![SpacyTokenizerSdk](https://github.com/jchristn/spacytokenizersdk/blob/main/assets/icon.ico)

# spaCy Tokenizer SDK

spaCy tokenizer SDK.  This SDK uses the spaCy tokenizer docker image found [here](https://hub.docker.com/r/jchristn/spacytokenizer) (repository for the Docker image is [here](https://github.com/jchristn/spacytokenizer)).

## New in v1.0.x

- Initial release

## Help or Feedback

Need help or have feedback? Please file an issue here!

## Simple Example

```csharp
using SpacyTokenizerSdk;

SpacyTokenizer tokenizer = new SpacyTokenizer(endpoint);
bool connected = await tokenizer.ValidateConnectivity();

TokenizationResult result1 = await tokenizer.Tokenize("The quick brown fox jumped over the lazy dog");
// {"text":"The quick brown fox jumps over the lazy dog","tokens":["quick","brown","fox","jump","lazy","dog"]}

BatchTokenizationResult result2 = await tokenizer.Tokenize(new List<string> {
  "The quick brown fox jumps over the lazy dog",
  "Machine learning models are powerful tools"
});
// {"results":[{"text":"The quick brown fox jumps over the lazy dog","tokens":["quick","brown","fox","jump","lazy","dog"]},{"text":"Machine learning models are powerful tools","tokens":["machine","learning","model","powerful","tool"]}]}
```

## Version History

Please refer to CHANGELOG.md.
