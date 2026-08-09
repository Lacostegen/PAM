namespace PragmaticAnalyzer.MVVM.Model.Rag
{
    public class RagManualChunk
    {
        public string Product { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Section { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public int Page { get; set; }

        public int ChunkIndex { get; set; }
    }
}