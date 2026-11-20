using System.Text.RegularExpressions;

namespace PragmaticAnalyzer.Services.Rag;

public static class RagAnswerSourcesFormatter
{
    public static string Append(string answer, string context)
    {
        if (string.IsNullOrWhiteSpace(answer)) return answer;

        var blocks = Regex.Matches(context ?? string.Empty,
            @"(?ms)^\[(\d+)\]\r?\n(.*?)(?=^\[\d+\]\r?$|\z)");
        var sources = new List<(string Number, string Description)>();
        foreach (Match block in blocks)
        {
            var body = block.Groups[2].Value;
            // A budget cut may leave metadata without any document excerpt.
            if (!Regex.IsMatch(body, @"(?m)^Фрагмент:\r?\n\s*\S")) continue;
            string Field(string name) => Regex.Match(body,
                @"(?m)^" + Regex.Escape(name) + @":\s*([^\r\n]+)").Groups[1].Value.Trim();
            var source = Field("Источник");
            var title = Field("Название");
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(source)) parts.Add(source);
            if (!string.IsNullOrWhiteSpace(title) && title != source) parts.Add(title);
            if (parts.Count == 0) continue;
            var section = Field("Раздел");
            var page = Field("Страница");
            var id = Field("ID");
            if (!string.IsNullOrWhiteSpace(section)) parts.Add("раздел: " + section);
            if (!string.IsNullOrWhiteSpace(page)) parts.Add("стр. " + page);
            if (!string.IsNullOrWhiteSpace(id)) parts.Add("ID: " + id);
            sources.Add((block.Groups[1].Value, string.Join("; ", parts)));
        }

        if (sources.Count == 0)
            return answer.TrimEnd() + "\n\nИсточники: ответ сформирован на основе знаний модели. Документальные источники в текущий запрос не передавались.";

        var cited = sources.Where(source => Regex.IsMatch(answer,
            @"\[" + Regex.Escape(source.Number) + @"\]")).ToList();
        var heading = cited.Count > 0 ? "Источники по ссылкам в ответе:" : "Материалы, переданные модели (использование в ответе не подтверждено):";
        var selected = cited.Count > 0 ? cited : sources;
        return answer.TrimEnd() + "\n\n" + heading + "\n" +
            string.Join("\n", selected.Select(source => $"[{source.Number}] {source.Description}"));
    }
}
