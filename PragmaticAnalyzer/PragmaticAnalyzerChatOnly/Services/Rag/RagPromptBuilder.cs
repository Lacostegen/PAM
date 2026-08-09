using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.MVVM.Model.Rag;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PragmaticAnalyzer.Services.Rag
{
    public class RagPromptBuilder
    {
        public string BuildContext(
            IEnumerable<RagSearchResult> results,
            RagConfig config)
        {
            config ??= new RagConfig();

            if (results == null)
            {
                return string.Empty;
            }

            var selectedResults = results
                .Where(result => result?.Document != null)
                .Take(config.TopK)
                .ToList();

            if (selectedResults.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            builder.AppendLine("Контекст из базы знаний PragmaticAnalyzer:");
            builder.AppendLine("Используй эти сведения только если они релевантны вопросу пользователя.");
            builder.AppendLine("Не выдумывай ID, версии, названия продуктов, CVE/BDU и настройки, которых нет в контексте.");
            builder.AppendLine("Не пересказывай нерелевантные определения и не создавай отдельный блок \"Факты из базы знаний\", если пользователь сам его не попросил.");
            builder.AppendLine();

            var totalChars = 0;
            var index = 1;

            foreach (var result in selectedResults)
            {
                var document = result.Document;

                var text = string.IsNullOrWhiteSpace(document.PromptText)
                    ? document.SearchText
                    : document.PromptText;

                text = LimitText(text, config.MaxCharsPerDocument);

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var block = BuildDocumentBlock(index, result, text);

                if (totalChars + block.Length > config.MaxTotalContextChars)
                {
                    break;
                }

                builder.AppendLine(block);
                totalChars += block.Length;
                index++;
            }

            return builder.ToString().Trim();
        }

        private static string BuildDocumentBlock(
            int index,
            RagSearchResult result,
            string text)
        {
            var document = result.Document;

            var builder = new StringBuilder();

            builder.AppendLine($"[{index}]");
            builder.AppendLine($"Источник: {document.Source}");
            builder.AppendLine($"Тип: {document.Type}");

            if (!string.IsNullOrWhiteSpace(document.Product))
            {
                builder.AppendLine($"Продукт: {document.Product}");
            }

            if (!string.IsNullOrWhiteSpace(document.Id))
            {
                builder.AppendLine($"ID: {document.Id}");
            }

            if (!string.IsNullOrWhiteSpace(document.Title))
            {
                builder.AppendLine($"Название: {document.Title}");
            }

            if (!string.IsNullOrWhiteSpace(document.Section))
            {
                builder.AppendLine($"Раздел: {document.Section}");
            }

            if (document.Page > 0)
            {
                builder.AppendLine($"Страница: {document.Page}");
            }

            builder.AppendLine($"Оценка релевантности: {result.Score:0.##}");
            builder.AppendLine("Фрагмент:");
            builder.AppendLine(text);
            builder.AppendLine();

            return builder.ToString();
        }

        private static string LimitText(string? text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            if (maxChars <= 0)
            {
                maxChars = 900;
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
