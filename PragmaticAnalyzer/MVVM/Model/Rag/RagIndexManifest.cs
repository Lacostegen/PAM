using System;
using System.Collections.Generic;

namespace PragmaticAnalyzer.MVVM.Model.Rag
{
    public class RagIndexManifest
    {
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public List<RagIndexSourceInfo> Sources { get; set; } = new();
    }
}