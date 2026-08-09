using PragmaticAnalyzer.MVVM.Model.Rag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PragmaticAnalyzer.Services.Rag
{
    public class RagDocumentFactory
    {
        public List<RagDocument> CreateFromDatabaseItems<T>(
            IEnumerable<T> items,
            string source,
            string type)
        {
            var documents = new List<RagDocument>();

            if (items == null)
            {
                return documents;
            }

            var index = 0;

            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }

                var document = CreateFromObject(
                    item,
                    sourceKind: "database",
                    source: source,
                    type: type,
                    product: string.Empty,
                    chunkIndex: index);

                if (!string.IsNullOrWhiteSpace(document.SearchText))
                {
                    documents.Add(document);
                    index++;
                }
            }

            return documents;
        }

        public RagDocument CreateFromManualChunk(
            string product,
            string source,
            string title,
            string section,
            string text,
            int page,
            int chunkIndex)
        {
            var safeProduct = product ?? string.Empty;
            var safeSource = source ?? string.Empty;
            var safeTitle = title ?? string.Empty;
            var safeSection = section ?? string.Empty;
            var safeText = text ?? string.Empty;

            var id = BuildManualId(safeProduct, safeTitle, page, chunkIndex);

            var searchText = BuildText(
                safeProduct,
                safeSource,
                safeTitle,
                safeSection,
                safeText);

            var promptText = BuildPromptText(
                source: safeSource,
                type: "manual",
                product: safeProduct,
                id: id,
                title: safeTitle,
                section: safeSection,
                page: page,
                body: safeText);

            return new RagDocument
            {
                Id = id,
                SourceKind = "manual",
                Source = safeSource,
                Type = "manual",
                Product = safeProduct,
                Title = safeTitle,
                Section = safeSection,
                Page = page,
                ChunkIndex = chunkIndex,
                SearchText = searchText,
                PromptText = promptText,
                Metadata = new Dictionary<string, string>
                {
                    ["product"] = safeProduct,
                    ["source"] = safeSource,
                    ["section"] = safeSection,
                    ["page"] = page.ToString(),
                    ["chunkIndex"] = chunkIndex.ToString()
                }
            };
        }

        private static RagDocument CreateFromObject(
            object item,
            string sourceKind,
            string source,
            string type,
            string product,
            int chunkIndex)
        {
            var properties = item
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead)
                .ToList();

            var values = new Dictionary<string, string>();

            foreach (var property in properties)
            {
                var value = ReadPropertyValue(item, property);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    values[property.Name] = value;
                }
            }

            var id = GuessValue(values, "Id", "ID", "Identifier", "Code", "Cve", "CVE", "Bdu", "BDU", "Number");
            var title = GuessValue(values, "Name", "Title", "GroupName", "ThreatName", "VulnerabilityName", "FullName", "ShortName");
            var section = GuessValue(values, "Section", "Category", "Class", "Group", "Type");

            if (string.IsNullOrWhiteSpace(id))
            {
                id = $"{source}_{chunkIndex}";
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = id;
            }

            var searchText = BuildSearchText(source, type, product, values);
            var promptText = BuildDatabasePromptText(source, type, product, id, title, section, values);

            return new RagDocument
            {
                Id = id,
                SourceKind = sourceKind,
                Source = source,
                Type = type,
                Product = product,
                Title = title,
                Section = section,
                Page = 0,
                ChunkIndex = chunkIndex,
                SearchText = searchText,
                PromptText = promptText,
                Metadata = values
            };
        }

        private static string ReadPropertyValue(object item, PropertyInfo property)
        {
            try
            {
                var value = property.GetValue(item);

                if (value == null)
                {
                    return string.Empty;
                }

                if (value is string stringValue)
                {
                    return stringValue.Trim();
                }

                if (value is IEnumerable<string> stringCollection)
                {
                    return string.Join("; ", stringCollection.Where(x => !string.IsNullOrWhiteSpace(x)));
                }

                if (IsSimpleValue(value))
                {
                    return value.ToString()?.Trim() ?? string.Empty;
                }

                return value.ToString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsSimpleValue(object value)
        {
            var type = value.GetType();

            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(Guid);
        }

        private static string GuessValue(
            Dictionary<string, string> values,
            params string[] names)
        {
            foreach (var name in names)
            {
                var match = values.FirstOrDefault(pair =>
                    string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(match.Value))
                {
                    return match.Value;
                }
            }

            foreach (var name in names)
            {
                var match = values.FirstOrDefault(pair =>
                    pair.Key.Contains(name, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(match.Value))
                {
                    return match.Value;
                }
            }

            return string.Empty;
        }

        private static string BuildSearchText(
            string source,
            string type,
            string product,
            Dictionary<string, string> values)
        {
            var builder = new StringBuilder();

            builder.AppendLine(source);
            builder.AppendLine(type);
            builder.AppendLine(product);

            foreach (var pair in values)
            {
                builder.AppendLine(pair.Key);
                builder.AppendLine(pair.Value);
            }

            return builder.ToString().Trim();
        }

        private static string BuildDatabasePromptText(
            string source,
            string type,
            string product,
            string id,
            string title,
            string section,
            Dictionary<string, string> values)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Источник: {source}");
            builder.AppendLine($"Тип: {type}");

            if (!string.IsNullOrWhiteSpace(product))
            {
                builder.AppendLine($"Продукт: {product}");
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                builder.AppendLine($"ID: {id}");
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                builder.AppendLine($"Название: {title}");
            }

            if (!string.IsNullOrWhiteSpace(section))
            {
                builder.AppendLine($"Раздел/категория: {section}");
            }

            builder.AppendLine("Поля записи:");

            foreach (var pair in values.Take(20))
            {
                builder.AppendLine($"- {pair.Key}: {LimitText(pair.Value, 500)}");
            }

            return builder.ToString().Trim();
        }

        private static string BuildPromptText(
            string source,
            string type,
            string product,
            string id,
            string title,
            string section,
            int page,
            string body)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Источник: {source}");
            builder.AppendLine($"Тип: {type}");

            if (!string.IsNullOrWhiteSpace(product))
            {
                builder.AppendLine($"Продукт: {product}");
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                builder.AppendLine($"ID: {id}");
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                builder.AppendLine($"Название: {title}");
            }

            if (!string.IsNullOrWhiteSpace(section))
            {
                builder.AppendLine($"Раздел: {section}");
            }

            if (page > 0)
            {
                builder.AppendLine($"Страница: {page}");
            }

            builder.AppendLine("Фрагмент:");
            builder.AppendLine(LimitText(body, 1500));

            return builder.ToString().Trim();
        }

        private static string BuildText(params string[] parts)
        {
            return string.Join(
                Environment.NewLine,
                parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        private static string BuildManualId(
            string product,
            string title,
            int page,
            int chunkIndex)
        {
            var raw = $"{product}_{title}_p{page}_c{chunkIndex}";

            var cleaned = new string(raw
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
                .ToArray());

            while (cleaned.Contains("__"))
            {
                cleaned = cleaned.Replace("__", "_");
            }

            return cleaned.Trim('_');
        }

        private static string LimitText(string? text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.Trim();

            if (normalized.Length <= maxChars)
            {
                return normalized;
            }

            return normalized[..maxChars].Trim() + "...";
        }
    }
}