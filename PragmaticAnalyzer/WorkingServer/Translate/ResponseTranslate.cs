using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.WorkingServer.Translate
{
    public class ResponseTranslate
    {
        [JsonPropertyName("results")]
        public Result[] Results { get; set; } // результат перевода текста

        [JsonPropertyName("context")]
        public Context Context { get; set; } // 
    } // ответ для запроса перевода текста 

    public class Result
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } //

        [JsonPropertyName("tokens")]
        public int Tokens { get; set; } //

        [JsonPropertyName("logprobs")]
        public object Logprobs { get; set; } //

        [JsonPropertyName("top_probs")]
        public object TopProbs { get; set; } //
    } //

    public class Context
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } //

        [JsonPropertyName("tokens")]
        public int Tokens { get; set; } //

        [JsonPropertyName("used_tokens")]
        public int UsedTokens { get; set; } //
    } //
}