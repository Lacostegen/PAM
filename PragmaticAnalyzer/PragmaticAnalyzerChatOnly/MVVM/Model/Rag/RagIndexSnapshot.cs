using System;
using System.Collections.Generic;

namespace PragmaticAnalyzer.MVVM.Model.Rag
{
    public class RagIndexSnapshot
    {
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int DocumentCount { get; set; }

        public List<RagDocument> Documents { get; set; } = new();
    }
}