using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.Extensions;
using PragmaticAnalyzer.MVVM.Model.Rag;
using PragmaticAnalyzer.MVVM.ViewModel.Main;
using PragmaticAnalyzer.MVVM.ViewModel.Viewer;
using PragmaticAnalyzer.Services.Rag;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PragmaticAnalyzer.Services.LocalLlama;
using System.Collections.Generic;

namespace PragmaticAnalyzer.Services
{
    public class InfrastructureOrchestrator : IInfrastructureOrchestrator
    {
        private readonly IFileService _fileService = new FileService();
        private readonly VulConfig _vulConfig = new();
        private readonly ThreatConfig _threatConfig = new();
        private readonly ExploitConfig _exploitConfig = new();
        private readonly LastUpdateConfig _lastUpdateConfig = new();
        private readonly ObservableCollection<AvailableDatabaseConfig> _availableDatabasesConfig = [];
        private readonly ObservableCollection<ModelConfig> _wordTwoVecConfig = [];
        private readonly ObservableCollection<ModelConfig> _fastTextVecConfig = [];
        private readonly Dictionary<string, object> _filePathToDatabase = [];
        private readonly HashSet<Guid> _vulJvnHashSet = [];
        private readonly HashSet<Guid> _vulNvdHashSet = [];
        public static IApiService ApiService { get; } = new ApiService();
        public static RagIndexService RagIndexService { get; } = new RagIndexService();

        public static LocalLlamaService LocalLlamaService { get; } = new();

        public static bool IsLocalLlamaLoaded => LocalLlamaService.IsLoaded;

        public static string LocalLlamaStatusText { get; private set; } =
            "Встроенная GGUF-модель: не загружена";
        public MainViewModel MainVm { get; }
        public ThreatViewModel ThreatVm { get; }
        public VulnerabilitieViewModel VulnerabilitieVm { get; }
        public ExploitViewModel ExploitVm { get; set; }
        public OntologyViewModel OntologyVm { get; }
        public TacticViewModel TacticVm { get; }
        public ViolatorViewModel ViolatorVm { get; }
        public ProtectionMeasureViewModel ProtectionMeasureVm { get; }
        public SpecialistViewModel SpecialistVm { get; }
        public ReferenceStatusViewModel ReferenceStatusVm { get; }
        public CurrentStatusViewModel CurrentStatusVm { get; }
        public OutcomesViewModel OutcomeVm { get; }
        public SetViewModel SetVm { get; }
        public ViewerViewModel ViewerVm { get; }
        public SettingViewModel SettingVm { get; }
        public ConnectionViewModel ConnectionVm { get; }
        public InformationViewModel InformationVm { get; }
        public CreatorViewModel CreatorVm { get; }
        public CommunicationViewModel CommunicationVm { get; }

        public static void StartRagBackgroundLoading()
        {
            _ = Task.Run(async () =>
            {
                await LoadRagIndexInBackgroundAsync();
            });
        }

        public static async Task EnsureLocalLlamaLoadedAsync(
    int contextSize = 4096,
    int gpuLayerCount = 0,
    int threadCount = 0,
    int batchSize = 512,
    int microBatchSize = 512,
    int readinessProbeMaxTokens = 24,
    CancellationToken ct = default)
        {
            if (LocalLlamaService.IsLoaded)
            {
                return;
            }

            var modelPath = ResolveLocalGgufModelPath();

            if (string.IsNullOrWhiteSpace(modelPath))
            {
                LocalLlamaStatusText = "Встроенная GGUF-модель: файл .gguf не найден";

                throw new FileNotFoundException(
                    "GGUF-модель не найдена. Положи .gguf файл в папку Translator рядом с exe или внутри проекта PragmaticAnalyzer.");
            }

            try
            {
                LocalLlamaStatusText = "Встроенная GGUF-модель: загрузка...";

                await LocalLlamaService.LoadAsync(
                    modelPath,
                    contextSize,
                    gpuLayerCount,
                    threadCount,
                    batchSize,
                    microBatchSize,
                    readinessProbeMaxTokens,
                    ct: ct);

                LocalLlamaStatusText =
                    $"Встроенная GGUF-модель: загружена ({Path.GetFileName(modelPath)})";
            }
            catch (OperationCanceledException)
            {
                LocalLlamaStatusText = "Встроенная GGUF-модель: загрузка отменена";
                throw;
            }
            catch (Exception ex)
            {
                LocalLlamaStatusText =
                    $"Встроенная GGUF-модель: ошибка загрузки — {ex.Message}";

                throw;
            }
        }

        private static string ResolveLocalGgufModelPath()
        {
            var candidates = new List<string>
    {
        Path.Combine(Environment.CurrentDirectory, "Translator")
    };

            var directory = new DirectoryInfo(Environment.CurrentDirectory);

            for (var i = 0; i < 8 && directory != null; i++)
            {
                candidates.Add(Path.Combine(directory.FullName, "Translator"));

                directory = directory.Parent;
            }

            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(candidate))
                {
                    continue;
                }

                var modelPath = Directory
                    .EnumerateFiles(candidate, "*.gguf", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(modelPath))
                {
                    return modelPath;
                }
            }

            return string.Empty;
        }
        public static async Task LoadRagIndexInBackgroundAsync(CancellationToken ct = default)
        {
            try
            {
                IsRagLoading = true;
                IsRagReady = false;
                RagStatusText = "RAG: загрузка индекса...";

                var fileService = new FileService();

                var ragConfig = await fileService.LoadFileToPathAsync<RagConfig>(
                    GlobalConfig.RagConfigPath,
                    ct);

                ragConfig ??= new RagConfig();
                var ragConfigChanged = ragConfig.NormalizePortablePaths();

                // По твоему решению RAG должен быть подключён всегда.
                ragConfig.IsEnabled = true;

                if (ragConfigChanged)
                {
                    await fileService.SaveFileAsync(
                        ragConfig,
                        GlobalConfig.RagConfigPath,
                        ct);
                }

                // 1. Сначала пытаемся быстро загрузить готовый общий индекс.
                if (RagIndexService.HasSavedIndex(
                        ragConfig.KnowledgeBasePath,
                        RagIndexPart.All))
                {
                    var loadedCount = await RagIndexService.LoadFromDiskAsync(
                        ragConfig.KnowledgeBasePath,
                        RagIndexPart.All,
                        ct);

                    if (loadedCount > 0)
                    {
                        IsRagReady = true;
                        RagStatusText = $"RAG: готово, документов: {loadedCount}";
                        return;
                    }
                }

                // 2. Если готового индекса нет — строим индекс из JSON-баз и руководств.
                RagStatusText = "RAG: индекс не найден, построение из баз и руководств...";

                RagIndexService.Clear();

                var databaseDocumentsCount = 0;

                if (ragConfig.UseProjectDatabases)
                {
                    databaseDocumentsCount = RagIndexService.LoadProjectDatabases(
                        ragConfig.ProjectDatabasePath);

                    var databaseDocuments = RagIndexService.Documents
                        .Where(document => document.SourceKind == "database")
                        .ToList();

                    if (databaseDocuments.Count > 0)
                    {
                        await RagIndexService.SaveDocumentsToDiskAsync(
                            ragConfig.KnowledgeBasePath,
                            databaseDocuments,
                            RagIndexPart.Databases,
                            ct);
                    }
                }

                var manualDocumentsCount = 0;

                if (ragConfig.UseManuals)
                {
                    manualDocumentsCount = RagIndexService.LoadManuals(
                        ragConfig.KnowledgeBasePath);

                    var manualDocuments = RagIndexService.Documents
                        .Where(document => document.SourceKind == "manual")
                        .ToList();

                    if (manualDocuments.Count > 0)
                    {
                        await RagIndexService.SaveDocumentsToDiskAsync(
                            ragConfig.KnowledgeBasePath,
                            manualDocuments,
                            RagIndexPart.Manuals,
                            ct);
                    }
                }

                var totalDocumentsCount = RagIndexService.DocumentCount;

                if (totalDocumentsCount > 0)
                {
                    await RagIndexService.SaveToDiskAsync(
                        ragConfig.KnowledgeBasePath,
                        RagIndexPart.All,
                        ct);

                    IsRagReady = true;
                    RagStatusText =
                        $"RAG: индекс построен, документов: {totalDocumentsCount} " +
                        $"(базы: {databaseDocumentsCount}, руководства: {manualDocumentsCount})";
                }
                else
                {
                    IsRagReady = false;
                    RagStatusText = "RAG: документы не найдены";
                }
            }
            catch (OperationCanceledException)
            {
                IsRagReady = false;
                RagStatusText = "RAG: загрузка отменена";
            }
            catch (Exception ex)
            {
                IsRagReady = false;
                RagStatusText = $"RAG: ошибка загрузки — {ex.Message}";
            }
            finally
            {
                IsRagLoading = false;
            }
        }
        public static bool IsRagLoading { get; private set; }

        public static bool IsRagReady { get; private set; }

        public static string RagStatusText { get; private set; } = "RAG: не загружен";

        public static int RagDocumentCount => RagIndexService.DocumentCount;

        public static async Task<int> LoadRagManualsAsync(CancellationToken ct = default)
        {
            var fileService = new FileService();

            var ragConfig = await fileService.LoadFileToPathAsync<RagConfig>(
                GlobalConfig.RagConfigPath,
                ct);

            ragConfig ??= new RagConfig();
            ragConfig.NormalizePortablePaths();

            return await Task.Run(() =>
            {
                RagIndexService.Clear();

                var loadedCount = RagIndexService.LoadManuals(
                    ragConfig.KnowledgeBasePath);

                return loadedCount;
            }, ct);
        }


        public InfrastructureOrchestrator()
        {
            MainVm = new(this);
            SetVm = new(_lastUpdateConfig, this);
            ThreatVm = new([], SetVm.UpdateConfig, _threatConfig, MainVm.OnSetCurrentView);
            VulnerabilitieVm = new(SetVm.UpdateConfig, _vulConfig, _vulJvnHashSet, _vulNvdHashSet, MainVm.OnSetCurrentView);
            ExploitVm = new([], SetVm.UpdateConfig, _exploitConfig, MainVm.OnSetCurrentView);
            OntologyVm = new([]);
            TacticVm = new([], SetVm.UpdateConfig, MainVm.OnSetCurrentView);
            ViolatorVm = new([], SetVm.UpdateConfig, MainVm.OnSetCurrentView);
            ProtectionMeasureVm = new([], SetVm.UpdateConfig, MainVm.OnSetCurrentView);
            SpecialistVm = new([], SetVm.UpdateConfig, MainVm.OnSetCurrentView);
            ReferenceStatusVm = new([], SetVm.UpdateConfig, MainVm.OnSetCurrentView);
            CurrentStatusVm = new([], SetVm.UpdateConfig, MainVm.OnSetCurrentView);
            OutcomeVm = new(new(), SetVm.UpdateConfig, MainVm.OnSetCurrentView);
            ViewerVm = new(this, _lastUpdateConfig, MainVm.OnSetCurrentView);
            ConnectionVm = new(this, ApiService, _availableDatabasesConfig, _filePathToDatabase);
            SettingVm = new(_wordTwoVecConfig, _fastTextVecConfig, ApiService);
            InformationVm = new(MainVm.OnSetCurrentView);
            CreatorVm = new([], SaveDatabaseAsync, DeleteDatabase);
            CommunicationVm = new(ApiService, _fileService);

            SetVm.UpdateThreatDb = ThreatVm.UpdateThreatDb;
            SetVm.UpdateExploitDb = ExploitVm.UpdateExploitDb;
            SetVm.UpdateVulDb = VulnerabilitieVm.UpdateVulDb;
        }

        public async Task InitializeAsync()
        {
            if (!Directory.Exists(GlobalConfig.DatabasePath))
            {
                Directory.CreateDirectory(GlobalConfig.DatabasePath);
            }      //
            if (!Directory.Exists(GlobalConfig.ExploitTextPath))
            {
                Directory.CreateDirectory(GlobalConfig.ExploitTextPath);
            }   //      проверка наличия каталогов
            if (!Directory.Exists(GlobalConfig.ModelsPath))
            {
                Directory.CreateDirectory(GlobalConfig.ModelsPath);
            }        //
            if (!Directory.Exists(GlobalConfig.ConfigPath))
            {
                Directory.CreateDirectory(GlobalConfig.ConfigPath);
            }        //

            if (File.Exists(GlobalConfig.LastUpdateConfig))
            {
                _lastUpdateConfig.Update(await _fileService.LoadDTOAsync<LastUpdateConfig>(GlobalConfig.LastUpdateConfig, DataType.LastUpdateConfig) ?? new());
            }              //
            if (File.Exists(GlobalConfig.ExploitConfigPath))
            {
                _exploitConfig.Update(await _fileService.LoadDTOAsync<ExploitConfig>(GlobalConfig.ExploitConfigPath, DataType.ExploitConfig) ?? new());
            }             //
            if (File.Exists(GlobalConfig.ThreatConfigPath))
            {
                _threatConfig.Update(await _fileService.LoadDTOAsync<ThreatConfig>(GlobalConfig.ThreatConfigPath, DataType.ThreatConfig) ?? new());
            }             //        проверка наличия конфигурационных файлов
            if (File.Exists(GlobalConfig.VulConfigPath))
            {
                _vulConfig.Update(await _fileService.LoadDTOAsync<VulConfig>(GlobalConfig.VulConfigPath, DataType.VulConfig) ?? new());
            }                  //
            if (File.Exists(GlobalConfig.VulNvdHashSetPath))
            {
                HashSet<Guid> hashSet = await _fileService.LoadFileToPathAsync<HashSet<Guid>>(GlobalConfig.VulNvdHashSetPath) ?? [];
                foreach (var item in hashSet)
                {
                    _vulNvdHashSet.Add(item);
                }
            }         //
            if (File.Exists(GlobalConfig.VulJvnHashSetPath))
            {
                HashSet<Guid> hashSet = await _fileService.LoadFileToPathAsync<HashSet<Guid>>(GlobalConfig.VulJvnHashSetPath) ?? [];
                foreach (var item in hashSet)
                {
                    _vulJvnHashSet.Add(item);
                }
            }          //
            if (File.Exists(GlobalConfig.WordTwoVecConfigPath))
            {
                var wordTwoVecConfigs = await LoadMatcherModelConfigsAsync(
                    GlobalConfig.WordTwoVecConfigPath,
                    DataType.WordTwoVecConfig,
                    Algorithm.WordTwoVec);

                _wordTwoVecConfig.ReplaceAll(wordTwoVecConfigs);
            } //
            if (File.Exists(GlobalConfig.FastTextConfigPath))
            {
                var fastTextConfigs = await LoadMatcherModelConfigsAsync(
                    GlobalConfig.FastTextConfigPath,
                    DataType.FastTextConfig,
                    Algorithm.FastText);

                _fastTextVecConfig.ReplaceAll(fastTextConfigs);
            }         //

            var vulFstecDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<VulnerabilitieFstec>>>(GlobalConfig.VulnerabilitieFstecPath);
            if (vulFstecDto != default)
            {
                foreach (var vul in vulFstecDto.Value)
                {
                    VulnerabilitieVm.VulnerabilitiesFstec.Add(vul);
                }
                _filePathToDatabase.Add(GlobalConfig.VulnerabilitieFstecPath, VulnerabilitieVm.DisplayedVulnerabilities);
                SetVm.UpdateConfig?.Invoke(File.GetLastWriteTime(GlobalConfig.VulnerabilitieFstecPath).ToString("f"), DataType.VulnerabilitiesFstec); //эталон без дто
            }

            var vulNvdDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<VulnerabilitieNvd>>>(GlobalConfig.VulnerabilitieNvdPath);
            if (vulNvdDto != default)
            {
                foreach (var vul in vulNvdDto.Value)
                {
                    VulnerabilitieVm.VulnerabilitiesNvd.Add(vul);
                }
                _filePathToDatabase.Add(GlobalConfig.VulnerabilitieNvdPath, VulnerabilitieVm.VulnerabilitiesNvd);
            }

            var vulJvnDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<VulnerabilitieJvn>>>(GlobalConfig.VulnerabilitieJvnPath);
            if (vulJvnDto != default)
            {
                foreach (var vul in vulJvnDto.Value)
                {
                    VulnerabilitieVm.VulnerabilitiesJvn.Add(vul);
                }
                _filePathToDatabase.Add(GlobalConfig.VulnerabilitieJvnPath, VulnerabilitieVm.VulnerabilitiesJvn);
            }

            var threatDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<Threat>>>(GlobalConfig.ThreatPath);
            if (threatDto != default)
            {
                foreach (var threat in threatDto.Value)
                {
                    ThreatVm.Threats.Add(threat);
                }
                _filePathToDatabase.Add(GlobalConfig.ThreatPath, ThreatVm.Threats);
                SetVm.UpdateConfig?.Invoke(threatDto.DateCreation.ToString("f"), DataType.Threat);
            }

            var protectionMeasureDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<ProtectionMeasure>>>(GlobalConfig.ProtectionMeasurePath);
            if (protectionMeasureDto != default)
            {
                foreach (var protectionMeasure in protectionMeasureDto.Value)
                {
                    ProtectionMeasureVm.ProtectionMeasures.Add(protectionMeasure);
                }
                SetVm.UpdateConfig?.Invoke(protectionMeasureDto.DateCreation.ToString("f"), DataType.ProtectionMeasures);
            }

            var techniquesTacticDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<Tactic>>>(GlobalConfig.TacticPath);
            if (techniquesTacticDto != default)
            {
                foreach (var techniquesTactic in techniquesTacticDto.Value)
                {
                    TacticVm.Tactics.Add(techniquesTactic);
                }
                _filePathToDatabase.Add(GlobalConfig.TacticPath, TacticVm.Tactics);
                SetVm.UpdateConfig?.Invoke(techniquesTacticDto.DateCreation.ToString("f"), DataType.Tactic);
            }

            var exploitDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<Exploit>>>(GlobalConfig.ExploitPath);
            if (exploitDto != default)
            {
                foreach (var exploit in exploitDto.Value)
                {
                    ExploitVm.Exploits.Add(exploit);
                }
                _filePathToDatabase.Add(GlobalConfig.ExploitPath, ExploitVm.Exploits);
                SetVm.UpdateConfig?.Invoke(exploitDto.DateCreation.ToString("f"), DataType.Exploit);
            }

            var outcomes = await _fileService.LoadDTOAsync<Outcomes>(GlobalConfig.OutcomesPath, DataType.Outcomes);
            if (outcomes != default)
            {
                foreach (var technology in outcomes.Technologys)
                {
                    OutcomeVm.Outcomes.Technologys.Add(technology);
                }
                foreach (var consequence in outcomes.Consequences)
                {
                    OutcomeVm.Outcomes.Consequences.Add(consequence);
                }
                SetVm.UpdateConfig?.Invoke(File.GetLastWriteTime(GlobalConfig.OutcomesPath).ToString("f"), DataType.Outcomes);
            }

            var specialistDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<Specialist>>>(GlobalConfig.SpecialistPath);
            if (specialistDto != default)
            {
                foreach (var specialist in specialistDto.Value)
                {
                    SpecialistVm.Specialists.Add(specialist);
                }
                SetVm.UpdateConfig?.Invoke(specialistDto.DateCreation.ToString("f"), DataType.Specialist);
            }

            var violatorDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<Violator>>>(GlobalConfig.ViolatorPath);
            if (violatorDto != default)
            {
                foreach (var violator in violatorDto.Value)
                {
                    ViolatorVm.Violators.Add(violator);
                }
                _filePathToDatabase.Add(GlobalConfig.ViolatorPath, ViolatorVm.Violators);
                SetVm.UpdateConfig?.Invoke(violatorDto.DateCreation.ToString("f"), DataType.Violator);
            }

            var currentStatusDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<CurrentStatus>>>(GlobalConfig.CurStatPath);
            if (currentStatusDto != default)
            {
                foreach (var currentStatus in currentStatusDto.Value)
                {
                    CurrentStatusVm.CurrentsStatus.Add(currentStatus);
                }
                SetVm.UpdateConfig?.Invoke(currentStatusDto.DateCreation.ToString("f"), DataType.CurrentStatus);
            }

            var referenceStatusDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<ReferenceStatus>>>(GlobalConfig.RefStatPath);
            if (referenceStatusDto != default)
            {
                foreach (var referenceStatus in referenceStatusDto.Value)
                {
                    ReferenceStatusVm.ReferencesStatus.Add(referenceStatus);
                }
                SetVm.UpdateConfig?.Invoke(referenceStatusDto.DateCreation.ToString("f"), DataType.ReferenceStatus);
            }

            var ontologyDto = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<Ontology>>>(GlobalConfig.OntologyPath);
            if (ontologyDto != default)
            {
                foreach (var ontology in ontologyDto.Value)
                {
                    OntologyVm.Ontologys.Add(ontology);
                }
            }

            var vulNvdTranslated = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<VulnerabilitieNvd>>>(GlobalConfig.VulnerabilitieNvdTranslated);
            if (vulNvdTranslated != default)
            {
                foreach (var vul in vulNvdTranslated.Value)
                {
                    VulnerabilitieVm.VulnerabilitiesNvdTranslated.Add(vul);
                }
            }

            var vulJvnTranslated = await _fileService.LoadFileToPathAsync<DTO<ObservableCollection<VulnerabilitieJvn>>>(GlobalConfig.VulnerabilitieJvnTranslated);
            if (vulJvnTranslated != default)
            {
                foreach (var vul in vulJvnTranslated.Value)
                {
                    VulnerabilitieVm.VulnerabilitiesJvnTranslated.Add(vul);
                }
            }

            var schemes = await _fileService.LoadDTOAsync<ObservableCollection<DynamicDatabase>>(GlobalConfig.SchemeDatabasePath, DataType.SchemeDatabase);
            if (schemes != default)
            {
                foreach (var scheme in schemes)
                {
                    CreatorVm.Databases.Add(scheme);
                    string recordsPath = Path.Combine(GlobalConfig.DatabasePath, scheme.Name + ".json");
                    var records = await _fileService.LoadDTOAsync<ObservableCollection<DynamicRecord>>(recordsPath, DataType.DunamicDatabase);
                    if (records != default)
                    {
                        foreach (var record in records)
                        {
                            record.NameDatadase = scheme.Name;
                        }
                        CreatorVm.Databases.Last().Records.ReplaceAll(records);
                        _filePathToDatabase.Add(recordsPath, records);
                    }
                }
            }

            SettingVm.NotifySelectedModels(); // оповещение о выборе модели
            _availableDatabasesConfig.ReplaceAll(FileService.GetAvailableDatabaseConfigs()); // обновление используемых баз данных
            ApiService.StartServer(); // запуск серверов
            StartRagBackgroundLoading();
        } // проверятет все глобальные пути (конфиги, базы данных) если нет - создает, если есть - загргужает

        private async Task<ObservableCollection<ModelConfig>> LoadMatcherModelConfigsAsync(
            string configPath,
            DataType dataType,
            Algorithm expectedAlgorithm)
        {
            var loadedConfigs =
                await _fileService.LoadDTOAsync<ObservableCollection<ModelConfig>>(configPath, dataType) ?? [];

            var normalizedConfigs = new ObservableCollection<ModelConfig>();
            var changed = false;

            foreach (var config in loadedConfigs)
            {
                if (!TryNormalizeMatcherModelConfig(config, expectedAlgorithm, out var normalizedConfig))
                {
                    changed = true;
                    continue;
                }

                changed |= !ReferenceEquals(config, normalizedConfig)
                    || config.Algorithm != expectedAlgorithm
                    || !string.Equals(config.Path, normalizedConfig.Path, StringComparison.OrdinalIgnoreCase);

                normalizedConfigs.Add(normalizedConfig);
            }

            changed |= NormalizeUsedModel(normalizedConfigs);

            if (changed)
            {
                await _fileService.SaveDTOAsync(normalizedConfigs, dataType, configPath);
            }

            return normalizedConfigs;
        }

        private static bool TryNormalizeMatcherModelConfig(
            ModelConfig config,
            Algorithm expectedAlgorithm,
            out ModelConfig normalizedConfig)
        {
            normalizedConfig = config;

            if (string.IsNullOrWhiteSpace(config.Path))
            {
                return false;
            }

            var fileName = Path.GetFileName(config.Path);
            if (string.IsNullOrWhiteSpace(fileName)
                || !string.Equals(Path.GetExtension(fileName), ".bin", StringComparison.OrdinalIgnoreCase)
                || LooksLikeAnotherMatcherAlgorithm(fileName, expectedAlgorithm))
            {
                return false;
            }

            var pathInModels = Path.GetFullPath(Path.Combine(GlobalConfig.ModelsPath, fileName));
            if (!File.Exists(pathInModels))
            {
                return false;
            }

            normalizedConfig = new ModelConfig
            {
                Path = pathInModels,
                Algorithm = expectedAlgorithm,
                IsUsed = config.IsUsed
            };

            return true;
        }

        private static bool NormalizeUsedModel(ObservableCollection<ModelConfig> configs)
        {
            if (configs.Count == 0)
            {
                return false;
            }

            var changed = false;
            var usedModelWasFound = false;

            foreach (var config in configs)
            {
                if (config.IsUsed && !usedModelWasFound)
                {
                    usedModelWasFound = true;
                    continue;
                }

                if (config.IsUsed)
                {
                    config.IsUsed = false;
                    changed = true;
                }
            }

            if (!usedModelWasFound)
            {
                configs[0].IsUsed = true;
                changed = true;
            }

            return changed;
        }

        private static bool LooksLikeAnotherMatcherAlgorithm(string fileName, Algorithm expectedAlgorithm)
        {
            var normalizedName = fileName.ToLowerInvariant();

            return expectedAlgorithm switch
            {
                Algorithm.WordTwoVec => normalizedName.Contains("fasttext") || normalizedName.Contains("fast_text"),
                Algorithm.FastText => normalizedName.Contains("word2vec")
                    || normalizedName.Contains("wordtwovec")
                    || normalizedName.Contains("word_two_vec"),
                _ => false
            };
        }

        public async Task SaveDatabaseAsync(object database, string name, DataType dataType)
        {
            var filePath = Path.Combine(GlobalConfig.DatabasePath, name + ".json");
            await _fileService.SaveDTOAsync(database, dataType, filePath);
            var fileInfo = new FileInfo(filePath);
            if (_availableDatabasesConfig.FirstOrDefault(config => config.FullName == filePath) is null)
            {
                AvailableDatabaseConfig config = new(Path.GetFileNameWithoutExtension(filePath), filePath, fileInfo.Length, fileInfo.LastWriteTimeUtc, dataType);
                _availableDatabasesConfig.Add(config);
                _filePathToDatabase.Add(filePath, database);
            }
        }

        public void DeleteDatabase(string name)
        {
            var filePath = Path.Combine(GlobalConfig.DatabasePath, name + ".json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _availableDatabasesConfig.Remove(_availableDatabasesConfig.First(config => config.FullName == filePath));
                _filePathToDatabase.Remove(filePath);
            }
        }

        public async Task CompletionWorkAsync()
        {
            try
            {
                await _fileService.SaveDTOAsync(SettingVm.WordTwoVecConfigs, DataType.WordTwoVecConfig, GlobalConfig.WordTwoVecConfigPath);
                await _fileService.SaveDTOAsync(SettingVm.FastTextConfigs, DataType.FastTextConfig, GlobalConfig.FastTextConfigPath);
            }
            finally
            {
                try
                {
                    CommunicationVm.Dispose();
                }
                finally
                {
                    LocalLlamaService.Unload();
                    ApiService.StopServer();
                }
            }
        }
    } // сервис инициализации программы
}
