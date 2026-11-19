using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.Services.SemanticSearch
{
    public sealed class SemanticSearchResult
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("matches")]
        public ObservableCollection<SemanticSearchMatch> Matches { get; set; } = [];
    }

    public sealed class SemanticSearchMatch
    {
        [JsonPropertyName("coefficient")]
        public float Coefficient { get; set; }

        [JsonPropertyName("confidence")]
        public string Confidence { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("vulnerabilityGuid")]
        public Guid? VulnerabilityGuid { get; set; }

        [JsonPropertyName("threatGuid")]
        public Guid? ThreatGuid { get; set; }

        [JsonPropertyName("vulnerabilityScore")]
        public float VulnerabilityScore { get; set; }

        [JsonPropertyName("threatScore")]
        public float ThreatScore { get; set; }
    }
}
