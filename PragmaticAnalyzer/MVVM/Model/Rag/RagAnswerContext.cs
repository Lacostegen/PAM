using System.Collections.Generic;

namespace PragmaticAnalyzer.MVVM.Model.Rag
{
    public class RagAnswerContext
    {
        public string Query { get; set; } = string.Empty;

        public string ContextText { get; set; } = string.Empty;

        public List<RagSearchResult> Results { get; set; } = new();

        public bool HasResults => Results.Count > 0 && !string.IsNullOrWhiteSpace(ContextText);
    }
}