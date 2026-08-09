using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.MVVM.Model.Rag;
using PragmaticAnalyzer.MVVM.ViewModel.Main;
using PragmaticAnalyzer.Services.LocalLlama;
using PragmaticAnalyzer.Services.Rag;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PragmaticAnalyzer.Services
{
    public class InfrastructureOrchestrator : IInfrastructureOrchestrator
    {
        private readonly IFileService _fileService = new FileService();

        public static IApiService ApiService { get; } = new ApiService();

        public static RagIndexService RagIndexService { get; } = new();

        public static LocalLlamaService LocalLlamaService { get; } = new();

        public static bool IsLocalLlamaLoaded => LocalLlamaService.IsLoaded;

        public static string LocalLlamaStatusText { get; private set; } =
            "Встроенная GGUF-модель: не загружена";

        public static bool IsRagLoading { get; private set; }

        public static bool IsRagReady { get; private set; }

        public static string RagStatusText { get; private set; } = "RAG: не загружен";

        public static int RagDocumentCount => RagIndexService.DocumentCount;

        public MainViewModel MainVm { get; }

        public CommunicationViewModel CommunicationVm { get; }

        public InfrastructureOrchestrator()
        {
            CommunicationVm = new CommunicationViewModel(ApiService, _fileService);
            MainVm = new MainViewModel(this);
        }

        public async Task InitializeCommunicationOnlyAsync()
        {
            EnsureCommunicationDirectories();
            ApiService.StartServer();
            StartRagBackgroundLoading();

            await Task.CompletedTask;
        }

        public Task CompletionWorkAsync()
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

            return Task.CompletedTask;
        }

        public static void StartRagBackgroundLoading()
        {
            _ = Task.Run(() => LoadRagIndexInBackgroundAsync());
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
                    "GGUF-модель не найдена. Положи .gguf файл в папку Translator рядом с exe или внутри проекта PragmaticAnalyzerChatOnly.");
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
                ragConfig.IsEnabled = true;

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

        public static async Task<int> LoadRagManualsAsync(CancellationToken ct = default)
        {
            var fileService = new FileService();
            var ragConfig = await fileService.LoadFileToPathAsync<RagConfig>(
                GlobalConfig.RagConfigPath,
                ct);

            ragConfig ??= new RagConfig();

            return await Task.Run(() =>
            {
                RagIndexService.Clear();

                return RagIndexService.LoadManuals(ragConfig.KnowledgeBasePath);
            }, ct);
        }

        private static void EnsureCommunicationDirectories()
        {
            Directory.CreateDirectory(GlobalConfig.DatabasePath);
            Directory.CreateDirectory(GlobalConfig.ConfigPath);
            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "KnowledgeBase"));
            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Translator"));
            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "NativeLlama"));
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
    }
}
