using PragmaticAnalyzer.MVVM.Model.Rag;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PragmaticAnalyzer.Services.Rag
{
    public class ManualDocumentLoader
    {
        private readonly ManualChunker _chunker = new();

        private static readonly string[] SupportedExtensions =
        {
            ".txt",
            ".md"
        };

        public List<RagManualChunk> LoadManualChunks(string knowledgeBasePath)
        {
            var chunks = new List<RagManualChunk>();

            if (string.IsNullOrWhiteSpace(knowledgeBasePath))
            {
                return chunks;
            }

            var manualsRoot = Path.Combine(knowledgeBasePath, "Manuals");

            if (!Directory.Exists(manualsRoot))
            {
                return chunks;
            }

            chunks.AddRange(LoadProductManuals(
                Path.Combine(manualsRoot, "Kaspersky"),
                product: "Kaspersky",
                source: "manual_kaspersky"));

            chunks.AddRange(LoadProductManuals(
                Path.Combine(manualsRoot, "SecretNetStudio"),
                product: "Secret Net Studio",
                source: "manual_secret_net_studio"));

            chunks.AddRange(LoadProductManuals(
                Path.Combine(manualsRoot, "DrWeb"),
                product: "Dr.Web",
                source: "manual_drweb"));

            return chunks;
        }

        private List<RagManualChunk> LoadProductManuals(
            string productFolder,
            string product,
            string source)
        {
            var chunks = new List<RagManualChunk>();

            if (!Directory.Exists(productFolder))
            {
                return chunks;
            }

            var files = Directory
                .EnumerateFiles(productFolder, "*.*", SearchOption.AllDirectories)
                .Where(file => SupportedExtensions.Contains(
                    Path.GetExtension(file),
                    StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in files)
            {
                var text = ReadTextFile(file);

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var title = Path.GetFileNameWithoutExtension(file);

                var fileChunks = _chunker.SplitText(
                    text,
                    product,
                    source,
                    title);

                chunks.AddRange(fileChunks);
            }

            return chunks;
        }

        private static string ReadTextFile(string path)
        {
            try
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }
            catch
            {
                try
                {
                    return File.ReadAllText(path, Encoding.Default);
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
    }
}