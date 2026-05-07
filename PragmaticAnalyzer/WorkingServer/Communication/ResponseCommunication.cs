using PragmaticAnalyzer.WorkingServer.Translate;
using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.WorkingServer.Communication
{
    public class ResponseCommunication
    {
        [JsonPropertyName("results")]
        public Result[] Results { get; set; }

        [JsonPropertyName("context")]
        public Context Context { get; set; }
    }
}