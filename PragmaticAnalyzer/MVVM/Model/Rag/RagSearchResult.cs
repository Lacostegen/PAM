namespace PragmaticAnalyzer.MVVM.Model.Rag
{
    public class RagSearchResult
    {
        public RagDocument Document { get; set; }

        public double Score { get; set; }

        public string MatchedText { get; set; } = string.Empty;

        public RagSearchResult(RagDocument document, double score, string matchedText = "")
        {
            Document = document;
            Score = score;
            MatchedText = matchedText;
        }
    }
}