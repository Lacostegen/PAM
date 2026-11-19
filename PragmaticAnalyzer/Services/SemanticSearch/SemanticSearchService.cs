using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.WorkingServer.Core;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PragmaticAnalyzer.Services.SemanticSearch
{
    public sealed partial class SemanticSearchService
    {
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "и", "в", "во", "на", "с", "со", "по", "для", "от", "до", "из", "у", "к", "о", "об", "при",
            "что", "как", "это", "или", "а", "но", "после", "часть", "данные", "файл", "файлы", "компьютер"
        };

        private readonly string _engineDirectory;
        private readonly object _modelGate = new();
        private Dictionary<string, int>? _wordIndex;
        private float[]? _weights;
        private int _cols;

        public SemanticSearchService()
        {
            _engineDirectory = Path.Combine(Environment.CurrentDirectory, "SemanticSearchEngine");
        }

        public Task<Result<SemanticSearchResult>> SearchAsync(
            string query,
            ObservableCollection<VulnerabilitieFstec> vulnerabilities,
            ObservableCollection<Threat> threats,
            bool useCvss,
            CancellationToken ct = default)
        {
            return Task.Run(() => SearchCore(query, vulnerabilities, threats, useCvss, ct), ct);
        }

        private Result<SemanticSearchResult> SearchCore(
            string query,
            ObservableCollection<VulnerabilitieFstec> vulnerabilities,
            ObservableCollection<Threat> threats,
            bool useCvss,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(query))
                {
                    return Result<SemanticSearchResult>.Failure("Описание инцидента пустое.");
                }

                if (vulnerabilities.Count == 0)
                {
                    return Result<SemanticSearchResult>.Failure("База уязвимостей ФСТЭК не загружена.");
                }

                if (threats.Count == 0)
                {
                    return Result<SemanticSearchResult>.Failure("База угроз ФСТЭК не загружена.");
                }

                EnsureModelLoaded();

                var queryTokens = Tokenize(query);
                if (queryTokens.Count < 2 && !BduRegex().IsMatch(query) && !CveRegex().IsMatch(query))
                {
                    return Result<SemanticSearchResult>.Success(new SemanticSearchResult
                    {
                        Status = "empty",
                        Message = "Недостаточно данных для надежного поиска. Уточните описание инцидента."
                    });
                }

                var vulnDocs = vulnerabilities.Select(BuildVulnerabilityDocument).ToList();
                var threatDocs = threats.Select(BuildThreatDocument).ToList();
                var vulnTokens = vulnDocs.Select(Tokenize).ToList();
                var threatTokens = threatDocs.Select(Tokenize).ToList();
                var idf = BuildIdf(vulnTokens.Concat(threatTokens).Append(queryTokens));

                var queryVector = BuildVector(queryTokens, idf);
                if (Norm(queryVector) <= 0 && !BduRegex().IsMatch(query) && !CveRegex().IsMatch(query))
                {
                    return Result<SemanticSearchResult>.Success(new SemanticSearchResult
                    {
                        Status = "empty",
                        Message = "В запросе не найдено терминов, известных поисковой модели."
                    });
                }

                var vulnVectors = vulnTokens.Select(tokens => BuildVector(tokens, idf)).ToList();
                var threatVectors = threatTokens.Select(tokens => BuildVector(tokens, idf)).ToList();
                var queryTokenSet = queryTokens.ToHashSet(StringComparer.OrdinalIgnoreCase);

                var rankedVulnerabilities = new List<VulnerabilityCandidate>();
                for (var i = 0; i < vulnerabilities.Count; i++)
                {
                    var semantic = Math.Max(0, Dot(queryVector, vulnVectors[i]));
                    var (exact, reasons) = ExactScore(query, queryTokenSet, vulnerabilities[i], vulnTokens[i]);
                    var score = Math.Min(1, semantic * 0.72f + exact);
                    if (score >= 0.08f)
                    {
                        rankedVulnerabilities.Add(new VulnerabilityCandidate(i, score, reasons));
                    }
                }

                var topVulnerabilities = rankedVulnerabilities
                    .OrderByDescending(v => v.Score)
                    .Take(7)
                    .ToList();

                var matches = new List<SemanticSearchMatch>();
                var usedPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var candidate in topVulnerabilities)
                {
                    var vulnerability = vulnerabilities[candidate.Index];
                    var (needC, needI, needA, cvssSource) = RequiredCia(vulnerability);
                    var threatCandidates = new List<int>();

                    for (var i = 0; i < threats.Count; i++)
                    {
                        if (useCvss)
                        {
                            if (needC && !IsEnabled(threats[i].PrivacyViolation))
                            {
                                continue;
                            }
                            if (needI && !IsEnabled(threats[i].IntegrityViolation))
                            {
                                continue;
                            }
                            if (needA && !IsEnabled(threats[i].AccessibilityViolation))
                            {
                                continue;
                            }
                        }

                        threatCandidates.Add(i);
                    }

                    if (threatCandidates.Count == 0 && useCvss)
                    {
                        matches.Add(new SemanticSearchMatch
                        {
                            Coefficient = Round(candidate.Score),
                            Confidence = Confidence(candidate.Score),
                            Reason = JoinReasons("Найдена уязвимость, но угрозы по CVSS-фильтру не найдены.", candidate.Reasons),
                            VulnerabilityGuid = vulnerability.GuidId,
                            VulnerabilityScore = Round(candidate.Score)
                        });
                        continue;
                    }

                    foreach (var threatCandidate in threatCandidates
                        .Select(i => new { Index = i, Score = Dot(vulnVectors[candidate.Index], threatVectors[i]) })
                        .OrderByDescending(t => t.Score)
                        .Take(2))
                    {
                        var threat = threats[threatCandidate.Index];
                        var pairKey = $"{vulnerability.GuidId}:{threat.GuidId}";
                        if (!usedPairs.Add(pairKey))
                        {
                            continue;
                        }

                        var threatScore = Math.Max(0, threatCandidate.Score);
                        var combined = Math.Min(1, candidate.Score * 0.78f + threatScore * 0.22f);
                        var reasons = new List<string>(candidate.Reasons);
                        if (useCvss && !string.IsNullOrWhiteSpace(cvssSource))
                        {
                            reasons.Add($"CVSS указывает воздействия C={(needC ? 1 : 0)}, I={(needI ? 1 : 0)}, A={(needA ? 1 : 0)}");
                        }
                        reasons.Add("угроза подобрана по близости к описанию уязвимости");

                        matches.Add(new SemanticSearchMatch
                        {
                            Coefficient = Round(combined),
                            Confidence = Confidence(combined),
                            Reason = string.Join(". ", reasons),
                            VulnerabilityGuid = vulnerability.GuidId,
                            ThreatGuid = threat.GuidId,
                            VulnerabilityScore = Round(candidate.Score),
                            ThreatScore = Round(threatScore)
                        });
                    }
                }

                matches = matches.OrderByDescending(m => m.Coefficient).Take(10).ToList();
                if (matches.Count == 0)
                {
                    return Result<SemanticSearchResult>.Success(new SemanticSearchResult
                    {
                        Status = "empty",
                        Message = "Надежных совпадений не найдено. Уточните описание инцидента или отключите CVSS-фильтр."
                    });
                }

                return Result<SemanticSearchResult>.Success(new SemanticSearchResult
                {
                    Status = "ok",
                    Message = $"Найдено вариантов: {matches.Count}",
                    Matches = new ObservableCollection<SemanticSearchMatch>(matches)
                });
            }
            catch (Exception ex)
            {
                return Result<SemanticSearchResult>.Failure($"Ошибка GloVe + CVSS: {ex.Message}");
            }
        }

        private void EnsureModelLoaded()
        {
            if (_wordIndex is not null && _weights is not null)
            {
                return;
            }

            lock (_modelGate)
            {
                if (_wordIndex is not null && _weights is not null)
                {
                    return;
                }

                var indexPath = Path.Combine(_engineDirectory, "glove_word_index.json");
                var shapePath = Path.Combine(_engineDirectory, "glove_shape.json");
                var vectorsPath = Path.Combine(_engineDirectory, "glove_vectors.bin");

                if (!File.Exists(indexPath) || !File.Exists(shapePath) || !File.Exists(vectorsPath))
                {
                    throw new FileNotFoundException("Не найдены файлы GloVe-модели. Проверь папку SemanticSearchEngine.");
                }

                var shape = JsonSerializer.Deserialize<VectorShape>(File.ReadAllText(shapePath))
                    ?? throw new InvalidDataException("Некорректный glove_shape.json.");
                var bytes = File.ReadAllBytes(vectorsPath);
                var weights = new float[bytes.Length / sizeof(float)];
                Buffer.BlockCopy(bytes, 0, weights, 0, bytes.Length);

                if (weights.Length != shape.Rows * shape.Cols)
                {
                    throw new InvalidDataException("Размер glove_vectors.bin не совпадает с glove_shape.json.");
                }

                _wordIndex = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(indexPath))
                    ?? throw new InvalidDataException("Некорректный glove_word_index.json.");
                _weights = weights;
                _cols = shape.Cols;
            }
        }

        private float[] BuildVector(IEnumerable<string> tokens, IReadOnlyDictionary<string, float> idf)
        {
            var vector = new float[_cols];
            var total = 0f;
            foreach (var token in tokens)
            {
                if (_wordIndex is null || _weights is null || !_wordIndex.TryGetValue(token, out var row))
                {
                    continue;
                }

                var weight = idf.TryGetValue(token, out var idfValue) ? idfValue : 1f;
                var offset = row * _cols;
                for (var i = 0; i < _cols; i++)
                {
                    vector[i] += _weights[offset + i] * weight;
                }
                total += weight;
            }

            if (total > 0)
            {
                for (var i = 0; i < vector.Length; i++)
                {
                    vector[i] /= total;
                }
            }

            Normalize(vector);
            return vector;
        }

        private static Dictionary<string, float> BuildIdf(IEnumerable<List<string>> docs)
        {
            var docList = docs.ToList();
            var df = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var doc in docList)
            {
                foreach (var token in doc.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    df[token] = df.TryGetValue(token, out var count) ? count + 1 : 1;
                }
            }

            var total = Math.Max(1, docList.Count);
            return df.ToDictionary(
                pair => pair.Key,
                pair => MathF.Log((1f + total) / (1f + pair.Value)) + 1f,
                StringComparer.OrdinalIgnoreCase);
        }

        private static List<string> Tokenize(string text)
        {
            return TokenRegex()
                .Matches((text ?? string.Empty).ToLowerInvariant().Replace("_x000d_", " "))
                .Select(match => match.Value)
                .Where(token => token.Length > 1 && !StopWords.Contains(token))
                .ToList();
        }

        private static (float Score, List<string> Reasons) ExactScore(
            string query,
            HashSet<string> queryTokens,
            VulnerabilitieFstec vulnerability,
            List<string> docTokens)
        {
            var score = 0f;
            var reasons = new List<string>();
            var itemText = BuildVulnerabilityDocument(vulnerability).ToLowerInvariant();

            foreach (Match match in BduRegex().Matches(query))
            {
                var normalized = match.Value.Replace("-", ":", StringComparison.Ordinal).Replace(" ", string.Empty);
                if (itemText.Replace("-", ":", StringComparison.Ordinal).Contains(normalized, StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.55f;
                    reasons.Add($"точное совпадение {match.Value.ToUpperInvariant()}");
                }
            }

            foreach (Match match in CveRegex().Matches(query))
            {
                if (itemText.Contains(match.Value, StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.45f;
                    reasons.Add($"совпадение {match.Value.ToUpperInvariant()}");
                }
            }

            var important = queryTokens
                .Intersect(docTokens, StringComparer.OrdinalIgnoreCase)
                .Where(token => token.Length >= 4)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();

            if (important.Count > 0)
            {
                score += Math.Min(0.45f, important.Count * 0.08f);
                reasons.Add("ключевые слова: " + string.Join(", ", important.Take(5)));
            }

            return (Math.Min(score, 0.75f), reasons);
        }

        private static string BuildVulnerabilityDocument(VulnerabilitieFstec v)
        {
            return string.Join(' ', v.Identifier, v.Name, v.Description, v.Vendor, v.NameSoftware, v.Version,
                v.Type, v.NameOperatingSystem, v.Class, v.DangerLevel, v.Measure, v.Exploit, v.OtherIdentifier,
                v.Incident, v.OperatingMethod, v.Cwe);
        }

        private static string BuildThreatDocument(Threat t)
        {
            return string.Join(' ', t.Id, t.Name, t.Description, t.Source, t.ObjectInfluence);
        }

        private static (bool NeedC, bool NeedI, bool NeedA, string Source) RequiredCia(VulnerabilitieFstec vulnerability)
        {
            var (c3, i3, a3) = ParseCvss(vulnerability.CvssThree);
            if (c3 || i3 || a3)
            {
                return (c3, i3, a3, vulnerability.CvssThree);
            }

            var (c2, i2, a2) = ParseCvss(vulnerability.CvssTwo);
            return (c2, i2, a2, vulnerability.CvssTwo);
        }

        private static (bool C, bool I, bool A) ParseCvss(string? cvss)
        {
            var c = false;
            var i = false;
            var a = false;
            foreach (Match match in CvssCiaRegex().Matches((cvss ?? string.Empty).ToUpperInvariant().Replace(" ", string.Empty)))
            {
                var metric = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                var enabled = value != "N";
                if (metric == "C")
                {
                    c = enabled;
                }
                else if (metric == "I")
                {
                    i = enabled;
                }
                else if (metric == "A")
                {
                    a = enabled;
                }
            }
            return (c, i, a);
        }

        private static bool IsEnabled(string? value)
        {
            var text = (value ?? string.Empty).ToLowerInvariant();
            return text.Contains('1') || text.Contains("да") || text.Contains("наруш");
        }

        private static string Confidence(float score)
        {
            if (score >= 0.70f)
            {
                return "высокая";
            }
            return score >= 0.45f ? "средняя" : "низкая";
        }

        private static string JoinReasons(string prefix, IEnumerable<string> reasons)
        {
            var tail = string.Join("; ", reasons.Where(reason => !string.IsNullOrWhiteSpace(reason)));
            return string.IsNullOrWhiteSpace(tail) ? prefix : $"{prefix} {tail}";
        }

        private static float Dot(IReadOnlyList<float> left, IReadOnlyList<float> right)
        {
            var result = 0f;
            for (var i = 0; i < left.Count; i++)
            {
                result += left[i] * right[i];
            }
            return result;
        }

        private static float Norm(IReadOnlyList<float> vector)
        {
            var result = 0f;
            for (var i = 0; i < vector.Count; i++)
            {
                result += vector[i] * vector[i];
            }
            return MathF.Sqrt(result);
        }

        private static void Normalize(float[] vector)
        {
            var norm = Norm(vector);
            if (norm <= 0)
            {
                return;
            }

            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }

        private static float Round(float value)
        {
            return MathF.Round(value, 3);
        }

        [GeneratedRegex("[A-Za-zА-Яа-яЁё0-9]+(?:[._:-][A-Za-zА-Яа-яЁё0-9]+)*")]
        private static partial Regex TokenRegex();

        [GeneratedRegex("\\bBDU[:\\-\\s]?\\d{4}[-:]\\d{4,}\\b", RegexOptions.IgnoreCase)]
        private static partial Regex BduRegex();

        [GeneratedRegex("\\bCVE-\\d{4}-\\d{4,}\\b", RegexOptions.IgnoreCase)]
        private static partial Regex CveRegex();

        [GeneratedRegex("(?:^|/)([CIA]):([HLNPC])(?=/|$)", RegexOptions.IgnoreCase)]
        private static partial Regex CvssCiaRegex();

        private sealed record VectorShape(
            [property: JsonPropertyName("rows")] int Rows,
            [property: JsonPropertyName("cols")] int Cols);

        private sealed record VulnerabilityCandidate(int Index, float Score, List<string> Reasons);
    }
}
