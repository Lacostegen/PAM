using System;

namespace PragmaticAnalyzer.MVVM.Model.Rag
{
    public class RagIndexSourceInfo
    {
        public string Path { get; set; } = string.Empty;

        public string SourceKind { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        public DateTime LastWriteTimeUtc { get; set; }

        public int DocumentCount { get; set; }
    }
}