using PragmaticAnalyzer.MVVM.Model.Rag;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PragmaticAnalyzer.Services.Rag
{
    public class ProjectDatabaseRagLoader
    {
        public List<RagDocument> LoadProjectDatabaseDocuments(string databasePath)
        {
            var documents = new List<RagDocument>();

            var resolvedDatabasePath = ResolveDatabasePath(databasePath);
            //WriteDebugLog($"Resolved database path: {resolvedDatabasePath}");

            if (string.IsNullOrWhiteSpace(resolvedDatabasePath) ||
                !Directory.Exists(resolvedDatabasePath))
            {
                return documents;
            }

            var jsonFiles = Directory
                .EnumerateFiles(resolvedDatabasePath, "*.json", SearchOption.AllDirectories)
                .ToList();

            //WriteDebugLog($"JSON files found: {jsonFiles.Count}");

            foreach (var jsonFile in jsonFiles)
            {
                //WriteDebugLog($"JSON file: {jsonFile}");
            }

            foreach (var filePath in jsonFiles)
            {
                var loadedFromFile = LoadJsonFile(filePath);
                //WriteDebugLog($"Loaded documents from {Path.GetFileName(filePath)}: {loadedFromFile.Count}");
                documents.AddRange(loadedFromFile);
            }

            return documents;
        }

        private static List<RagDocument> LoadJsonFile(string filePath)
        {
            var documents = new List<RagDocument>();

            try
            {
                var json = ReadTextFile(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return documents;
                }

                using var jsonDocument = JsonDocument.Parse(json);

                var fileName = Path.GetFileNameWithoutExtension(filePath)
                    .ToLowerInvariant();

                // Сложные базы лучше раскрывать не "одна запись = один документ",
                // а по внутренним элементам: тактики -> техники, онтология -> термины,
                // outcomes -> технологии и последствия.
                if (fileName.Contains("tactic") || fileName.Contains("technique"))
                {
                    return LoadTacticDocuments(jsonDocument.RootElement);
                }

                if (fileName.Contains("outcome"))
                {
                    return LoadOutcomeDocuments(jsonDocument.RootElement);
                }

                if (fileName.Contains("ontology"))
                {
                    return LoadOntologyDocuments(jsonDocument.RootElement);
                }

                var records = ExtractRecords(jsonDocument.RootElement).ToList();

                if (records.Count == 0)
                {
                    return documents;
                }

                for (var i = 0; i < records.Count; i++)
                {
                    RagDocument? document;

                    if (fileName.Contains("threat"))
                    {
                        document = CreateThreatDocument(records[i], i);
                    }
                    else if (fileName.Contains("fstec"))
                    {
                        document = CreateFstecVulnerabilityDocument(records[i], i);
                    }
                    else if (fileName.Contains("nvd"))
                    {
                        document = CreateNvdVulnerabilityDocument(records[i], i);
                    }
                    else if (fileName.Contains("jvn"))
                    {
                        document = CreateJvnVulnerabilityDocument(records[i], i);
                    }
                    else if (fileName.Contains("violator"))
                    {
                        document = CreateViolatorDocument(records[i], i);
                    }
                    else if (fileName.Contains("protection"))
                    {
                        document = CreateProtectionMeasureDocument(records[i], i);
                    }
                    else if (fileName.Contains("exploit"))
                    {
                        document = CreateExploitDocument(records[i], i);
                    }
                    else if (fileName.Contains("specialist"))
                    {
                        document = CreateSpecialistDocument(records[i], i);
                    }
                    else
                    {
                        document = CreateFallbackDatabaseDocument(records[i], filePath, i);
                    }

                    if (document != null &&
                        !string.IsNullOrWhiteSpace(document.SearchText))
                    {
                        documents.Add(document);
                    }
                }
            }
            catch
            {
                // Один повреждённый или неожиданный JSON не должен ломать загрузку всего RAG.
            }

            return documents;
        }

        private static IEnumerable<JsonElement> ExtractRecords(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Object &&
                TryGetProperty(root, "Value", out var valueElement))
            {
                if (valueElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in valueElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            yield return item;
                        }
                    }

                    yield break;
                }

                if (valueElement.ValueKind == JsonValueKind.Object)
                {
                    var hasArrayProperty = false;

                    foreach (var property in valueElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        hasArrayProperty = true;

                        foreach (var item in property.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                yield return item;
                            }
                        }
                    }

                    if (hasArrayProperty)
                    {
                        yield break;
                    }

                    yield return valueElement;
                    yield break;
                }
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        yield return item;
                    }
                }

                yield break;
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                yield return root;
            }
        }

        // =========================
        // Threat
        // =========================

        private static RagDocument CreateThreatDocument(JsonElement record, int index)
        {
            var id = GetString(record, "Id");

            if (string.IsNullOrWhiteSpace(id))
            {
                id = $"threat_{index}";
            }

            var name = GetString(record, "Name");
            var description = GetString(record, "Description");
            var source = GetString(record, "Source");
            var objectInfluence = GetString(record, "ObjectInfluence");
            var privacyViolation = GetString(record, "PrivacyViolation");
            var integrityViolation = GetString(record, "IntegrityViolation");
            var accessibilityViolation = GetString(record, "AccessibilityViolation");
            var dateInclusion = GetString(record, "DateInclusion");
            var dateChange = GetString(record, "DateChange");

            var title = string.IsNullOrWhiteSpace(name)
                ? $"Угроза {id}"
                : name;

            var ragId = $"threat_{id}";

            var searchText = BuildText(
                "Threat",
                "threat",
                "угроза",
                $"ID {id}",
                $"УБИ {id}",
                title,
                description,
                source,
                objectInfluence,
                $"конфиденциальность {privacyViolation}",
                $"целостность {integrityViolation}",
                $"доступность {accessibilityViolation}",
                dateInclusion,
                dateChange);

            var promptText = BuildText(
                "Источник: База угроз",
                "Тип: threat",
                $"ID: {id}",
                $"Название: {title}",
                $"Описание: {description}",
                $"Источник угрозы / нарушитель: {source}",
                $"Объект воздействия: {objectInfluence}",
                $"Нарушение конфиденциальности: {ToYesNo(privacyViolation)}",
                $"Нарушение целостности: {ToYesNo(integrityViolation)}",
                $"Нарушение доступности: {ToYesNo(accessibilityViolation)}",
                $"Дата включения: {dateInclusion}",
                $"Дата изменения: {dateChange}");

            return new RagDocument
            {
                Id = ragId,
                SourceKind = "database",
                Source = "Threat",
                Type = "threat",
                Product = string.Empty,
                Title = title,
                Section = objectInfluence,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 9000),
                PromptText = LimitText(promptText, 3500),
                Metadata = BuildMetadata(record)
            };
        }

        // =========================
        // VulnerabilitieFstec
        // =========================

        private static RagDocument CreateFstecVulnerabilityDocument(JsonElement record, int index)
        {
            var identifier = GetString(record, "Identifier");

            if (string.IsNullOrWhiteSpace(identifier))
            {
                identifier = $"BDU_UNKNOWN_{index}";
            }

            var name = GetString(record, "Name");
            var description = GetString(record, "Description");
            var vendor = GetString(record, "Vendor");
            var software = GetString(record, "NameSoftware");
            var version = GetString(record, "Version");
            var type = GetString(record, "Type");
            var os = GetString(record, "NameOperatingSystem");
            var vulnerabilityClass = GetString(record, "Class");
            var date = GetString(record, "Date");
            var cvssTwo = GetString(record, "CvssTwo");
            var cvssThree = GetString(record, "CvssThree");
            var dangerLevel = GetString(record, "DangerLevel");
            var measure = GetString(record, "Measure");
            var exploit = GetString(record, "Exploit");
            var information = GetString(record, "Information");
            var links = GetString(record, "Links");
            var otherIdentifier = GetString(record, "OtherIdentifier");
            var otherInformation = GetString(record, "OtherInformation");
            var incident = GetString(record, "Incident");
            var operatingMethod = GetString(record, "OperatingMethod");
            var eliminationMethod = GetString(record, "EliminationMethod");
            var descriptionCwe = GetString(record, "DescriptionCwe");
            var cwe = GetString(record, "Cwe");

            var title = string.IsNullOrWhiteSpace(name)
                ? identifier
                : name;

            var searchText = BuildText(
                "VulnerabilitieFstec",
                "vulnerability_fstec",
                "уязвимость",
                "БДУ",
                identifier,
                title,
                description,
                vendor,
                software,
                version,
                type,
                os,
                vulnerabilityClass,
                date,
                cvssTwo,
                cvssThree,
                dangerLevel,
                measure,
                exploit,
                information,
                links,
                otherIdentifier,
                otherInformation,
                incident,
                operatingMethod,
                eliminationMethod,
                descriptionCwe,
                cwe);

            var promptText = BuildText(
                "Источник: БДУ ФСТЭК",
                "Тип: vulnerability_fstec",
                $"ID: {identifier}",
                $"Название: {title}",
                $"Описание: {LimitText(description, 700)}",

                // Важные поля ставим выше длинных Version/OS,
                // чтобы они не обрезались в RAG-контексте.
                $"CVSS 2.0: {cvssTwo}",
                $"CVSS 3.0: {cvssThree}",
                $"Уровень опасности: {dangerLevel}",
                $"Оценка опасности / мера: {measure}",
                $"Рекомендованное устранение из базы: {exploit}",
                $"Статус устранения: {otherIdentifier}",
                $"Связанные идентификаторы CVE/OSVDB/Bugtraq/ExploitDB: {incident}",
                $"CWE описание: {descriptionCwe}",
                $"CWE / рекомендация: {cwe}",

                $"Вендор: {vendor}",
                $"ПО: {software}",
                $"Версия: {LimitText(version, 500)}",
                $"Тип ПО: {type}",
                $"ОС / платформа: {LimitText(os, 500)}",
                $"Класс уязвимости: {vulnerabilityClass}",
                $"Дата: {date}",
                $"Статус подтверждения: {information}",
                $"Наличие ссылок: {links}",
                $"Дополнительная информация / ссылка: {otherInformation}",
                $"Метод эксплуатации / реализации: {operatingMethod}",
                $"Метод устранения: {eliminationMethod}");

            return new RagDocument
            {
                Id = identifier,
                SourceKind = "database",
                Source = "VulnerabilitieFstec",
                Type = "vulnerability_fstec",
                Product = software,
                Title = title,
                Section = vulnerabilityClass,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 12000),
                PromptText = LimitText(promptText, 4500),
                Metadata = BuildMetadata(record)
            };
        }

        // =========================
        // VulnerabilitieNvd
        // =========================

        private static RagDocument CreateNvdVulnerabilityDocument(JsonElement record, int index)
        {
            var identifier = GetString(record, "Identifier");

            if (string.IsNullOrWhiteSpace(identifier))
            {
                identifier = $"CVE_UNKNOWN_{index}";
            }

            var description = GetString(record, "Description");
            var published = GetString(record, "Published");
            var lastModified = GetString(record, "LastModified");
            var status = GetString(record, "VulnStatus");
            var vectorString = GetString(record, "VectorString");

            var title = identifier;

            var searchText = BuildText(
                "VulnerabilitieNvd",
                "vulnerability_nvd",
                "уязвимость",
                "NVD",
                "CVE",
                identifier,
                description,
                published,
                lastModified,
                status,
                vectorString);

            var promptText = BuildText(
                "Источник: NVD",
                "Тип: vulnerability_nvd",
                $"ID: {identifier}",
                $"Описание: {LimitText(description, 1300)}",
                $"Дата публикации: {published}",
                $"Дата последнего изменения: {lastModified}",
                $"Статус уязвимости: {status}",
                $"CVSS/vector string: {vectorString}");

            return new RagDocument
            {
                Id = identifier,
                SourceKind = "database",
                Source = "VulnerabilitieNvd",
                Type = "vulnerability_nvd",
                Product = string.Empty,
                Title = title,
                Section = status,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 9000),
                PromptText = LimitText(promptText, 3000),
                Metadata = BuildMetadata(record)
            };
        }

        // =========================
        // VulnerabilitieJvn
        // =========================

        private static RagDocument CreateJvnVulnerabilityDocument(JsonElement record, int index)
        {
            var identifier = GetString(record, "Identifier");

            if (string.IsNullOrWhiteSpace(identifier))
            {
                identifier = $"JVN_UNKNOWN_{index}";
            }

            var name = GetString(record, "Name");
            var description = GetString(record, "Description");
            var link = GetString(record, "Link");
            var dateChange = GetString(record, "DateChange");
            var dateAdded = GetString(record, "DateAdded");
            var references = GetString(record, "References");
            var cvss = GetString(record, "Cvss");
            var vendor = GetString(record, "Vendor");
            var software = GetString(record, "NameSoftware");
            var version = GetString(record, "VersionSoftware");
            var cve = ExtractCveFromText(references);

            var title = string.IsNullOrWhiteSpace(name)
                ? identifier
                : name;

            var searchText = BuildText(
                "VulnerabilitieJvn",
                "vulnerability_jvn",
                "уязвимость",
                "JVN",
                "JVNDB",
                identifier,
                cve,
                title,
                description,
                link,
                dateChange,
                dateAdded,
                references,
                cvss,
                vendor,
                software,
                version);

            var promptText = BuildText(
                "Источник: JVN/JVNDB",
                "Тип: vulnerability_jvn",
                $"ID: {identifier}",
                $"Название: {title}",
                $"Описание: {LimitText(description, 1200)}",
                $"CVSS: {cvss}",
                Field("Связанные CVE", cve),
                $"Вендор: {LimitText(vendor, 700)}",
                $"ПО: {LimitText(software, 700)}",
                $"Версия ПО: {LimitText(version, 700)}",
                $"Дата добавления: {dateAdded}",
                $"Дата изменения: {dateChange}",
                $"Ссылка: {link}",
                $"Ссылки/References: {LimitText(references, 1000)}");

            return new RagDocument
            {
                Id = identifier,
                SourceKind = "database",
                Source = "VulnerabilitieJvn",
                Type = "vulnerability_jvn",
                Product = software,
                Title = title,
                Section = cvss,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 12000),
                PromptText = LimitText(promptText, 4500),
                Metadata = BuildMetadata(record)
            };
        }

        // =========================
        // Violator
        // =========================

        private static RagDocument CreateViolatorDocument(JsonElement record, int index)
        {
            var groupName = GetString(record, "GroupName");

            if (string.IsNullOrWhiteSpace(groupName))
            {
                groupName = $"violator_{index}";
            }

            var description = GetString(record, "Description");
            var stateAffiliation = GetString(record, "StateAffiliation");
            var alternateNames = GetString(record, "AlternateNames");
            var attackTargets = GetString(record, "AttackTargets");
            var knownAttacks = GetString(record, "KnownAttacks");
            var usedTools = GetString(record, "UsedTools");
            var attackObjectives = GetString(record, "AttackObjectives");
            var tactics = GetStringArray(record, "TacticsUsed");

            var shortTacticsForPrompt = tactics
                .Take(12)
                .ToList();

            var searchText = BuildText(
                "Violator",
                "violator",
                "нарушитель",
                "APT",
                groupName,
                description,
                stateAffiliation,
                alternateNames,
                attackTargets,
                knownAttacks,
                usedTools,
                attackObjectives,
                string.Join(Environment.NewLine, tactics));

            var promptText = BuildText(
                "Источник: Модель нарушителей",
                "Тип: violator",
                $"Группа: {groupName}",
                $"Описание: {description}",
                $"Государственная принадлежность: {stateAffiliation}",
                $"Альтернативные названия: {alternateNames}",
                $"Цели/объекты атак: {attackTargets}",
                $"Известные атаки: {knownAttacks}",
                $"Используемые инструменты: {usedTools}",
                $"Цели атак: {attackObjectives}",
                "Используемые тактики и техники MITRE:",
                string.Join(Environment.NewLine, shortTacticsForPrompt));

            return new RagDocument
            {
                Id = $"violator_{NormalizeIdPart(groupName)}",
                SourceKind = "database",
                Source = "Violator",
                Type = "violator",
                Product = string.Empty,
                Title = groupName,
                Section = attackObjectives,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 16000),
                PromptText = LimitText(promptText, 5500),
                Metadata = BuildMetadata(record)
            };
        }

        // =========================
        // ProtectionMeasures
        // =========================

        private static RagDocument CreateProtectionMeasureDocument(JsonElement record, int index)
        {
            var nameGroup = GetString(record, "NameGroup");
            var name = GetString(record, "Name");
            var number = GetString(record, "Number");
            var fullName = GetString(record, "FullName");
            var securityClasses = GetString(record, "SecurityClasses");

            var id = BuildProtectionMeasureId(name, number, index);
            var title = string.IsNullOrWhiteSpace(fullName)
                ? id
                : fullName;

            var searchText = BuildText(
                "ProtectionMeasures",
                "protection_measure",
                "мера защиты",
                "защита",
                "контроль безопасности",
                id,
                nameGroup,
                name,
                number,
                fullName,
                securityClasses);

            var promptText = BuildText(
                "Источник: Меры защиты",
                "Тип: protection_measure",
                $"ID: {id}",
                $"Группа мер: {nameGroup}",
                $"Код группы: {name}",
                $"Номер меры: {number}",
                $"Название меры: {title}",
                $"Классы защищенности: {securityClasses}");

            return new RagDocument
            {
                Id = id,
                SourceKind = "database",
                Source = "ProtectionMeasures",
                Type = "protection_measure",
                Product = string.Empty,
                Title = title,
                Section = nameGroup,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 7000),
                PromptText = LimitText(promptText, 2500),
                Metadata = BuildMetadata(record)
            };
        }

        // =========================
        // TechniquesTactics
        // =========================

        private static List<RagDocument> LoadTacticDocuments(JsonElement root)
        {
            var documents = new List<RagDocument>();
            var tactics = ExtractRecords(root).ToList();
            var index = 0;

            foreach (var tacticRecord in tactics)
            {
                var tacticName = GetString(tacticRecord, "Name");
                var tacticDescription = GetString(tacticRecord, "Description");

                if (string.IsNullOrWhiteSpace(tacticName))
                {
                    tacticName = $"Т{index + 1}";
                }

                var tacticDocument = CreateTacticDocument(
                    tacticRecord,
                    tacticName,
                    tacticDescription,
                    index++);

                documents.Add(tacticDocument);

                if (!TryGetProperty(tacticRecord, "Techniques", out var techniquesElement) ||
                    techniquesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var techniqueRecord in techniquesElement.EnumerateArray())
                {
                    if (techniqueRecord.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    documents.Add(CreateTechniqueDocument(
                        techniqueRecord,
                        tacticName,
                        tacticDescription,
                        index++));
                }
            }

            return documents;
        }

        private static RagDocument CreateTacticDocument(
            JsonElement record,
            string tacticName,
            string tacticDescription,
            int index)
        {
            var techniques = new List<string>();

            if (TryGetProperty(record, "Techniques", out var techniquesElement) &&
                techniquesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var technique in techniquesElement.EnumerateArray().Take(20))
                {
                    if (technique.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    techniques.Add(BuildText(
                        GetString(technique, "Name"),
                        LimitText(GetString(technique, "Description"), 250)));
                }
            }

            var searchText = BuildText(
                "TechniquesTactics",
                "tactic",
                "тактика",
                "тактическая задача",
                tacticName,
                tacticDescription,
                string.Join(Environment.NewLine, techniques));

            var promptText = BuildText(
                "Источник: Тактики и техники",
                "Тип: tactic",
                $"ID: {tacticName}",
                $"Тактика: {tacticName}",
                $"Описание: {tacticDescription}",
                "Связанные техники:",
                string.Join(Environment.NewLine, techniques.Take(10)));

            return new RagDocument
            {
                Id = $"tactic_{NormalizeIdPart(tacticName)}",
                SourceKind = "database",
                Source = "TechniquesTactics",
                Type = "tactic",
                Product = string.Empty,
                Title = tacticName,
                Section = "Тактика",
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 12000),
                PromptText = LimitText(promptText, 4000),
                Metadata = BuildMetadata(record)
            };
        }

        private static RagDocument CreateTechniqueDocument(
            JsonElement record,
            string tacticName,
            string tacticDescription,
            int index)
        {
            var techniqueName = GetString(record, "Name");

            if (string.IsNullOrWhiteSpace(techniqueName))
            {
                techniqueName = $"technique_{index}";
            }

            var techniqueDescription = GetString(record, "Description");

            var searchText = BuildText(
                "TechniquesTactics",
                "technique",
                "техника",
                "тактика",
                tacticName,
                tacticDescription,
                techniqueName,
                techniqueDescription);

            var promptText = BuildText(
                "Источник: Тактики и техники",
                "Тип: technique",
                $"ID: {techniqueName}",
                $"Тактика: {tacticName}",
                $"Описание тактики: {LimitText(tacticDescription, 600)}",
                $"Техника: {techniqueName}",
                $"Описание техники: {techniqueDescription}");

            var metadata = BuildMetadata(record);
            metadata["ParentTactic"] = tacticName;
            metadata["ParentTacticDescription"] = tacticDescription;

            return new RagDocument
            {
                Id = $"technique_{NormalizeIdPart(techniqueName)}",
                SourceKind = "database",
                Source = "TechniquesTactics",
                Type = "technique",
                Product = string.Empty,
                Title = techniqueName,
                Section = tacticName,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 9000),
                PromptText = LimitText(promptText, 3500),
                Metadata = metadata
            };
        }

        // =========================
        // Exploit
        // =========================

        private static RagDocument CreateExploitDocument(JsonElement record, int index)
        {
            var values = BuildMetadata(record);

            var id = GuessValue(
                values,
                "Id", "ID", "Identifier", "Code",
                "ExploitId", "EDBID", "ExploitDB",
                "Код", "Идентификатор", "Номер");

            if (string.IsNullOrWhiteSpace(id))
            {
                id = $"exploit_{index}";
            }

            var title = GuessValue(
                values,
                "Name", "Title", "ExploitName",
                "FullName", "ShortName",
                "Наименование", "Название");

            if (string.IsNullOrWhiteSpace(title))
            {
                title = id;
            }

            var cve = GuessValue(
                values,
                "CVE", "Cve", "CveId", "Incident",
                "VulnerabilityId", "BDU", "Bdu",
                "СвязанныйCVE", "ИдентификаторУязвимости");

            var platform = GuessValue(
                values,
                "Platform", "OS", "OperatingSystem",
                "Платформа", "ОС", "Система");

            var exploitType = GuessValue(
                values,
                "Type", "ExploitType", "Category",
                "Тип", "Категория");

            var description = GuessValue(
                values,
                "Description", "Desc", "Text", "Content",
                "Описание", "Текст");

            var date = GuessValue(
                values,
                "Date", "Published", "PublicationDate", "Created",
                "Дата", "ДатаПубликации");

            var searchText = BuildText(
                "Exploit",
                "exploit",
                "эксплойт",
                "публичный эксплойт",
                id,
                title,
                cve,
                platform,
                exploitType,
                description,
                date,
                string.Join(Environment.NewLine, values.Select(pair => $"{pair.Key}: {pair.Value}")));

            var promptText = BuildText(
                "Источник: Эксплойты",
                "Тип: exploit",
                $"ID: {id}",
                $"Название: {title}",
                Field("Связанная уязвимость / CVE / BDU", cve),
                Field("Платформа", platform),
                Field("Тип эксплойта", exploitType),
                Field("Дата", date),
                Field("Описание", description),
                "Важно: использовать сведения об эксплойте только для оценки риска и наличия публичной эксплуатации. Не давать инструкции по эксплуатации уязвимости.",
                "Поля записи:",
                string.Join(Environment.NewLine, values.Take(20).Select(pair =>
                    $"- {pair.Key}: {LimitText(pair.Value, 500)}")));

            return new RagDocument
            {
                Id = $"exploit_{NormalizeIdPart(id)}",
                SourceKind = "database",
                Source = "Exploit",
                Type = "exploit",
                Product = platform,
                Title = LimitText(title, 300),
                Section = LimitText(exploitType, 300),
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 12000),
                PromptText = LimitText(promptText, 4500),
                Metadata = values
            };
        }

        // =========================
        // Outcomes
        // =========================

        private static List<RagDocument> LoadOutcomeDocuments(JsonElement root)
        {
            var documents = new List<RagDocument>();

            if (!TryGetProperty(root, "Value", out var valueElement) ||
                valueElement.ValueKind != JsonValueKind.Object)
            {
                return documents;
            }

            var index = 0;

            if (TryGetProperty(valueElement, "Technologys", out var technologiesElement) &&
                technologiesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var technology in technologiesElement.EnumerateArray())
                {
                    if (technology.ValueKind == JsonValueKind.Object)
                    {
                        documents.Add(CreateRiskTechnologyDocument(technology, index++));
                    }
                }
            }

            if (TryGetProperty(valueElement, "Consequences", out var consequencesElement) &&
                consequencesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var consequence in consequencesElement.EnumerateArray())
                {
                    if (consequence.ValueKind == JsonValueKind.Object)
                    {
                        documents.Add(CreateConsequenceDocument(consequence, index++));
                    }
                }
            }

            return documents;
        }

        private static RagDocument CreateRiskTechnologyDocument(JsonElement record, int index)
        {
            var sequenceNumber = GetString(record, "SequenceNumber");
            var methodName = GetString(record, "MethodName");
            var description = GetString(record, "Description");
            var usage = GetString(record, "Usage");
            var scale = GetString(record, "Scale");
            var horizont = GetString(record, "Horizont");
            var level = GetString(record, "Level");
            var necessity = GetString(record, "Necessity");
            var experience = GetString(record, "Experience");
            var characteristic = GetString(record, "Сharacteristic");
            var effort = GetString(record, "Effort");

            var id = string.IsNullOrWhiteSpace(sequenceNumber)
                ? $"risk_technology_{index}"
                : $"risk_technology_{sequenceNumber}";

            var title = string.IsNullOrWhiteSpace(methodName)
                ? id
                : methodName;

            var searchText = BuildText(
                "Outcomes",
                "risk_technology",
                "метод оценки риска",
                "технология оценки риска",
                "оценка риска",
                id,
                title,
                description,
                usage,
                scale,
                horizont,
                level,
                necessity,
                experience,
                characteristic,
                effort);

            var promptText = BuildText(
                "Источник: Методы и технологии оценки риска",
                "Тип: risk_technology",
                $"ID: {id}",
                $"Метод: {title}",
                $"Описание: {description}",
                $"Применение: {usage}",
                $"Масштаб: {scale}",
                $"Горизонт: {horizont}",
                $"Уровень: {level}",
                $"Необходимость: {necessity}",
                $"Требуемый опыт: {experience}",
                $"Характеристика: {characteristic}",
                $"Трудоёмкость: {effort}");

            return new RagDocument
            {
                Id = id,
                SourceKind = "database",
                Source = "Outcomes",
                Type = "risk_technology",
                Product = string.Empty,
                Title = title,
                Section = usage,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 9000),
                PromptText = LimitText(promptText, 3500),
                Metadata = BuildMetadata(record)
            };
        }

        private static RagDocument CreateConsequenceDocument(JsonElement record, int index)
        {
            var number = GetString(record, "Number");
            var name = GetString(record, "Name");
            var damage = GetString(record, "Damage");
            var nameThreats = GetString(record, "NameThreats");

            var id = string.IsNullOrWhiteSpace(number)
                ? $"consequence_{index}"
                : $"consequence_{number}";

            var title = string.IsNullOrWhiteSpace(name)
                ? id
                : name;

            var searchText = BuildText(
                "Outcomes",
                "consequence",
                "последствие",
                "ущерб",
                "риск",
                id,
                title,
                damage,
                nameThreats,
                $"связанные угрозы {nameThreats}");

            var promptText = BuildText(
                "Источник: Последствия / риски",
                "Тип: consequence",
                $"ID: {id}",
                $"Номер: {number}",
                $"Последствие: {title}",
                $"Ущерб: {damage}",
                $"Связанные угрозы по номерам: {nameThreats}");

            return new RagDocument
            {
                Id = id,
                SourceKind = "database",
                Source = "Outcomes",
                Type = "consequence",
                Product = string.Empty,
                Title = title,
                Section = damage,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 7000),
                PromptText = LimitText(promptText, 2500),
                Metadata = BuildMetadata(record)
            };
        }

        // =========================
        // Specialist
        // =========================

        private static RagDocument CreateSpecialistDocument(JsonElement record, int index)
        {
            var nameOrgan = GetString(record, "NameOrgan");
            var highestOrgan = GetString(record, "NameHighestOrgan");
            var subordinateOrgan = GetString(record, "NameSubordinateOrgan");
            var statusVulnerability = GetString(record, "StatusVulnerability");
            var actionsTaken = GetString(record, "ActionsTaken");
            var software = GetString(record, "NameSoftware");
            var interactingOrgans = GetString(record, "NameInteractingOrgans");
            var usingMeasures = GetString(record, "UsingMeasures");

            var id = string.IsNullOrWhiteSpace(nameOrgan)
                ? $"specialist_{index}"
                : $"specialist_{NormalizeIdPart(nameOrgan)}";

            var title = string.IsNullOrWhiteSpace(nameOrgan)
                ? id
                : nameOrgan;

            var searchText = BuildText(
                "Specialist",
                "specialist",
                "организация",
                "специалист",
                "взаимодействие",
                "применяемые меры",
                title,
                highestOrgan,
                subordinateOrgan,
                statusVulnerability,
                actionsTaken,
                software,
                interactingOrgans,
                usingMeasures);

            var promptText = BuildText(
                "Источник: Специалисты / организационная информация",
                "Тип: specialist",
                $"ID: {id}",
                $"Орган: {title}",
                $"Вышестоящий орган: {highestOrgan}",
                $"Подчинённый орган: {subordinateOrgan}",
                $"Статус уязвимости: {statusVulnerability}",
                $"Принятые действия: {actionsTaken}",
                $"ПО: {software}",
                $"Взаимодействующие органы: {interactingOrgans}",
                $"Используемые меры защиты: {usingMeasures}");

            return new RagDocument
            {
                Id = id,
                SourceKind = "database",
                Source = "Specialist",
                Type = "specialist",
                Product = software,
                Title = title,
                Section = statusVulnerability,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 7000),
                PromptText = LimitText(promptText, 3000),
                Metadata = BuildMetadata(record)
            };
        }

        // =========================
        // Ontology
        // =========================

        private static List<RagDocument> LoadOntologyDocuments(JsonElement root)
        {
            var documents = new List<RagDocument>();
            var ontologyRecords = ExtractRecords(root).ToList();
            var index = 0;

            foreach (var ontology in ontologyRecords)
            {
                var ontologyName = GetString(ontology, "Name");
                var ontologyDescription = GetString(ontology, "Description");

                if (string.IsNullOrWhiteSpace(ontologyName))
                {
                    ontologyName = $"ontology_{index}";
                }

                documents.Add(CreateOntologyRootDocument(
                    ontology,
                    ontologyName,
                    ontologyDescription,
                    index++));

                if (!TryGetProperty(ontology, "Entities", out var entitiesElement) ||
                    entitiesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var entity in entitiesElement.EnumerateArray())
                {
                    if (entity.ValueKind == JsonValueKind.Object)
                    {
                        documents.Add(CreateOntologyEntityDocument(
                            entity,
                            ontologyName,
                            ontologyDescription,
                            index++));
                    }
                }
            }

            return documents;
        }

        private static RagDocument CreateOntologyRootDocument(
            JsonElement record,
            string ontologyName,
            string ontologyDescription,
            int index)
        {
            var entities = new List<string>();

            if (TryGetProperty(record, "Entities", out var entitiesElement) &&
                entitiesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var entity in entitiesElement.EnumerateArray().Take(25))
                {
                    if (entity.ValueKind == JsonValueKind.Object)
                    {
                        entities.Add(GetString(entity, "Name"));
                    }
                }
            }

            var searchText = BuildText(
                "Ontology",
                "ontology",
                "онтология",
                "терминология",
                "понятия",
                ontologyName,
                ontologyDescription,
                string.Join(Environment.NewLine, entities));

            var promptText = BuildText(
                "Источник: Онтология",
                "Тип: ontology",
                $"Название: {ontologyName}",
                $"Описание: {ontologyDescription}",
                "Основные сущности:",
                string.Join(Environment.NewLine, entities.Take(15)));

            return new RagDocument
            {
                Id = $"ontology_{NormalizeIdPart(ontologyName)}",
                SourceKind = "database",
                Source = "Ontology",
                Type = "ontology",
                Product = string.Empty,
                Title = ontologyName,
                Section = "Онтология",
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 12000),
                PromptText = LimitText(promptText, 3500),
                Metadata = BuildMetadata(record)
            };
        }

        private static RagDocument CreateOntologyEntityDocument(
            JsonElement record,
            string ontologyName,
            string ontologyDescription,
            int index)
        {
            var entityName = GetString(record, "Name");

            if (string.IsNullOrWhiteSpace(entityName))
            {
                entityName = $"ontology_entity_{index}";
            }

            var entityDescription = GetString(record, "Description");

            var searchText = BuildText(
                "Ontology",
                "ontology_entity",
                "термин",
                "понятие",
                "определение",
                ontologyName,
                ontologyDescription,
                entityName,
                entityDescription);

            var promptText = BuildText(
                "Источник: Онтология",
                "Тип: ontology_entity",
                $"Онтология: {ontologyName}",
                $"Термин: {entityName}",
                $"Определение: {entityDescription}");

            var metadata = BuildMetadata(record);
            metadata["OntologyName"] = ontologyName;
            metadata["OntologyDescription"] = ontologyDescription;

            return new RagDocument
            {
                Id = $"ontology_entity_{NormalizeIdPart(entityName)}",
                SourceKind = "database",
                Source = "Ontology",
                Type = "ontology_entity",
                Product = string.Empty,
                Title = entityName,
                Section = ontologyName,
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchText, 9000),
                PromptText = LimitText(promptText, 3000),
                Metadata = metadata
            };
        }

        // =========================
        // Fallback для остальных JSON
        // =========================

        private static RagDocument CreateFallbackDatabaseDocument(
            JsonElement record,
            string filePath,
            int index)
        {
            var values = BuildMetadata(record);

            var sourceInfo = GetSourceInfo(filePath);

            var id = GuessValue(
                values,
                "Id",
                "ID",
                "Identifier",
                "Code",
                "Number",
                "GuidId",
                "Name",
                "Title");

            if (string.IsNullOrWhiteSpace(id))
            {
                id = $"{sourceInfo.Source}_{index}";
            }

            var title = GuessValue(
                values,
                "Name",
                "Title",
                "GroupName",
                "Description",
                "FullName",
                "ShortName");

            if (string.IsNullOrWhiteSpace(title))
            {
                title = id;
            }

            var section = GuessValue(
                values,
                "Type",
                "Class",
                "Category",
                "Group",
                "Section");

            var searchTextBuilder = new StringBuilder();

            searchTextBuilder.AppendLine(sourceInfo.Source);
            searchTextBuilder.AppendLine(sourceInfo.Type);
            searchTextBuilder.AppendLine(title);

            foreach (var pair in values.Take(60))
            {
                searchTextBuilder.AppendLine(pair.Key);
                searchTextBuilder.AppendLine(LimitText(pair.Value, 1000));
            }

            var promptTextBuilder = new StringBuilder();

            promptTextBuilder.AppendLine($"Источник: {sourceInfo.Source}");
            promptTextBuilder.AppendLine($"Тип: {sourceInfo.Type}");
            promptTextBuilder.AppendLine($"ID: {id}");
            promptTextBuilder.AppendLine($"Название: {title}");

            if (!string.IsNullOrWhiteSpace(section))
            {
                promptTextBuilder.AppendLine($"Раздел/категория: {section}");
            }

            promptTextBuilder.AppendLine("Поля записи:");

            foreach (var pair in values.Take(25))
            {
                promptTextBuilder.AppendLine($"- {pair.Key}: {LimitText(pair.Value, 500)}");
            }

            return new RagDocument
            {
                Id = $"{sourceInfo.Source}_{NormalizeIdPart(id)}",
                SourceKind = "database",
                Source = sourceInfo.Source,
                Type = sourceInfo.Type,
                Product = string.Empty,
                Title = LimitText(title, 300),
                Section = LimitText(section, 300),
                Page = 0,
                ChunkIndex = index,
                SearchText = LimitText(searchTextBuilder.ToString(), 12000),
                PromptText = LimitText(promptTextBuilder.ToString(), 4500),
                Metadata = values
            };
        }

        // =========================
        // Общие методы
        // =========================

        private static Dictionary<string, string> BuildMetadata(JsonElement record)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (record.ValueKind != JsonValueKind.Object)
            {
                return metadata;
            }

            foreach (var property in record.EnumerateObject())
            {
                var value = ConvertJsonValueToText(property.Value);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    metadata[property.Name] = LimitText(value, 3000);
                }
            }

            return metadata;
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            if (!TryGetProperty(element, propertyName, out var property))
            {
                return string.Empty;
            }

            return ConvertJsonValueToText(property);
        }

        private static List<string> GetStringArray(JsonElement element, string propertyName)
        {
            var values = new List<string>();

            if (!TryGetProperty(element, propertyName, out var property))
            {
                return values;
            }

            if (property.ValueKind != JsonValueKind.Array)
            {
                var singleValue = ConvertJsonValueToText(property);

                if (!string.IsNullOrWhiteSpace(singleValue))
                {
                    values.Add(singleValue);
                }

                return values;
            }

            foreach (var item in property.EnumerateArray())
            {
                var text = ConvertJsonValueToText(item);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    values.Add(text);
                }
            }

            return values;
        }

        private static bool TryGetProperty(
            JsonElement element,
            string propertyName,
            out JsonElement property)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out property))
            {
                return true;
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var currentProperty in element.EnumerateObject())
                {
                    if (string.Equals(
                            currentProperty.Name,
                            propertyName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        property = currentProperty.Value;
                        return true;
                    }
                }
            }

            property = default;
            return false;
        }

        private static string ConvertJsonValueToText(JsonElement value)
        {
            try
            {
                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString()?.Trim() ?? string.Empty,
                    JsonValueKind.Number => value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    JsonValueKind.Array => ConvertArrayToText(value),
                    JsonValueKind.Object => LimitText(value.GetRawText(), 3000),
                    _ => value.GetRawText()
                };
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ConvertArrayToText(JsonElement arrayElement)
        {
            var parts = new List<string>();

            foreach (var item in arrayElement.EnumerateArray().Take(120))
            {
                var text = ConvertJsonValueToText(item);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    parts.Add(text);
                }
            }

            return LimitText(string.Join(Environment.NewLine, parts), 12000);
        }

        private static string GuessValue(
            Dictionary<string, string> values,
            params string[] names)
        {
            foreach (var name in names)
            {
                var exact = values.FirstOrDefault(pair =>
                    string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(exact.Value))
                {
                    return exact.Value;
                }
            }

            foreach (var name in names)
            {
                var contains = values.FirstOrDefault(pair =>
                    pair.Key.Contains(name, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(contains.Value))
                {
                    return contains.Value;
                }
            }

            return string.Empty;
        }

        private static (string Source, string Type) GetSourceInfo(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath)
                .ToLowerInvariant();

            if (fileName.Contains("protection"))
            {
                return ("ProtectionMeasures", "protection_measure");
            }

            if (fileName.Contains("tactic") || fileName.Contains("technique"))
            {
                return ("TechniquesTactics", "technique_tactic");
            }

            if (fileName.Contains("exploit"))
            {
                return ("Exploit", "exploit");
            }

            if (fileName.Contains("outcome"))
            {
                return ("Outcomes", "outcome");
            }

            if (fileName.Contains("specialist"))
            {
                return ("Specialist", "specialist");
            }

            if (fileName.Contains("ontology"))
            {
                return ("Ontology", "ontology");
            }

            if (fileName.Contains("status"))
            {
                return ("Status", "status");
            }

            if (fileName.Contains("nvd"))
            {
                return ("VulnerabilitieNvd", "vulnerability_nvd");
            }

            if (fileName.Contains("jvn"))
            {
                return ("VulnerabilitieJvn", "vulnerability_jvn");
            }

            if (fileName.Contains("vulnerabilitie") ||
                fileName.Contains("vulnerability"))
            {
                return ("Vulnerabilitie", "vulnerability");
            }

            return (Path.GetFileNameWithoutExtension(filePath), "database_record");
        }

        private static string ResolveDatabasePath(string databasePath)
        {
            if (!string.IsNullOrWhiteSpace(databasePath) &&
                Directory.Exists(databasePath))
            {
                return databasePath;
            }

            var candidates = new List<string>
            {
                Path.Combine(Environment.CurrentDirectory, "Database"),
                Path.Combine(Environment.CurrentDirectory, "Программа", "Database")
            };

            var directory = new DirectoryInfo(Environment.CurrentDirectory);

            for (var i = 0; i < 8 && directory != null; i++)
            {
                candidates.Add(Path.Combine(directory.FullName, "Database"));
                candidates.Add(Path.Combine(directory.FullName, "Программа", "Database"));

                directory = directory.Parent;
            }

            return candidates.FirstOrDefault(Directory.Exists) ?? string.Empty;
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

        private static string BuildText(params string[] parts)
        {
            return string.Join(
                Environment.NewLine,
                parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        private static string Field(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return $"{name}: {value}";
        }

        private static string ToYesNo(string value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "1" => "да",
                "0" => "нет",
                "true" => "да",
                "false" => "нет",
                _ => value ?? string.Empty
            };
        }

        private static string NormalizeIdPart(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "unknown";
            }

            var chars = text
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
                .ToArray();

            var result = new string(chars);

            while (result.Contains("__"))
            {
                result = result.Replace("__", "_");
            }

            return result.Trim('_');
        }

        private static string BuildProtectionMeasureId(
            string name,
            string number,
            int index)
        {
            if (!string.IsNullOrWhiteSpace(name) &&
                !string.IsNullOrWhiteSpace(number))
            {
                return $"{name}.{number}";
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return $"protection_measure_{index}";
        }

        private static string ExtractCveFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var matches = Regex
                .Matches(text, @"CVE-\d{4}-\d{4,7}", RegexOptions.IgnoreCase)
                .Select(match => match.Value.ToUpperInvariant())
                .Distinct()
                .ToList();

            return string.Join(", ", matches);
        }

        private static string LimitText(string? text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.Trim();

            if (maxChars <= 0 || normalized.Length <= maxChars)
            {
                return normalized;
            }

            return normalized[..maxChars].Trim() + "...";
        }

       
        
    }
}
