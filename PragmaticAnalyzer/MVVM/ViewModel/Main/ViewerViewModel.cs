using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class ViewerViewModel : ViewModelBase
    {
        private const string SearchModeContains = "Содержит текст";
        private const string SearchModeWholeWord = "Слово целиком";
        private const string SearchModeAllWords = "Все слова";
        private const string SearchModeAnyWord = "Любое слово";

        private static readonly string[] TitlePropertyPriority =
        [
            "Identifier",
            "DisplayedId",
            "Id",
            "IndexValue",
            "Name",
            "FullName",
            "GroupName",
            "Number",
            "MethodName",
            "NameOrgan",
            "Description"
        ];

        private static readonly HashSet<string> SkippedPropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "GuidId",
            "OpenFileCommand"
        };

        private readonly Action<object> _setCurrentView;

        public IInfrastructureOrchestrator ViewModelsService { get => Get<IInfrastructureOrchestrator>(); private set => Set(value); }
        public LastUpdateConfig LastUpdateConfig { get => Get<LastUpdateConfig>(); set => Set(value); }
        public ObservableCollection<string> DatabaseSearchModes { get; } =
        [
            SearchModeContains,
            SearchModeWholeWord,
            SearchModeAllWords,
            SearchModeAnyWord
        ];

        public string DatabaseSearchText
        {
            get => Get<string>() ?? string.Empty;
            set => Set(value);
        }

        public string SelectedDatabaseSearchMode
        {
            get => Get<string>() ?? SearchModeContains;
            set
            {
                Set(string.IsNullOrWhiteSpace(value) ? SearchModeContains : value);
                NotifyPropertyChanged(nameof(DatabaseSearchModeButtonText));
            }
        }

        public string DatabaseSearchModeButtonText => SelectedDatabaseSearchMode switch
        {
            SearchModeWholeWord => "Слово",
            SearchModeAllWords => "Все",
            SearchModeAnyWord => "Любое",
            _ => "Текст"
        };

        public ViewerViewModel(
            IInfrastructureOrchestrator viewModelsService,
            LastUpdateConfig lastUpdateConfig,
            Action<object> setCurrentView)
        {
            ViewModelsService = viewModelsService;
            LastUpdateConfig = lastUpdateConfig;
            _setCurrentView = setCurrentView;
            SelectedDatabaseSearchMode = SearchModeContains;
        }

        public RelayCommand SetCurrentViewCommand => GetCommand(vm =>
        {
            if (vm is not null)
            {
                _setCurrentView?.Invoke(vm);
            }
        });

        public RelayCommand SetDatabaseSearchModeCommand => GetCommand(mode =>
        {
            if (mode is string selectedMode)
            {
                SelectedDatabaseSearchMode = selectedMode;
            }
        });

        public RelayCommand FindDatabaseSearchCommand => GetCommand(o =>
        {
            var query = DatabaseSearchText.Trim();
            var results = SearchDatabases(query);
            _setCurrentView?.Invoke(new DatabaseSearchResultsViewModel(
                query,
                SelectedDatabaseSearchMode,
                results,
                this,
                _setCurrentView));
        }, o => !string.IsNullOrWhiteSpace(DatabaseSearchText));

        private ObservableCollection<DatabaseSearchResult> SearchDatabases(string query)
        {
            return SearchSources(GetDatabaseSources(), query, SelectedDatabaseSearchMode);
        }

        public static ObservableCollection<DatabaseSearchResult> SearchSources(
            IEnumerable<DatabaseSearchSource> sources,
            string query,
            string searchMode)
        {
            var results = new ObservableCollection<DatabaseSearchResult>();

            foreach (var source in sources)
            {
                foreach (var item in source.Items)
                {
                    var fields = BuildFields(item);
                    if (fields.Count == 0)
                    {
                        continue;
                    }

                    var searchableText = BuildSearchText(source.Name, fields);
                    if (!MatchesSearch(searchableText, query, searchMode))
                    {
                        continue;
                    }

                    ApplyHighlights(fields, query, searchMode);
                    var snippet = BuildSnippet(fields, query, searchMode);

                    results.Add(new DatabaseSearchResult(
                        source.Name,
                        BuildTitle(item, source.Name, fields),
                        snippet,
                        BuildHighlightedTextSegments(snippet, query, searchMode),
                        fields));
                }
            }

            return results;
        }

        private IEnumerable<DatabaseSearchSource> GetDatabaseSources()
        {
            yield return new("БД Уязвимостей ФСТЭК", ViewModelsService.VulnerabilitieVm.VulnerabilitiesFstec);
            yield return new("БД Уязвимостей NVD", ViewModelsService.VulnerabilitieVm.VulnerabilitiesNvd);
            yield return new("БД Уязвимостей NVD (рус.)", ViewModelsService.VulnerabilitieVm.VulnerabilitiesNvdTranslated);
            yield return new("БД Уязвимостей JVN", ViewModelsService.VulnerabilitieVm.VulnerabilitiesJvn);
            yield return new("БД Уязвимостей JVN (рус.)", ViewModelsService.VulnerabilitieVm.VulnerabilitiesJvnTranslated);
            yield return new("БД Угроз", ViewModelsService.ThreatVm.Threats);
            yield return new("БД Техник и тактик", ViewModelsService.TacticVm.Tactics);
            yield return new("БД Рисков ИБ: технологии оценки", ViewModelsService.OutcomeVm.Outcomes.Technologys);
            yield return new("БД Рисков ИБ: негативные последствия", ViewModelsService.OutcomeVm.Outcomes.Consequences);
            yield return new("БД Эксплойтов", ViewModelsService.ExploitVm.Exploits);
            yield return new("БД Нарушителей", ViewModelsService.ViolatorVm.Violators);
            yield return new("БД Специалистов по ЗИ", ViewModelsService.SpecialistVm.Specialists);
            yield return new("БД Мер защиты", ViewModelsService.ProtectionMeasureVm.ProtectionMeasures);
            yield return new("БД Эталонного состояния", ViewModelsService.ReferenceStatusVm.ReferencesStatus);
            yield return new("БД Текущего состояния", ViewModelsService.CurrentStatusVm.CurrentsStatus);
            yield return new("БД Онтологий", ViewModelsService.OntologyVm.Ontologys);

            foreach (var database in ViewModelsService.CreatorVm.Databases)
            {
                yield return new($"БД {database.Name}", database.Records);
            }
        }

        private static ObservableCollection<DatabaseSearchField> BuildFields(object item)
        {
            if (item is DynamicRecord dynamicRecord)
            {
                var dynamicFields = new ObservableCollection<DatabaseSearchField>
                {
                    new("Индекс", dynamicRecord.IndexValue),
                    new("Описание", dynamicRecord.Description ?? string.Empty)
                };

                foreach (var field in dynamicRecord.Fields)
                {
                    dynamicFields.Add(new(field.Key, field.Value));
                }

                return dynamicFields;
            }

            var fields = new ObservableCollection<DatabaseSearchField>();
            foreach (var property in item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead ||
                    property.GetIndexParameters().Length > 0 ||
                    SkippedPropertyNames.Contains(property.Name))
                {
                    continue;
                }

                var value = property.GetValue(item);
                var formattedValue = FormatValue(value);
                if (!string.IsNullOrWhiteSpace(formattedValue))
                {
                    fields.Add(new(GetPropertyLabel(property), formattedValue, property.Name));
                }
            }

            return fields;
        }

        private static string BuildTitle(
            object item,
            string sourceName,
            ObservableCollection<DatabaseSearchField> fields)
        {
            if (item is DynamicRecord dynamicRecord)
            {
                return dynamicRecord.IndexValue;
            }

            foreach (var propertyName in TitlePropertyPriority)
            {
                var property = item.GetType().GetProperty(propertyName);
                if (property is null || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var value = FormatValue(property.GetValue(item));
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Limit(value, 120);
                }
            }

            return Limit(fields.FirstOrDefault()?.Value ?? sourceName, 120);
        }

        private static string BuildSearchText(
            string sourceName,
            ObservableCollection<DatabaseSearchField> fields)
        {
            var builder = new StringBuilder(sourceName);
            foreach (var field in fields)
            {
                builder.AppendLine();
                builder.Append(field.Label);
                builder.Append(": ");
                builder.Append(field.Value);
            }

            return builder.ToString();
        }

        private static string BuildSnippet(
            ObservableCollection<DatabaseSearchField> fields,
            string query,
            string mode)
        {
            var terms = GetSearchTerms(query, mode).ToList();
            foreach (var field in fields)
            {
                if (terms.Any(term => field.Value.Contains(term, StringComparison.CurrentCultureIgnoreCase)))
                {
                    return $"{field.Label}: {MakeExcerpt(field.Value, terms)}";
                }
            }

            var firstTextField = fields.FirstOrDefault(field => field.Value.Length > 0);
            return firstTextField is null
                ? string.Empty
                : $"{firstTextField.Label}: {Limit(NormalizeSpaces(firstTextField.Value), 220)}";
        }

        private static void ApplyHighlights(
            ObservableCollection<DatabaseSearchField> fields,
            string query,
            string mode)
        {
            foreach (var field in fields)
            {
                field.HighlightedValueSegments = BuildHighlightedTextSegments(field.Value, query, mode);
            }
        }

        private static ObservableCollection<HighlightedTextSegment> BuildHighlightedTextSegments(
            string value,
            string query,
            string mode)
        {
            var segments = new ObservableCollection<HighlightedTextSegment>();
            if (string.IsNullOrEmpty(value))
            {
                segments.Add(new HighlightedTextSegment(string.Empty, false));
                return segments;
            }

            var pattern = BuildHighlightPattern(query, mode);
            if (string.IsNullOrWhiteSpace(pattern))
            {
                segments.Add(new HighlightedTextSegment(value, false));
                return segments;
            }

            var matches = Regex.Matches(value, pattern, RegexOptions.IgnoreCase);
            var currentIndex = 0;

            foreach (Match match in matches)
            {
                if (!match.Success || match.Length == 0 || match.Index < currentIndex)
                {
                    continue;
                }

                if (match.Index > currentIndex)
                {
                    segments.Add(new HighlightedTextSegment(
                        value[currentIndex..match.Index],
                        false));
                }

                segments.Add(new HighlightedTextSegment(match.Value, true));
                currentIndex = match.Index + match.Length;
            }

            if (currentIndex < value.Length)
            {
                segments.Add(new HighlightedTextSegment(value[currentIndex..], false));
            }

            if (segments.Count == 0)
            {
                segments.Add(new HighlightedTextSegment(value, false));
            }

            return segments;
        }

        private static string BuildHighlightPattern(string query, string mode)
        {
            var terms = GetSearchTerms(query, mode)
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderByDescending(term => term.Length)
                .Select(term => Regex.Escape(term).Replace("\\ ", "\\s+"))
                .ToList();

            if (terms.Count == 0)
            {
                return string.Empty;
            }

            if (mode == SearchModeWholeWord)
            {
                return $@"(?<![\p{{L}}\p{{N}}_])(?:{string.Join("|", terms)})(?![\p{{L}}\p{{N}}_])";
            }

            return string.Join("|", terms);
        }

        private static IEnumerable<string> GetSearchTerms(string query, string mode)
        {
            if (mode == SearchModeAllWords || mode == SearchModeAnyWord)
            {
                return Regex.Split(query.Trim(), @"\s+")
                    .Where(term => !string.IsNullOrWhiteSpace(term));
            }

            return [NormalizeSpaces(query)];
        }

        private static string MakeExcerpt(string value, IEnumerable<string> terms)
        {
            var normalizedValue = NormalizeSpaces(value);
            var firstTerm = terms.FirstOrDefault(term => !string.IsNullOrWhiteSpace(term));
            if (string.IsNullOrWhiteSpace(firstTerm))
            {
                return Limit(normalizedValue, 220);
            }

            var index = normalizedValue.IndexOf(firstTerm, StringComparison.CurrentCultureIgnoreCase);
            if (index < 0)
            {
                return Limit(normalizedValue, 220);
            }

            var start = Math.Max(0, index - 70);
            var length = Math.Min(normalizedValue.Length - start, firstTerm.Length + 150);
            var excerpt = normalizedValue.Substring(start, length);

            if (start > 0)
            {
                excerpt = "..." + excerpt;
            }

            if (start + length < normalizedValue.Length)
            {
                excerpt += "...";
            }

            return excerpt;
        }

        private static bool MatchesSearch(string text, string query, string mode)
        {
            var normalizedQuery = NormalizeSpaces(query);
            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return false;
            }

            return mode switch
            {
                SearchModeWholeWord => ContainsWholeWordOrPhrase(text, normalizedQuery),
                SearchModeAllWords => Regex.Split(normalizedQuery, @"\s+")
                    .All(term => text.Contains(term, StringComparison.CurrentCultureIgnoreCase)),
                SearchModeAnyWord => Regex.Split(normalizedQuery, @"\s+")
                    .Any(term => text.Contains(term, StringComparison.CurrentCultureIgnoreCase)),
                _ => text.Contains(normalizedQuery, StringComparison.CurrentCultureIgnoreCase)
            };
        }

        private static bool ContainsWholeWordOrPhrase(string text, string query)
        {
            var escapedQuery = Regex.Escape(query).Replace("\\ ", "\\s+");
            var pattern = $@"(?<![\p{{L}}\p{{N}}_]){escapedQuery}(?![\p{{L}}\p{{N}}_])";
            return Regex.IsMatch(NormalizeSpaces(text), pattern, RegexOptions.IgnoreCase);
        }

        private static string GetPropertyLabel(PropertyInfo property)
        {
            var description = property.GetCustomAttribute<DescriptionAttribute>();
            return string.IsNullOrWhiteSpace(description?.Description)
                ? property.Name
                : description.Description;
        }

        private static string FormatValue(object? value, int depth = 0)
        {
            if (value is null || depth > 2)
            {
                return string.Empty;
            }

            if (value is string stringValue)
            {
                return stringValue;
            }

            var type = value.GetType();
            if (type.IsPrimitive || value is decimal || value is DateTime || value is Guid)
            {
                return value.ToString() ?? string.Empty;
            }

            if (value is IDictionary dictionary)
            {
                var parts = new List<string>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    var entryValue = FormatValue(entry.Value, depth + 1);
                    if (!string.IsNullOrWhiteSpace(entryValue))
                    {
                        parts.Add($"{entry.Key}: {entryValue}");
                    }
                }

                return string.Join("; ", parts);
            }

            if (value is IEnumerable enumerable)
            {
                var parts = new List<string>();
                foreach (var item in enumerable)
                {
                    var itemValue = FormatValue(item, depth + 1);
                    if (!string.IsNullOrWhiteSpace(itemValue))
                    {
                        parts.Add(itemValue);
                    }
                }

                return string.Join("; ", parts);
            }

            if (value is ViewModelBase)
            {
                var parts = new List<string>();
                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!property.CanRead ||
                        property.GetIndexParameters().Length > 0 ||
                        SkippedPropertyNames.Contains(property.Name))
                    {
                        continue;
                    }

                    var formattedValue = FormatValue(property.GetValue(value), depth + 1);
                    if (!string.IsNullOrWhiteSpace(formattedValue))
                    {
                        parts.Add(formattedValue);
                    }
                }

                return string.Join("; ", parts);
            }

            return value.ToString() ?? string.Empty;
        }

        private static string NormalizeSpaces(string value)
        {
            return Regex.Replace(value.Trim(), @"\s+", " ");
        }

        private static string Limit(string value, int maxLength)
        {
            var normalizedValue = NormalizeSpaces(value);
            return normalizedValue.Length <= maxLength
                ? normalizedValue
                : normalizedValue[..Math.Max(0, maxLength - 3)] + "...";
        }
    }

    public class DatabaseSearchResultsViewModel : ViewModelBase
    {
        private readonly object _backViewModel;
        private readonly Action<object> _setCurrentView;

        public string Query { get; }
        public string SearchMode { get; }
        public ObservableCollection<DatabaseSearchResult> Results { get; }
        public string Summary => Results.Count == 0
            ? $"По запросу \"{Query}\" совпадений не найдено"
            : $"Запрос: \"{Query}\". Найдено результатов: {Results.Count}. Вариант поиска: {SearchMode}";

        public DatabaseSearchResultsViewModel(
            string query,
            string searchMode,
            ObservableCollection<DatabaseSearchResult> results,
            object backViewModel,
            Action<object> setCurrentView)
        {
            Query = query;
            SearchMode = searchMode;
            Results = results;
            _backViewModel = backViewModel;
            _setCurrentView = setCurrentView;
        }

        public RelayCommand BackCommand => GetCommand(o =>
        {
            _setCurrentView?.Invoke(_backViewModel);
        });

        public RelayCommand OpenResultCommand => GetCommand(result =>
        {
            if (result is DatabaseSearchResult searchResult)
            {
                _setCurrentView?.Invoke(new DatabaseSearchDetailsViewModel(this, searchResult, _setCurrentView));
            }
        });
    }

    public class DatabaseSearchDetailsViewModel : ViewModelBase
    {
        private readonly DatabaseSearchResultsViewModel _resultsViewModel;
        private readonly Action<object> _setCurrentView;

        public DatabaseSearchResult Result { get; }

        public DatabaseSearchDetailsViewModel(
            DatabaseSearchResultsViewModel resultsViewModel,
            DatabaseSearchResult result,
            Action<object> setCurrentView)
        {
            _resultsViewModel = resultsViewModel;
            Result = result;
            _setCurrentView = setCurrentView;
        }

        public RelayCommand BackCommand => GetCommand(o =>
        {
            _setCurrentView?.Invoke(_resultsViewModel);
        });
    }

    public class DatabaseSearchResult(
        string databaseName,
        string title,
        string snippet,
        ObservableCollection<HighlightedTextSegment> highlightedSnippetSegments,
        ObservableCollection<DatabaseSearchField> fields)
    {
        public string DatabaseName { get; } = databaseName;
        public string Title { get; } = title;
        public string Snippet { get; } = snippet;
        public ObservableCollection<HighlightedTextSegment> HighlightedSnippetSegments { get; } = highlightedSnippetSegments;
        public ObservableCollection<DatabaseSearchField> Fields { get; } = fields;
    }

    public class DatabaseSearchField(string label, string value, string? propertyName = null) : ViewModelBase
    {
        public string Label { get; } = label;
        public string Value { get; } = value;
        public string? PropertyName { get; } = propertyName;
        public ObservableCollection<HighlightedTextSegment> HighlightedValueSegments
        {
            get => Get<ObservableCollection<HighlightedTextSegment>>() ?? [new HighlightedTextSegment(value, false)];
            set => Set(value);
        }
    }

    public class HighlightedTextSegment(string text, bool isMatch)
    {
        public string Text { get; } = text;
        public bool IsMatch { get; } = isMatch;
    }

    public class DatabaseSearchSource(string name, IEnumerable items)
    {
        public string Name { get; } = name;
        public IEnumerable Items { get; } = items;
    }
}
