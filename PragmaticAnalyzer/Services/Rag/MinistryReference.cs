using System.IO;
using System.Text.RegularExpressions;
using PragmaticAnalyzer.MVVM.Model.Rag;

namespace PragmaticAnalyzer.Services.Rag;

internal static class MinistryReference
{
    private static readonly Lazy<List<RagDocument>> Sections = new(() =>
    {
        using var stream = typeof(MinistryReference).Assembly.GetManifestResourceStream(
            "PragmaticAnalyzer.References.DefenceMinistry.md") ?? throw new InvalidDataException("Не найдена справка о Минобороны России.");
        using var reader = new StreamReader(stream);
        return Regex.Split(reader.ReadToEnd().Replace("\r", ""), "(?m)^## ")
            .Skip(1).Select(section =>
            {
                var lines = section.Trim().Split('\n');
                return new RagDocument
                {
                    Title = "Минобороны России: краткая справка", Section = lines[0],
                    Source = lines[^1]["Источник: ".Length..], Type = "Справка",
                    PromptText = string.Join("\n", lines[1..^1]).Trim()
                };
            }).ToList();
    });

    internal static bool IsRelevant(string query) => Regex.IsMatch(query,
        @"(?i)\b(?:мо\s+рф|(?:минобороны|министерств[а-яё]*\s+обороны)(?:\s+(?:рф|россии|российской\s+федерации)|(?=\s*(?:[?.!,;:]|$))))\b");

    internal static List<RagSearchResult> Find(string query)
    {
        var topics = new List<string>();
        void Match(string pattern, string section)
        {
            if (Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase)) topics.Add(section);
        }
        Match("фстэк|экспортн", "Минобороны и ФСТЭК");
        Match(@"возглав|подчин|руковод|министр\b|министром", "Руководство");
        Match("структур|устроен|состав|военкомат|комиссариат|подразделени", "Структура");
        Match("задач|функци|полномочи|занимается|обязанност", "Основные задачи");
        Match("статус|расшифр|что такое|что означает", "Статус");

        bool Relevant(RagDocument d) => topics.Any(topic => d.Section.StartsWith(topic, StringComparison.Ordinal));
        var overview = topics.Count == 0 || Regex.IsMatch(query, "(?i)обзор|в целом|общие сведения|общую информацию");
        return Sections.Value.Where(d => overview || Relevant(d)).OrderByDescending(Relevant)
            .Select(d => new RagSearchResult(d, Relevant(d) ? 2 : 1)).ToList();
    }
}
