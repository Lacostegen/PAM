using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.MVVM.Model.Rag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PragmaticAnalyzer.Services.Rag
{
    public class RagSearchService
    {
        private readonly List<RagDocument> _documents = new();

        public IReadOnlyList<RagDocument> Documents => _documents;

        public void SetDocuments(IEnumerable<RagDocument> documents)
        {
            _documents.Clear();

            if (documents == null)
            {
                return;
            }

            _documents.AddRange(documents.Where(document =>
                document != null &&
                !string.IsNullOrWhiteSpace(document.SearchText)));
        }

        public List<RagSearchResult> Search(string query, RagConfig config)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<RagSearchResult>();
            }

            config ??= new RagConfig();

            var queryTokens = Tokenize(query);

            if (queryTokens.Count == 0)
            {
                return new List<RagSearchResult>();
            }

            var queryProfile = AnalyzeQuery(query);

            var candidates = _documents
                .Where(document => IsDocumentAllowed(document, config))
                .ToList();

            // Жёсткая маршрутизация: если вопрос явно относится к конкретной базе,
            // сначала ищем только в ней. Это не даёт Ontology перебивать ProtectionMeasures,
            // TechniquesTactics и Outcomes.
            var routedCandidates = candidates
                .Where(document => IsDocumentAllowedByRoute(document, queryProfile))
                .ToList();

            if (routedCandidates.Count > 0)
            {
                candidates = routedCandidates;
            }

            var results = new List<RagSearchResult>();

            foreach (var document in candidates)
            {
                var score = CalculateScore(document, query, queryTokens);
                score += CalculateProfileBoost(document, queryProfile);

                if (score >= config.MinScore)
                {
                    results.Add(new RagSearchResult(
                        document,
                        score,
                        GetMatchedText(document, queryTokens)));
                }
            }

            var orderedResults = results
                .OrderByDescending(result => result.Score)
                .ThenBy(result => GetSourcePriority(result.Document, queryProfile))
                .ThenBy(result => result.Document.Source)
                .ThenBy(result => result.Document.Title)
                .ToList();

            var exactProtectionMeasureResults = orderedResults
                .Where(result => HasExactProtectionMeasureMatch(result.Document, queryProfile))
                .ToList();

            if (exactProtectionMeasureResults.Count > 0)
            {
                return exactProtectionMeasureResults
                    .Take(config.TopK)
                    .ToList();
            }

            var exactTechniqueResults = orderedResults
                .Where(result => HasExactTechniqueMatch(result.Document, queryProfile))
                .ToList();

            if (exactTechniqueResults.Count > 0)
            {
                return exactTechniqueResults
                    .Take(config.TopK)
                    .ToList();
            }

            var exactIdResults = orderedResults
                .Where(result => HasExactIdMatch(result.Document, queryProfile))
                .ToList();

            if (exactIdResults.Count > 0)
            {
                return exactIdResults
                    .Take(config.TopK)
                    .ToList();
            }

            var exactTitleResults = orderedResults
                .Where(result => HasExactTitleMatch(result.Document, Normalize(query)))
                .ToList();

            if (exactTitleResults.Count > 0)
            {
                return exactTitleResults
                    .Take(config.TopK)
                    .ToList();
            }

            return orderedResults
                .Take(config.TopK)
                .ToList();
        }

        private static bool IsDocumentAllowed(RagDocument document, RagConfig config)
        {
            var sourceKind = Normalize(document.SourceKind);
            var source = Normalize(document.Source);
            var type = Normalize(document.Type);
            var product = Normalize(document.Product);

            if (sourceKind == "manual")
            {
                if (!config.UseManuals)
                {
                    return false;
                }

                if (product.Contains("kaspersky"))
                {
                    return config.UseKasperskyManuals;
                }

                if (product.Contains("secret") ||
                    product.Contains("secret net") ||
                    product.Contains("secret_net"))
                {
                    return config.UseSecretNetStudioManuals;
                }

                if (product.Contains("drweb") ||
                    product.Contains("dr.web") ||
                    product.Contains("doctor web"))
                {
                    return config.UseDrWebManuals;
                }

                return config.UseManuals;
            }

            if (sourceKind == "database" && !config.UseProjectDatabases)
            {
                return false;
            }

            var combined = $"{source} {type} {product}";

            if (combined.Contains("threat") || combined.Contains("угроз"))
            {
                return config.UseThreats;
            }

            if (combined.Contains("vulnerabil") ||
                combined.Contains("уязвим") ||
                combined.Contains("cve") ||
                combined.Contains("bdu") ||
                combined.Contains("jvn"))
            {
                return config.UseVulnerabilities;
            }

            if (combined.Contains("violator") || combined.Contains("наруш"))
            {
                return config.UseViolators;
            }

            if (combined.Contains("protection") ||
                combined.Contains("measure") ||
                combined.Contains("защит") ||
                combined.Contains("мер"))
            {
                return config.UseProtectionMeasures;
            }

            if (combined.Contains("tactic") ||
                combined.Contains("technique") ||
                combined.Contains("тактик") ||
                combined.Contains("техник"))
            {
                return config.UseTechniquesAndTactics;
            }

            if (combined.Contains("exploit") || combined.Contains("эксплойт"))
            {
                return config.UseExploits;
            }

            if (combined.Contains("outcome") ||
                combined.Contains("risk") ||
                combined.Contains("послед") ||
                combined.Contains("риск"))
            {
                return config.UseOutcomes;
            }

            return true;
        }

        private static bool IsDocumentAllowedByRoute(
            RagDocument document,
            QueryProfile profile)
        {
            // Самые явные маршруты. Если пользователь спрашивает про меры, тактики или методы риска,
            // Ontology не должна перебивать профильную базу.
            if (profile.RouteToProtectionMeasures)
            {
                return IsProtectionMeasureDocument(document);
            }

            if (profile.RouteToTechniquesTactics)
            {
                return IsTechniqueDocument(document);
            }

            if (profile.RouteToOutcomes)
            {
                return IsOutcomeDocument(document);
            }

            if (profile.RouteToOntology)
            {
                return IsOntologyDocument(document);
            }

            if (profile.RouteToFstecVulnerabilities)
            {
                return IsFstecVulnerabilityDocument(document);
            }

            if (profile.RouteToJvnVulnerabilities)
            {
                return IsJvnVulnerabilityDocument(document);
            }

            if (profile.RouteToNvdVulnerabilities)
            {
                return IsNvdVulnerabilityDocument(document) ||
                       IsJvnVulnerabilityDocument(document) ||
                       IsFstecVulnerabilityDocument(document) ||
                       IsExploitDocument(document);
            }

            if (profile.RouteToViolators)
            {
                return IsViolatorDocument(document);
            }

            if (profile.RouteToThreats)
            {
                return IsThreatDocument(document);
            }

            return true;
        }

        private static double CalculateScore(
            RagDocument document,
            string query,
            List<string> queryTokens)
        {
            var score = 0.0;

            var normalizedQuery = Normalize(query);

            var id = Normalize(document.Id);
            var source = Normalize(document.Source);
            var type = Normalize(document.Type);
            var product = Normalize(document.Product);
            var title = Normalize(document.Title);
            var section = Normalize(document.Section);
            var searchText = Normalize(document.SearchText);

            if (!string.IsNullOrWhiteSpace(id) && normalizedQuery.Contains(id))
            {
                score += 80;
            }

            if (!string.IsNullOrWhiteSpace(title) && normalizedQuery.Contains(title))
            {
                score += 35;
            }

            if (!string.IsNullOrWhiteSpace(title) && title.Contains(normalizedQuery))
            {
                score += 18;
            }

            if (!string.IsNullOrWhiteSpace(section) && section.Contains(normalizedQuery))
            {
                score += 10;
            }

            foreach (var token in queryTokens)
            {
                if (token.Length < 2)
                {
                    continue;
                }

                if (id.Contains(token))
                {
                    score += 14;
                }

                if (title.Contains(token))
                {
                    score += 7;
                }

                if (section.Contains(token))
                {
                    score += 5;
                }

                if (product.Contains(token))
                {
                    score += 4;
                }

                if (source.Contains(token))
                {
                    score += 2;
                }

                if (type.Contains(token))
                {
                    score += 2;
                }

                if (searchText.Contains(token))
                {
                    score += 1;
                }
            }

            if (ContainsImportantPhrase(searchText, normalizedQuery))
            {
                score += 20;
            }

            if (!string.IsNullOrWhiteSpace(document.PromptText))
            {
                score += 0.5;
            }

            return score;
        }

        private static double CalculateProfileBoost(
            RagDocument document,
            QueryProfile profile)
        {
            var score = 0.0;

            var id = Normalize(document.Id);
            var source = Normalize(document.Source);
            var type = Normalize(document.Type);
            var product = Normalize(document.Product);
            var title = Normalize(document.Title);
            var section = Normalize(document.Section);
            var searchText = Normalize(document.SearchText);

            var combined = $"{id} {source} {type} {product} {title} {section} {searchText}";

            foreach (var exactId in profile.ExactIds)
            {
                if (combined.Contains(exactId))
                {
                    score += 100;
                }
            }

            if (profile.IsProtectionMeasureQuery)
            {
                if (IsProtectionMeasureDocument(document))
                {
                    score += 35;
                }
                else if (IsOntologyDocument(document))
                {
                    score -= 20;
                }
                else if (IsSpecialistDocument(document))
                {
                    score -= 12;
                }
                else
                {
                    score -= 8;
                }
            }

            if (profile.IsTechniqueQuery)
            {
                if (IsTechniqueDocument(document))
                {
                    score += 35;
                }
                else if (IsOntologyDocument(document))
                {
                    score -= 20;
                }
                else if (IsViolatorDocument(document))
                {
                    score += 4;
                }
                else
                {
                    score -= 8;
                }
            }

            if (profile.IsOutcomeQuery)
            {
                if (IsOutcomeDocument(document))
                {
                    score += 35;
                }
                else if (IsOntologyDocument(document))
                {
                    score -= 20;
                }
                else
                {
                    score -= 8;
                }
            }

            if (profile.IsOntologyQuery)
            {
                if (IsOntologyDocument(document))
                {
                    score += 20;
                }
            }

            if (profile.IsVulnerabilityQuery)
            {
                if (IsVulnerabilityDocument(document))
                {
                    score += 12;
                }
                else
                {
                    score -= 4;
                }
            }

            if (profile.IsThreatQuery)
            {
                if (IsThreatDocument(document))
                {
                    score += 10;
                }
                else if (IsExploitDocument(document))
                {
                    score -= 4;
                }
            }

            if (profile.IsViolatorQuery)
            {
                if (IsViolatorDocument(document))
                {
                    score += 12;
                }
                else
                {
                    score -= 4;
                }
            }

            if (profile.IsManualQuery)
            {
                if (Normalize(document.SourceKind) == "manual")
                {
                    score += 8;
                }
            }

            if (profile.IsKasperskyQuery && product.Contains("kaspersky"))
            {
                score += 12;
            }

            if (profile.IsSecretNetStudioQuery &&
                (product.Contains("secret") || product.Contains("secret net")))
            {
                score += 12;
            }

            if (profile.IsDrWebQuery &&
                (product.Contains("dr.web") || product.Contains("drweb") || product.Contains("doctor web")))
            {
                score += 12;
            }

            if (!profile.IsExploitQuery && IsExploitDocument(document))
            {
                score -= 8;
            }

            if (profile.IsExploitQuery && IsExploitDocument(document))
            {
                score += 15;
            }

            return score;
        }

        private static int GetSourcePriority(
            RagDocument document,
            QueryProfile profile)
        {
            if (profile.IsProtectionMeasureQuery && IsProtectionMeasureDocument(document))
            {
                return 0;
            }

            if (profile.IsTechniqueQuery && IsTechniqueDocument(document))
            {
                return 0;
            }

            if (profile.IsOutcomeQuery && IsOutcomeDocument(document))
            {
                return 0;
            }

            if (profile.IsVulnerabilityQuery && IsVulnerabilityDocument(document))
            {
                return 0;
            }

            if (profile.IsViolatorQuery && IsViolatorDocument(document))
            {
                return 0;
            }

            if (profile.IsThreatQuery && IsThreatDocument(document))
            {
                return 0;
            }

            if (profile.IsManualQuery && Normalize(document.SourceKind) == "manual")
            {
                return 0;
            }

            if (profile.IsOntologyQuery && IsOntologyDocument(document))
            {
                return 0;
            }

            return 10;
        }

        private static QueryProfile AnalyzeQuery(string query)
        {
            var normalized = Normalize(query);
            var exactIds = ExtractExactIds(normalized);

            var isProtectionMeasureQuery =
                exactIds.Any(IsProtectionMeasureCode) ||
                normalized.Contains("мера защиты") ||
                normalized.Contains("меры защиты") ||
                normalized.Contains("мер защиты") ||
                normalized.Contains("управление доступом") ||
                normalized.Contains("управлению доступом") ||
                normalized.Contains("управления доступом") ||
                normalized.Contains("идентификация и аутентификация") ||
                normalized.Contains("иаф") ||
                normalized.Contains("упд") ||
                normalized.Contains("ауд");

            var isTechniqueQuery =
                exactIds.Any(IsTechniqueCode) ||
                normalized.Contains("mitre") ||
                normalized.Contains("тактик") ||
                normalized.Contains("техник") ||
                normalized.Contains("tactic") ||
                normalized.Contains("technique") ||
                normalized.Contains("сбор информации") ||
                normalized.Contains("сбором информации") ||
                normalized.Contains("связаны со сбором") ||
                normalized.Contains("разведк") ||
                normalized.Contains("сканирован") ||
                normalized.Contains("фишинг");

            var isOutcomeQuery =
                normalized.Contains("метод оценки риска") ||
                normalized.Contains("методы оценки риска") ||
                normalized.Contains("методов оценки риска") ||
                normalized.Contains("оценка риска") ||
                normalized.Contains("оценки риска") ||
                normalized.Contains("оценке риска") ||
                normalized.Contains("анализ риска") ||
                normalized.Contains("методы риска") ||
                normalized.Contains("alarp") ||
                normalized.Contains("sfairp") ||
                normalized.Contains("bayes") ||
                normalized.Contains("байес") ||
                normalized.Contains("bia") ||
                normalized.Contains("галстук-бабочка") ||
                normalized.Contains("мозговой штурм");

            var isVulnerabilityQuery =
                exactIds.Any(id => id.StartsWith("bdu:") || id.StartsWith("cve-") || id.StartsWith("jvndb-")) ||
                normalized.Contains("уязвим") ||
                normalized.Contains("bdu") ||
                normalized.Contains("cve") ||
                normalized.Contains("jvndb") ||
                normalized.Contains("cvss") ||
                normalized.Contains("cwe") ||
                normalized.Contains("патч") ||
                normalized.Contains("обновлен");

            var isThreatQuery =
                normalized.Contains("угроз") ||
                normalized.Contains("уби") ||
                normalized.Contains("биос") ||
                normalized.Contains("bios") ||
                normalized.Contains("uefi") ||
                normalized.Contains("доступност") ||
                normalized.Contains("целостност") ||
                normalized.Contains("конфиденциальност");

            var isViolatorQuery =
                normalized.Contains("нарушител") ||
                normalized.Contains("apt") ||
                normalized.Contains("группировк") ||
                normalized.Contains("space pirates") ||
                normalized.Contains("chamelgang") ||
                normalized.Contains("инсайдер") ||
                normalized.Contains("злоумышленник");

            var isOntologyQuery =
                !isProtectionMeasureQuery &&
                !isTechniqueQuery &&
                !isOutcomeQuery &&
                !isVulnerabilityQuery &&
                !isThreatQuery &&
                !isViolatorQuery &&
                (normalized.Contains("что означает") ||
                 normalized.Contains("что значит") ||
                 normalized.Contains("термин") ||
                 normalized.Contains("определение") ||
                 normalized.Contains("понятие"));

            return new QueryProfile
            {
                ExactIds = exactIds,

                IsProtectionMeasureQuery = isProtectionMeasureQuery,
                IsTechniqueQuery = isTechniqueQuery,
                IsOutcomeQuery = isOutcomeQuery,
                IsOntologyQuery = isOntologyQuery,
                IsVulnerabilityQuery = isVulnerabilityQuery,
                IsThreatQuery = isThreatQuery,
                IsViolatorQuery = isViolatorQuery,

                RouteToProtectionMeasures = isProtectionMeasureQuery,
                RouteToTechniquesTactics = isTechniqueQuery,
                RouteToOutcomes = isOutcomeQuery,
                RouteToOntology = isOntologyQuery,
                RouteToFstecVulnerabilities = exactIds.Any(id => id.StartsWith("bdu:")),
                RouteToJvnVulnerabilities = exactIds.Any(id => id.StartsWith("jvndb-")),
                RouteToNvdVulnerabilities = exactIds.Any(id => id.StartsWith("cve-")),
                RouteToViolators = isViolatorQuery,
                RouteToThreats = isThreatQuery && !isVulnerabilityQuery && !isTechniqueQuery,

                IsManualQuery =
                    normalized.Contains("руководств") ||
                    normalized.Contains("инструкц") ||
                    normalized.Contains("настро") ||
                    normalized.Contains("как включить") ||
                    normalized.Contains("как отключить") ||
                    normalized.Contains("kaspersky") ||
                    normalized.Contains("dr.web") ||
                    normalized.Contains("drweb") ||
                    normalized.Contains("secret net"),

                IsKasperskyQuery =
                    normalized.Contains("kaspersky") ||
                    normalized.Contains("касперск"),

                IsSecretNetStudioQuery =
                    normalized.Contains("secret net") ||
                    normalized.Contains("secretnet") ||
                    normalized.Contains("секрет нет"),

                IsDrWebQuery =
                    normalized.Contains("dr.web") ||
                    normalized.Contains("drweb") ||
                    normalized.Contains("doctor web") ||
                    normalized.Contains("доктор веб"),

                IsExploitQuery =
                    normalized.Contains("exploit") ||
                    normalized.Contains("эксплойт") ||
                    normalized.Contains("exploitdb") ||
                    normalized.Contains("exploit database")
            };
        }

        private static List<string> ExtractExactIds(string normalizedQuery)
        {
            var ids = new List<string>();

            foreach (Match match in Regex.Matches(
                         normalizedQuery,
                         @"bdu:\d{4}-\d{5}",
                         RegexOptions.IgnoreCase))
            {
                ids.Add(match.Value);
            }

            foreach (Match match in Regex.Matches(
                         normalizedQuery,
                         @"cve-\d{4}-\d{4,7}",
                         RegexOptions.IgnoreCase))
            {
                ids.Add(match.Value);
            }

            foreach (Match match in Regex.Matches(
                         normalizedQuery,
                         @"jvndb-\d{4}-\d{6}",
                         RegexOptions.IgnoreCase))
            {
                ids.Add(match.Value);
            }

            foreach (Match match in Regex.Matches(
                         normalizedQuery,
                         @"\bt\d{4}(\.\d{3})?\b",
                         RegexOptions.IgnoreCase))
            {
                ids.Add(match.Value);
            }

            foreach (Match match in Regex.Matches(
                         normalizedQuery,
                         @"(?<![a-zа-я0-9])([тt]\d+(\.\d+)?)(?![a-zа-я0-9])",
                         RegexOptions.IgnoreCase))
            {
                ids.Add(match.Value);
            }

            foreach (Match match in Regex.Matches(
                         normalizedQuery,
                         @"(?<![a-zа-я0-9])([a-zа-я]{2,8}\.\d+)(?![a-zа-я0-9])",
                         RegexOptions.IgnoreCase))
            {
                ids.Add(match.Value);
            }

            return ids
                .Select(Normalize)
                .Distinct()
                .ToList();
        }

        private static bool HasExactProtectionMeasureMatch(
            RagDocument document,
            QueryProfile profile)
        {
            if (!profile.IsProtectionMeasureQuery ||
                profile.ExactIds.Count == 0 ||
                !IsProtectionMeasureDocument(document))
            {
                return false;
            }

            var id = Normalize(document.Id);
            var title = Normalize(document.Title);
            var searchText = Normalize(document.SearchText);

            foreach (var exactId in profile.ExactIds)
            {
                if (!IsProtectionMeasureCode(exactId))
                {
                    continue;
                }

                if (id == exactId)
                {
                    return true;
                }

                if (id.EndsWith("_" + exactId.Replace(".", "_")))
                {
                    return true;
                }

                if (searchText.Contains(exactId) || title.Contains(exactId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasExactTechniqueMatch(
            RagDocument document,
            QueryProfile profile)
        {
            if (!profile.IsTechniqueQuery ||
                profile.ExactIds.Count == 0 ||
                !IsTechniqueDocument(document))
            {
                return false;
            }

            var id = Normalize(document.Id);
            var title = Normalize(document.Title);
            var searchText = Normalize(document.SearchText);

            foreach (var exactId in profile.ExactIds)
            {
                if (!IsTechniqueCode(exactId))
                {
                    continue;
                }

                if (id == exactId || title.Contains(exactId) || searchText.Contains(exactId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasExactIdMatch(
            RagDocument document,
            QueryProfile profile)
        {
            if (profile.ExactIds.Count == 0)
            {
                return false;
            }

            var id = Normalize(document.Id);
            var searchText = Normalize(document.SearchText);

            return profile.ExactIds.Any(exactId =>
                id == exactId ||
                searchText.Contains(exactId));
        }

        private static bool HasExactTitleMatch(
            RagDocument document,
            string normalizedQuery)
        {
            var title = Normalize(document.Title);

            if (string.IsNullOrWhiteSpace(title) || title.Length < 4)
            {
                return false;
            }

            return normalizedQuery.Contains(title);
        }

        private static bool IsProtectionMeasureCode(string value)
        {
            return Regex.IsMatch(
                Normalize(value),
                @"^[а-я]{2,8}\.\d+$",
                RegexOptions.IgnoreCase);
        }

        private static bool IsTechniqueCode(string value)
        {
            return Regex.IsMatch(
                Normalize(value),
                @"^[тt]\d+(\.\d+)?$",
                RegexOptions.IgnoreCase);
        }

        private static bool IsVulnerabilityDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("vulnerabil") ||
                   combined.Contains("уязвим") ||
                   combined.Contains("bdu") ||
                   combined.Contains("cve") ||
                   combined.Contains("jvn");
        }

        private static bool IsFstecVulnerabilityDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("fstec") || combined.Contains("bdu");
        }

        private static bool IsNvdVulnerabilityDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("nvd");
        }

        private static bool IsJvnVulnerabilityDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("jvn");
        }

        private static bool IsThreatDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("threat") ||
                   combined.Contains("угроз");
        }

        private static bool IsViolatorDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("violator") ||
                   combined.Contains("наруш");
        }

        private static bool IsTechniqueDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("tactic") ||
                   combined.Contains("technique") ||
                   combined.Contains("тактик") ||
                   combined.Contains("техник");
        }

        private static bool IsProtectionMeasureDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("protection") ||
                   combined.Contains("measure") ||
                   combined.Contains("мера") ||
                   combined.Contains("защит");
        }

        private static bool IsOutcomeDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("outcome") ||
                   combined.Contains("risk") ||
                   combined.Contains("послед") ||
                   combined.Contains("риск");
        }

        private static bool IsOntologyDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("ontology") ||
                   combined.Contains("онтолог") ||
                   combined.Contains("term") ||
                   combined.Contains("entity");
        }

        private static bool IsSpecialistDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("specialist") ||
                   combined.Contains("специалист");
        }

        private static bool IsExploitDocument(RagDocument document)
        {
            var combined = Normalize($"{document.Source} {document.Type}");

            return combined.Contains("exploit") ||
                   combined.Contains("эксплойт");
        }

        private static bool ContainsImportantPhrase(string searchText, string normalizedQuery)
        {
            if (string.IsNullOrWhiteSpace(searchText) || string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return false;
            }

            var importantPhrases = new[]
            {
                "сбор информации",
                "управление доступом",
                "идентификация и аутентификация",
                "оценка риска",
                "метод оценки",
                "вредоносная программа",
                "bios",
                "uefi"
            };

            return importantPhrases.Any(phrase =>
                normalizedQuery.Contains(phrase) && searchText.Contains(phrase));
        }

        private static string GetMatchedText(RagDocument document, List<string> queryTokens)
        {
            var text = document.SearchText ?? string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalizedText = Normalize(text);

            foreach (var token in queryTokens)
            {
                var index = normalizedText.IndexOf(token, StringComparison.OrdinalIgnoreCase);

                if (index >= 0)
                {
                    var start = Math.Max(0, index - 100);
                    var length = Math.Min(text.Length - start, 300);

                    return text.Substring(start, length).Trim();
                }
            }

            return string.Empty;
        }

        private static List<string> Tokenize(string text)
        {
            return Regex
                .Split(Normalize(text), @"[^a-zа-яё0-9\-_:\.]+")
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Where(token => token.Length > 1)
                .Distinct()
                .ToList();
        }

        private static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text
                .ToLowerInvariant()
                .Replace('ё', 'е')
                .Trim();
        }

        private class QueryProfile
        {
            public List<string> ExactIds { get; set; } = new();

            public bool IsVulnerabilityQuery { get; set; }

            public bool IsThreatQuery { get; set; }

            public bool IsViolatorQuery { get; set; }

            public bool IsTechniqueQuery { get; set; }

            public bool IsProtectionMeasureQuery { get; set; }

            public bool IsOutcomeQuery { get; set; }

            public bool IsOntologyQuery { get; set; }

            public bool IsManualQuery { get; set; }

            public bool IsKasperskyQuery { get; set; }

            public bool IsSecretNetStudioQuery { get; set; }

            public bool IsDrWebQuery { get; set; }

            public bool IsExploitQuery { get; set; }

            public bool RouteToProtectionMeasures { get; set; }

            public bool RouteToTechniquesTactics { get; set; }

            public bool RouteToOutcomes { get; set; }

            public bool RouteToOntology { get; set; }

            public bool RouteToFstecVulnerabilities { get; set; }

            public bool RouteToNvdVulnerabilities { get; set; }

            public bool RouteToJvnVulnerabilities { get; set; }

            public bool RouteToViolators { get; set; }

            public bool RouteToThreats { get; set; }
        }
    }
}
