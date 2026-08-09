using PragmaticAnalyzer.MVVM.Model.Rag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PragmaticAnalyzer.Services.Rag
{
    public class ManualChunker
    {
        private const int DefaultMaxChunkChars = 1400;
        private const int DefaultOverlapChars = 200;

        public List<RagManualChunk> SplitText(
            string text,
            string product,
            string source,
            string title,
            int maxChunkChars = DefaultMaxChunkChars,
            int overlapChars = DefaultOverlapChars)
        {
            var chunks = new List<RagManualChunk>();

            if (string.IsNullOrWhiteSpace(text))
            {
                return chunks;
            }

            if (maxChunkChars <= 0)
            {
                maxChunkChars = DefaultMaxChunkChars;
            }

            if (overlapChars < 0)
            {
                overlapChars = 0;
            }

            var normalizedText = NormalizeText(text);
            var paragraphs = SplitIntoParagraphs(normalizedText);

            var currentParts = new List<string>();
            var currentLength = 0;
            var chunkIndex = 0;
            var currentSection = string.Empty;

            foreach (var paragraph in paragraphs)
            {
                if (IsLikelyHeading(paragraph))
                {
                    currentSection = paragraph.Trim();
                }

                if (currentLength + paragraph.Length > maxChunkChars && currentParts.Count > 0)
                {
                    var chunkText = string.Join(Environment.NewLine + Environment.NewLine, currentParts).Trim();

                    chunks.Add(new RagManualChunk
                    {
                        Product = product,
                        Source = source,
                        Title = title,
                        Section = currentSection,
                        Text = chunkText,
                        Page = 0,
                        ChunkIndex = chunkIndex++
                    });

                    currentParts = BuildOverlapParts(chunkText, overlapChars);
                    currentLength = currentParts.Sum(part => part.Length);
                }

                currentParts.Add(paragraph);
                currentLength += paragraph.Length;
            }

            if (currentParts.Count > 0)
            {
                var chunkText = string.Join(Environment.NewLine + Environment.NewLine, currentParts).Trim();

                chunks.Add(new RagManualChunk
                {
                    Product = product,
                    Source = source,
                    Title = title,
                    Section = currentSection,
                    Text = chunkText,
                    Page = 0,
                    ChunkIndex = chunkIndex
                });
            }

            return chunks
                .Where(chunk => !string.IsNullOrWhiteSpace(chunk.Text))
                .ToList();
        }

        private static string NormalizeText(string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim();
        }

        private static List<string> SplitIntoParagraphs(string text)
        {
            return Regex
                .Split(text, @"\n\s*\n")
                .Select(paragraph => paragraph.Trim())
                .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
                .ToList();
        }

        private static bool IsLikelyHeading(string paragraph)
        {
            var text = paragraph.Trim();

            if (text.Length < 3 || text.Length > 140)
            {
                return false;
            }

            if (text.EndsWith("."))
            {
                return false;
            }

            if (Regex.IsMatch(text, @"^\d+(\.\d+)*\.?\s+\S+"))
            {
                return true;
            }

            return text == text.ToUpperInvariant() && text.Any(char.IsLetter);
        }

        private static List<string> BuildOverlapParts(string text, int overlapChars)
        {
            if (overlapChars <= 0 || string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            var start = Math.Max(0, text.Length - overlapChars);
            var overlap = text[start..].Trim();

            return string.IsNullOrWhiteSpace(overlap)
                ? new List<string>()
                : new List<string> { overlap };
        }
    }
}