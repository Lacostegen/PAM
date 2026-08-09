using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.Model.Rag;
using PragmaticAnalyzer.Services;
using PragmaticAnalyzer.Services.LocalLlama;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class CommunicationViewModel : ViewModelBase
    {
        private const string FastResponseMode = "Быстро";
        private const string DetailedResponseMode = "Подробно";
        private const string ExpertResponseMode = "Экспертно";
        private const string AutoRagMode = "Авто";
        private const string DisabledRagMode = "Без базы";
        private const string OnlyRagMode = "Только по базе";
        private const string FastPerformanceProfile = "Быстро";
        private const string BalancedPerformanceProfile = "Баланс";
        private const string QualityPerformanceProfile = "Качество";
        private const string MaxDetailPerformanceProfile = "Максимально подробно";

        private readonly IFileService _fileService;
        private readonly IApiService _apiService;
        private readonly CancellationTokenSource _cts;
        private readonly DispatcherTimer _ragStatusTimer;
        private CancellationTokenSource? _generationCts;
        private RagConfig _ragConfig = new();
        private ChatMessage? _typingMessage;

        public ObservableCollection<ChatMessage> Messages { get; } = [];

        public ObservableCollection<string> ResponseModes { get; } =
        [
            FastResponseMode,
            DetailedResponseMode,
            ExpertResponseMode
        ];

        public ObservableCollection<string> RagModes { get; } =
        [
            AutoRagMode,
            DisabledRagMode,
            OnlyRagMode
        ];

        public ObservableCollection<string> PerformanceProfiles { get; } =
        [
            FastPerformanceProfile,
            BalancedPerformanceProfile,
            QualityPerformanceProfile,
            MaxDetailPerformanceProfile
        ];

        public string UserInput { get => Get<string>(); set => Set(value); }

        public string ResponseMode { get => Get<string>(); set => Set(value); }

        public string RagMode { get => Get<string>(); set => Set(value); }

        public string PerformanceProfile { get => Get<string>(); set => Set(value); }

        public bool UseCompactSystemPrompt { get => Get<bool>(); set => Set(value); }

        public bool IsSending { get => Get<bool>(); set => Set(value); }

        public bool IsBenchmarkRunning { get => Get<bool>(); set => Set(value); }

        public bool IsModelLoading { get => Get<bool>(); set => Set(value); }

        public bool CanCancelGeneration { get => Get<bool>(); set => Set(value); }

        public string ServerStatusText { get => Get<string>(); set => Set(value); }

        public string RagStatusText { get => Get<string>(); set => Set(value); }

        public string GenerationStatusText { get => Get<string>(); set => Set(value); }

        public string GenerationDiagnosticsText { get => Get<string>(); set => Set(value); }

        public string BenchmarkStatusText { get => Get<string>(); set => Set(value); }

        public string LastRequestSummaryText { get => Get<string>(); set => Set(value); }

        public string LastRagSourcesText { get => Get<string>(); set => Set(value); }

        public string HardwareProfileText { get => Get<string>(); set => Set(value); }

        public bool IsRagReady { get => Get<bool>(); set => Set(value); }

        public int RagDocumentCount { get => Get<int>(); set => Set(value); }

        public string LlamaServerPath { get => Get<string>(); set => Set(value); }

        public string ModelPath { get => Get<string>(); set => Set(value); }

        public string ModelEndpointText { get => Get<string>(); set => Set(value); }

        public string ModelLogPath { get => Get<string>(); set => Set(value); }

        public int ContextSize { get => Get<int>(); set => Set(value); }

        public int MaxTokens { get => Get<int>(); set => Set(value); }

        public double Temperature { get => Get<double>(); set => Set(value); }

        public double TopP { get => Get<double>(); set => Set(value); }

        public double RepeatPenalty { get => Get<double>(); set => Set(value); }

        public int HistoryMessagesCount { get => Get<int>(); set => Set(value); }

        public int GpuLayerCount { get => Get<int>(); set => Set(value); }

        public int ThreadCount { get => Get<int>(); set => Set(value); }

        public int BatchSize { get => Get<int>(); set => Set(value); }

        public int MicroBatchSize { get => Get<int>(); set => Set(value); }

        public int WarmUpMaxTokens { get => Get<int>(); set => Set(value); }

        public string SystemPrompt { get => Get<string>(); set => Set(value); }

        public bool IsServerAvailable { get => Get<bool>(); set => Set(value); }

        public CommunicationViewModel(IApiService apiService, IFileService fileService)
        {
            _apiService = apiService;
            _fileService = fileService;
            _cts = new CancellationTokenSource();

            IsSending = false;
            IsBenchmarkRunning = false;
            IsModelLoading = false;
            CanCancelGeneration = false;
            IsServerAvailable = false;
            ResponseMode = DetailedResponseMode;
            RagMode = AutoRagMode;
            PerformanceProfile = BalancedPerformanceProfile;
            UseCompactSystemPrompt = true;
            ServerStatusText = "🔴 Модель не проверена";
            RagStatusText = "RAG: не загружен";
            GenerationStatusText = string.Empty;
            GenerationDiagnosticsText = "Диагностика ответа появится после первой генерации.";
            BenchmarkStatusText = "Бенчмарк ещё не запускался.";
            LastRequestSummaryText = "Сводка последнего запроса появится после генерации.";
            LastRagSourcesText = "Источники RAG появятся после запроса с базой знаний.";
            HardwareProfileText = "Автонастройка под железо ещё не запускалась.";
            IsRagReady = false;
            RagDocumentCount = 0;
            ModelEndpointText = $"{LocalLlamaService.DefaultHost}:{LocalLlamaService.DefaultPort}";
            ModelLogPath = "Лог появится после запуска модели";
            WarmUpMaxTokens = 3;

            LoadTranslatorConfigAsync();
            LoadRagConfigAsync();

            _ragStatusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _ragStatusTimer.Tick += (_, _) => RefreshRagStatus();
            _ragStatusTimer.Start();

            RefreshRagStatus();

            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text = "👋 Привет! Я готов к диалогу. Задавайте вопросы."
            });
        }

        private async void LoadTranslatorConfigAsync()
        {
            var config = await _fileService.LoadFileToPathAsync<TranslatorConfig>(
                GlobalConfig.TranslatorConfigPath,
                _cts.Token);

            config ??= new TranslatorConfig();

            ResponseMode = NormalizeResponseMode(config.ResponseMode);
            RagMode = NormalizeRagMode(config.RagMode);
            PerformanceProfile = NormalizePerformanceProfile(config.PerformanceProfile);
            UseCompactSystemPrompt = config.UseCompactSystemPrompt;
            LlamaServerPath = config.LlamaServerPath;
            ModelPath = config.ModelPath;
            ContextSize = config.ContextSize;
            MaxTokens = config.MaxTokens;
            Temperature = config.Temperature;
            TopP = config.TopP;
            RepeatPenalty = config.RepeatPenalty;
            HistoryMessagesCount = config.HistoryMessagesCount;
            GpuLayerCount = config.GpuLayerCount;
            ThreadCount = config.ThreadCount;
            BatchSize = config.BatchSize;
            MicroBatchSize = config.MicroBatchSize;
            WarmUpMaxTokens = Math.Clamp(config.ReadinessProbeMaxTokens <= 0 ? 3 : config.ReadinessProbeMaxTokens, 1, 3);
            SystemPrompt = NormalizeSystemPrompt(config.SystemPrompt);
        }

        private async Task SaveTranslatorConfigAsync()
        {
            SystemPrompt = NormalizeSystemPrompt(SystemPrompt);

            var config = new TranslatorConfig
            {
                ResponseMode = NormalizeResponseMode(ResponseMode),
                RagMode = NormalizeRagMode(RagMode),
                PerformanceProfile = NormalizePerformanceProfile(PerformanceProfile),
                UseCompactSystemPrompt = UseCompactSystemPrompt,
                LlamaServerPath = LlamaServerPath,
                ModelPath = ModelPath,
                Port = LocalLlamaService.DefaultPort.ToString(),
                ContextSize = ContextSize,
                MaxTokens = MaxTokens,
                Temperature = Temperature,
                TopP = TopP,
                RepeatPenalty = RepeatPenalty,
                HistoryMessagesCount = HistoryMessagesCount,
                GpuLayerCount = GpuLayerCount,
                ThreadCount = ThreadCount,
                BatchSize = BatchSize,
                MicroBatchSize = MicroBatchSize,
                ReadinessProbeMaxTokens = Math.Clamp(WarmUpMaxTokens <= 0 ? 3 : WarmUpMaxTokens, 1, 3),
                SystemPrompt = SystemPrompt
            };

            await _fileService.SaveFileAsync(
                config,
                GlobalConfig.TranslatorConfigPath,
                _cts.Token);
        }

        private async void LoadRagConfigAsync()
        {
            var config = await _fileService.LoadFileToPathAsync<RagConfig>(
                GlobalConfig.RagConfigPath,
                _cts.Token);

            _ragConfig = config ?? new RagConfig();
            _ragConfig.IsEnabled = true;
        }

        private async Task SaveRagConfigAsync()
        {
            _ragConfig.IsEnabled = true;

            await _fileService.SaveFileAsync(
                _ragConfig,
                GlobalConfig.RagConfigPath,
                _cts.Token);
        }

        public RelayCommand StartModelCommand => GetCommand(async _ =>
        {
            await StartModelAsync();
        });

        public RelayCommand SelectModelPathCommand => GetCommand(_ =>
        {
            var path = DialogService.OpenFileDialog(DialogService.GgufModelFilter);

            if (!string.IsNullOrWhiteSpace(path))
            {
                ModelPath = path;
            }
        });

        public RelayCommand SaveTranslatorConfigCommand => GetCommand(async _ =>
        {
            await SaveTranslatorConfigAsync();

            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text = "✅ Настройки модели сохранены."
            });
        });

        public RelayCommand ApplyRecommendedSettingsCommand => GetCommand(async _ =>
        {
            ApplyRecommendedSettings();
            await SaveTranslatorConfigAsync();
            await SaveRagConfigAsync();

            GenerationDiagnosticsText =
                $"Применены рекомендуемые настройки профиля «{NormalizePerformanceProfile(PerformanceProfile)}». " +
                "Если модель уже загружена, перезапусти её, чтобы применились context, batch, micro batch, GPU layers и потоки.";

            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text = $"✅ Применены рекомендуемые настройки профиля «{NormalizePerformanceProfile(PerformanceProfile)}»."
            });
        });

        public RelayCommand ApplyHardwareAutoSettingsCommand => GetCommand(async _ =>
        {
            await ApplyHardwareAutoSettingsAsync();
        });

        public RelayCommand RunBenchmarkCommand => GetCommand(async _ =>
        {
            await RunModelBenchmarkAsync();
        }, _ => !IsSending && !IsBenchmarkRunning);

        public RelayCommand StopModelCommand => GetCommand(_ =>
        {
            StopModel();
        });

        public RelayCommand SendCommand => GetCommand(async _ =>
        {
            await SendMessageAsync();
        }, _ => !IsSending);

        public RelayCommand CancelGenerationCommand => GetCommand(_ =>
        {
            _generationCts?.Cancel();
        }, _ => CanCancelGeneration);

        public RelayCommand CheckServerCommand => GetCommand(async _ =>
        {
            await CheckServerAvailabilityAsync(showMessage: true);
        });

        public RelayCommand ClearCommand => GetCommand(_ =>
        {
            ClearChat();
        });

        private async Task StartModelAsync()
        {
            ServerStatusText = "🟡 Запуск llama-server и загрузка GGUF-модели...";
            IsServerAvailable = false;

            await SaveTranslatorConfigAsync();

            try
            {
                await EnsureLocalLlamaLoadedFromSettingsAsync(_cts.Token);

                IsServerAvailable = true;
                ServerStatusText =
                    $"🟢 GGUF-модель загружена; warm-up {FormatDuration(InfrastructureOrchestrator.LocalLlamaService.LastWarmUpElapsed)}";
                ModelLogPath = InfrastructureOrchestrator.LocalLlamaService.CurrentLogPath;

                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = $"✅ Модель загружена, endpoint проверен, warm-up выполнен за {FormatDuration(InfrastructureOrchestrator.LocalLlamaService.LastWarmUpElapsed)}."
                });
            }
            catch (OperationCanceledException)
            {
                IsServerAvailable = false;
                ServerStatusText = "🔴 Загрузка модели отменена";

                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = "⏹️ Загрузка GGUF-модели отменена."
                });
            }
            catch (Exception ex)
            {
                IsServerAvailable = false;
                ServerStatusText = "🔴 GGUF-модель не загружена";

                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = $"❌ Не удалось загрузить GGUF-модель: {ex.Message}"
                });
            }
        }

        private void StopModel()
        {
            _generationCts?.Cancel();
            InfrastructureOrchestrator.LocalLlamaService.Unload();

            IsServerAvailable = false;
            CanCancelGeneration = false;
            ServerStatusText = "🔴 GGUF-модель выгружена";
            ModelLogPath = InfrastructureOrchestrator.LocalLlamaService.CurrentLogPath;

            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text = "🛑 GGUF-модель выгружена из памяти."
            });
        }

        private async Task<bool> CheckServerAvailabilityAsync(bool showMessage)
        {
            var isLoadedByApplication = InfrastructureOrchestrator.LocalLlamaService.IsLoaded;
            var isEndpointAvailable = isLoadedByApplication ||
                await _apiService.IsServerAvailableAsync(
                    LocalLlamaService.DefaultHost,
                    LocalLlamaService.DefaultPort.ToString(),
                    _cts.Token);

            IsServerAvailable = isEndpointAvailable;
            ModelLogPath = InfrastructureOrchestrator.LocalLlamaService.CurrentLogPath;

            ServerStatusText = isLoadedByApplication
                ? $"🟢 GGUF-модель загружена; warm-up {FormatDuration(InfrastructureOrchestrator.LocalLlamaService.LastWarmUpElapsed)}"
                : isEndpointAvailable
                    ? $"🟡 На {ModelEndpointText} отвечает внешний llama-server"
                    : "🔴 GGUF-модель не загружена";

            if (showMessage)
            {
                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = isLoadedByApplication
                        ? "✅ Модель загружена и готова к работе."
                        : isEndpointAvailable
                            ? "⚠️ Endpoint отвечает, но сервер был запущен не текущей сессией приложения. Для стабильной работы останови модель и запусти её из этой вкладки."
                            : $"❌ Модель ещё не загружена на {ModelEndpointText}. Нажми «Запустить модель»."
                });
            }

            return isEndpointAvailable;
        }

        private async Task ApplyHardwareAutoSettingsAsync()
        {
            HardwareProfileText = "Автонастройка под железо: анализ CPU, RAM и GPU...";

            try
            {
                var hardwareInfo = await DetectHardwareAsync(_cts.Token);
                var recommendedProfile = RecommendHardwarePerformanceProfile(hardwareInfo);

                PerformanceProfile = recommendedProfile;
                ApplyRecommendedSettings();
                ApplyHardwareSpecificSettings(hardwareInfo);

                await SaveTranslatorConfigAsync();
                await SaveRagConfigAsync();

                HardwareProfileText =
                    $"{BuildHardwareProfileText(hardwareInfo, recommendedProfile)} " +
                    "Если модель уже загружена, перезапусти её, чтобы применились GPU layers, threads, context и batch.";

                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = $"✅ Автонастройка под железо выполнена. Выбран профиль «{recommendedProfile}»."
                });
            }
            catch (Exception ex)
            {
                HardwareProfileText = $"Автонастройка под железо не выполнена: {ex.Message}";
            }
        }

        private static async Task<HardwareInfo> DetectHardwareAsync(CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                var hardwareInfo = new HardwareInfo
                {
                    CpuThreads = Environment.ProcessorCount
                };

                TryFillHardwareFromPowerShell(hardwareInfo);

                if (hardwareInfo.TotalMemoryBytes <= 0)
                {
                    hardwareInfo.TotalMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                }

                return hardwareInfo;
            }, ct);
        }

        private static void TryFillHardwareFromPowerShell(HardwareInfo hardwareInfo)
        {
            try
            {
                const string script =
                    "$gpu=Get-CimInstance Win32_VideoController | Select-Object Name,AdapterRAM;" +
                    "$ram=(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory;" +
                    "[pscustomobject]@{TotalPhysicalMemory=$ram;Gpus=$gpu} | ConvertTo-Json -Compress -Depth 4";

                using var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.StartInfo.ArgumentList.Add("-NoProfile");
                process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
                process.StartInfo.ArgumentList.Add("Bypass");
                process.StartInfo.ArgumentList.Add("-Command");
                process.StartInfo.ArgumentList.Add(script);

                if (!process.Start())
                {
                    return;
                }

                if (!process.WaitForExit(4000))
                {
                    process.Kill(entireProcessTree: true);
                    return;
                }

                var output = process.StandardOutput.ReadToEnd();

                if (string.IsNullOrWhiteSpace(output))
                {
                    return;
                }

                using var document = JsonDocument.Parse(output);
                var root = document.RootElement;

                if (root.TryGetProperty("TotalPhysicalMemory", out var memoryElement))
                {
                    hardwareInfo.TotalMemoryBytes = ReadJsonInt64(memoryElement);
                }

                if (root.TryGetProperty("Gpus", out var gpusElement))
                {
                    ReadGpuElements(gpusElement, hardwareInfo.Gpus);
                }
            }
            catch
            {
                // Hardware detection is best-effort; CPU-only fallback is enough to continue.
            }
        }

        private static void ReadGpuElements(JsonElement element, List<GpuInfo> gpus)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    ReadGpuElement(item, gpus);
                }

                return;
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                ReadGpuElement(element, gpus);
            }
        }

        private static void ReadGpuElement(JsonElement element, List<GpuInfo> gpus)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            var name = element.TryGetProperty("Name", out var nameElement)
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;
            var adapterRamBytes = element.TryGetProperty("AdapterRAM", out var ramElement)
                ? ReadJsonInt64(ramElement)
                : 0;

            if (!string.IsNullOrWhiteSpace(name))
            {
                gpus.Add(new GpuInfo
                {
                    Name = name,
                    AdapterRamBytes = adapterRamBytes
                });
            }
        }

        private static long ReadJsonInt64(JsonElement element)
        {
            try
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Number when element.TryGetInt64(out var value) => value,
                    JsonValueKind.String when long.TryParse(element.GetString(), out var value) => value,
                    _ => 0
                };
            }
            catch
            {
                return 0;
            }
        }

        private static string RecommendHardwarePerformanceProfile(HardwareInfo hardwareInfo)
        {
            var cpuThreads = hardwareInfo.CpuThreads <= 0
                ? Environment.ProcessorCount
                : hardwareInfo.CpuThreads;
            var memoryGb = hardwareInfo.TotalMemoryGb;
            var nvidiaGpu = hardwareInfo.BestNvidiaGpu;

            if (nvidiaGpu == null)
            {
                return cpuThreads >= 10 && memoryGb >= 16
                    ? BalancedPerformanceProfile
                    : FastPerformanceProfile;
            }

            if (nvidiaGpu.AdapterRamGb >= 12 && memoryGb >= 32 && cpuThreads >= 12)
            {
                return MaxDetailPerformanceProfile;
            }

            if (nvidiaGpu.AdapterRamGb >= 8 && memoryGb >= 16)
            {
                return QualityPerformanceProfile;
            }

            return BalancedPerformanceProfile;
        }

        private void ApplyHardwareSpecificSettings(HardwareInfo hardwareInfo)
        {
            var cpuThreads = hardwareInfo.CpuThreads <= 0
                ? Environment.ProcessorCount
                : hardwareInfo.CpuThreads;
            var memoryGb = hardwareInfo.TotalMemoryGb;
            var nvidiaGpu = hardwareInfo.BestNvidiaGpu;

            ThreadCount = cpuThreads <= 4
                ? Math.Clamp(cpuThreads - 1, 1, 4)
                : Math.Clamp(cpuThreads - 2, 2, 16);

            if (nvidiaGpu == null)
            {
                GpuLayerCount = 0;
                BatchSize = 256;
                MicroBatchSize = 128;
            }
            else
            {
                GpuLayerCount = nvidiaGpu.AdapterRamGb switch
                {
                    >= 12 => 40,
                    >= 10 => 34,
                    >= 8 => 28,
                    >= 6 => 18,
                    _ => 10
                };
                BatchSize = 512;
                MicroBatchSize = nvidiaGpu.AdapterRamGb >= 8 ? 512 : 256;
            }

            if (memoryGb > 0 && memoryGb < 12)
            {
                ContextSize = Math.Min(ContextSize, 4096);
                MaxTokens = Math.Min(MaxTokens, 800);
                HistoryMessagesCount = Math.Min(HistoryMessagesCount, 3);
                _ragConfig.MaxTotalContextChars = Math.Min(_ragConfig.MaxTotalContextChars, 2500);
            }
            else if (memoryGb >= 24 && nvidiaGpu != null)
            {
                ContextSize = Math.Max(ContextSize, 8192);
            }
        }

        private static string BuildHardwareProfileText(
            HardwareInfo hardwareInfo,
            string recommendedProfile)
        {
            var cpuThreads = hardwareInfo.CpuThreads <= 0
                ? Environment.ProcessorCount
                : hardwareInfo.CpuThreads;
            var memoryText = hardwareInfo.TotalMemoryGb > 0
                ? $"{hardwareInfo.TotalMemoryGb:F1} ГБ RAM"
                : "RAM не определена";
            var gpuText = hardwareInfo.Gpus.Count == 0
                ? "GPU не определена"
                : string.Join(
                    "; ",
                    hardwareInfo.Gpus.Select(gpu =>
                        gpu.AdapterRamGb > 0
                            ? $"{gpu.Name} ({gpu.AdapterRamGb:F1} ГБ VRAM)"
                            : gpu.Name));

            return
                $"Железо: CPU потоков {cpuThreads}; {memoryText}; GPU: {gpuText}. " +
                $"Выбран профиль «{recommendedProfile}». " +
                "GPU layers выставляются автоматически только для NVIDIA; для CPU/неизвестной GPU используется безопасный CPU-режим.";
        }

        private sealed class HardwareInfo
        {
            public int CpuThreads { get; set; }

            public long TotalMemoryBytes { get; set; }

            public List<GpuInfo> Gpus { get; } = [];

            public double TotalMemoryGb => TotalMemoryBytes > 0
                ? TotalMemoryBytes / 1024.0 / 1024.0 / 1024.0
                : 0;

            public GpuInfo? BestNvidiaGpu => Gpus
                .Where(gpu => gpu.Name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                              gpu.Name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
                              gpu.Name.Contains("RTX", StringComparison.OrdinalIgnoreCase) ||
                              gpu.Name.Contains("GTX", StringComparison.OrdinalIgnoreCase) ||
                              gpu.Name.Contains("Quadro", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(gpu => gpu.AdapterRamBytes)
                .FirstOrDefault();
        }

        private sealed class GpuInfo
        {
            public string Name { get; set; } = string.Empty;

            public long AdapterRamBytes { get; set; }

            public double AdapterRamGb => AdapterRamBytes > 0
                ? AdapterRamBytes / 1024.0 / 1024.0 / 1024.0
                : 0;
        }

        private async Task RunModelBenchmarkAsync()
        {
            if (IsBenchmarkRunning)
            {
                return;
            }

            IsBenchmarkRunning = true;
            BenchmarkStatusText = "Бенчмарк: подготовка модели...";

            try
            {
                await EnsureLocalLlamaLoadedFromSettingsAsync(_cts.Token);

                var scenarios = BuildBenchmarkScenarios();
                var results = new List<BenchmarkRunResult>();

                for (var i = 0; i < scenarios.Count; i++)
                {
                    var scenario = scenarios[i];
                    BenchmarkStatusText = $"Бенчмарк: {i + 1}/{scenarios.Count} — {scenario.Name}...";
                    await YieldToUiAsync();

                    var result = await RunBenchmarkScenarioAsync(scenario, _cts.Token);
                    results.Add(result);

                    BenchmarkStatusText = BuildBenchmarkReport(results, recommendedProfile: string.Empty, isFinal: false);
                    await YieldToUiAsync();
                }

                var recommendedProfile = RecommendPerformanceProfile(results);
                PerformanceProfile = recommendedProfile;
                ApplyRecommendedSettings();
                await SaveTranslatorConfigAsync();
                await SaveRagConfigAsync();

                BenchmarkStatusText = BuildBenchmarkReport(results, recommendedProfile, isFinal: true);
                LastRequestSummaryText =
                    $"Бенчмарк завершён. Автоподбор выбрал профиль «{recommendedProfile}». " +
                    "Если модель уже была загружена, перезапусти её для применения context/batch/micro batch.";

                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = $"✅ Бенчмарк завершён. Рекомендованный профиль: «{recommendedProfile}»."
                });
            }
            catch (OperationCanceledException)
            {
                BenchmarkStatusText = "Бенчмарк остановлен.";
            }
            catch (Exception ex)
            {
                BenchmarkStatusText = $"Бенчмарк прерван ошибкой: {ex.Message}";
            }
            finally
            {
                IsBenchmarkRunning = false;
            }
        }

        private List<BenchmarkScenario> BuildBenchmarkScenarios()
        {
            var scenarios = new List<BenchmarkScenario>
            {
                new()
                {
                    Name = "короткий вопрос без RAG",
                    MaxTokens = 80,
                    Messages = BuildBenchmarkMessages(
                        "Кратко объясни, что такое RAG в локальном ассистенте.",
                        string.Empty)
                },
                new()
                {
                    Name = "технический ответ без RAG",
                    MaxTokens = 160,
                    Messages = BuildBenchmarkMessages(
                        "Дай структурированный список из пяти способов ускорить локальную GGUF-модель.",
                        string.Empty)
                }
            };

            var ragContext = BuildRagContext(
                "Какие меры защиты и ограничения важны для анализа угроз информационной безопасности?",
                AutoRagMode);

            if (!string.IsNullOrWhiteSpace(ragContext.ContextText))
            {
                scenarios.Add(new BenchmarkScenario
                {
                    Name = "ответ с RAG",
                    MaxTokens = 140,
                    RagSourceCount = ragContext.SourceCount,
                    Messages = BuildBenchmarkMessages(
                        "По найденному контексту кратко перечисли ключевые меры защиты.",
                        ragContext.ContextText)
                });
            }

            var historyMessages = BuildBenchmarkMessages(
                "С учётом предыдущего диалога кратко предложи оптимальный профиль скорости.",
                string.Empty);
            historyMessages.Insert(
                1,
                new LocalLlamaChatMessage(
                    "user",
                    "Мы настраиваем локальную GGUF-модель в WPF-приложении."));
            historyMessages.Insert(
                2,
                new LocalLlamaChatMessage(
                    "assistant",
                    "Я помогу подобрать параметры генерации и контекста."));

            scenarios.Add(new BenchmarkScenario
            {
                Name = "вопрос с историей",
                MaxTokens = 120,
                Messages = historyMessages
            });

            return scenarios;
        }

        private List<LocalLlamaChatMessage> BuildBenchmarkMessages(
            string userMessage,
            string ragContextText)
        {
            return
            [
                new LocalLlamaChatMessage(
                    "system",
                    BuildSystemMessage(
                        SystemPrompt,
                        ragContextText,
                        DetailedResponseMode,
                        string.IsNullOrWhiteSpace(ragContextText) ? DisabledRagMode : AutoRagMode,
                        UseCompactSystemPrompt)),
                new LocalLlamaChatMessage("user", userMessage)
            ];
        }

        private async Task<BenchmarkRunResult> RunBenchmarkScenarioAsync(
            BenchmarkScenario scenario,
            CancellationToken ct)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            TimeSpan? firstTokenDelay = null;
            var outputChars = 0;
            var chunks = 0;

            await foreach (var chunk in InfrastructureOrchestrator.LocalLlamaService.GenerateStreamAsync(
                               scenario.Messages,
                               scenario.MaxTokens,
                               (float)Math.Clamp(Temperature <= 0 ? 0.25 : Temperature, 0.0, 0.7),
                               (float)Math.Clamp(TopP <= 0 ? 0.9 : TopP, 0.3, 0.95),
                               (float)Math.Clamp(RepeatPenalty <= 0 ? 1.08 : RepeatPenalty, 1.0, 1.2),
                               ct))
            {
                if (!firstTokenDelay.HasValue)
                {
                    firstTokenDelay = stopwatch.Elapsed;
                }

                outputChars += chunk.Length;
                chunks++;
            }

            stopwatch.Stop();

            var outputTokens = EstimateTokenCountForDisplay(outputChars);
            var generationSeconds = GetEffectiveGenerationSeconds(firstTokenDelay, stopwatch.Elapsed);

            return new BenchmarkRunResult
            {
                Name = scenario.Name,
                PromptChars = scenario.Messages.Sum(message => message.Content.Length),
                MaxTokens = scenario.MaxTokens,
                OutputChars = outputChars,
                OutputTokens = outputTokens,
                Chunks = chunks,
                FirstTokenDelay = firstTokenDelay,
                TotalElapsed = stopwatch.Elapsed,
                TokensPerSecond = outputTokens > 0 ? outputTokens / generationSeconds : 0,
                RagSourceCount = scenario.RagSourceCount
            };
        }

        private static string RecommendPerformanceProfile(IReadOnlyCollection<BenchmarkRunResult> results)
        {
            var completed = results
                .Where(result => result.OutputChars > 0)
                .ToList();

            if (completed.Count == 0)
            {
                return FastPerformanceProfile;
            }

            var averageSpeed = completed.Average(result => result.TokensPerSecond);
            var averageFirstToken = completed
                .Where(result => result.FirstTokenDelay.HasValue)
                .Select(result => result.FirstTokenDelay!.Value.TotalSeconds)
                .DefaultIfEmpty(0)
                .Average();

            if (averageSpeed < 5 || averageFirstToken > 10)
            {
                return FastPerformanceProfile;
            }

            if (averageSpeed < 9 || averageFirstToken > 6)
            {
                return BalancedPerformanceProfile;
            }

            if (averageSpeed < 14 || averageFirstToken > 4)
            {
                return QualityPerformanceProfile;
            }

            return MaxDetailPerformanceProfile;
        }

        private static string BuildBenchmarkReport(
            IReadOnlyCollection<BenchmarkRunResult> results,
            string recommendedProfile,
            bool isFinal)
        {
            if (results.Count == 0)
            {
                return "Бенчмарк ещё не дал результатов.";
            }

            var builder = new StringBuilder();
            builder.Append(isFinal ? "Бенчмарк завершён. " : "Промежуточный бенчмарк. ");

            foreach (var result in results)
            {
                var firstTokenText = result.FirstTokenDelay.HasValue
                    ? $"{result.FirstTokenDelay.Value.TotalSeconds:F1} с"
                    : "нет";
                var ragText = result.RagSourceCount > 0
                    ? $", RAG источников {result.RagSourceCount}"
                    : string.Empty;

                builder.Append(
                    $"{result.Name}: первый токен {firstTokenText}, ~{result.TokensPerSecond:F1} ток/с, " +
                    $"всего {result.TotalElapsed.TotalSeconds:F1} с, prompt {result.PromptChars:N0} симв.{ragText}. ");
            }

            if (isFinal && !string.IsNullOrWhiteSpace(recommendedProfile))
            {
                builder.Append($"Автоподбор: профиль «{recommendedProfile}» применён. ");
                builder.Append("Для применения context/batch/micro batch к уже запущенной модели нужен перезапуск модели.");
            }

            return builder.ToString().Trim();
        }

        private sealed class BenchmarkScenario
        {
            public string Name { get; set; } = string.Empty;

            public List<LocalLlamaChatMessage> Messages { get; set; } = [];

            public int MaxTokens { get; set; }

            public int RagSourceCount { get; set; }
        }

        private sealed class BenchmarkRunResult
        {
            public string Name { get; set; } = string.Empty;

            public int PromptChars { get; set; }

            public int MaxTokens { get; set; }

            public int OutputChars { get; set; }

            public int OutputTokens { get; set; }

            public int Chunks { get; set; }

            public TimeSpan? FirstTokenDelay { get; set; }

            public TimeSpan TotalElapsed { get; set; }

            public double TokensPerSecond { get; set; }

            public int RagSourceCount { get; set; }
        }

        private void RefreshRagStatus()
        {
            RagStatusText = InfrastructureOrchestrator.RagStatusText;
            IsRagReady = InfrastructureOrchestrator.IsRagReady;
            RagDocumentCount = InfrastructureOrchestrator.RagDocumentCount;
        }

        private async Task SendMessageAsync()
        {
            if (IsSending || string.IsNullOrWhiteSpace(UserInput))
            {
                return;
            }

            var userMessage = UserInput.Trim();
            UserInput = string.Empty;
            IsSending = true;
            CanCancelGeneration = true;
            GenerationStatusText = "Подготовка запроса...";
            GenerationDiagnosticsText = string.Empty;

            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.User,
                Text = userMessage
            });

            _typingMessage = new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text = "🤔 Готовлю структурированный ответ...",
                IsTyping = true
            };
            Messages.Add(_typingMessage);

            _generationCts?.Dispose();
            _generationCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            var generationToken = _generationCts.Token;
            var answerBuilder = new StringBuilder();
            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var streamStopwatch = new System.Diagnostics.Stopwatch();
            TimeSpan? firstTokenDelay = null;
            var streamedChunksCount = 0;
            var promptChars = 0;
            var ragChars = 0;
            var historyMessagesUsed = 0;
            var modelReadyElapsed = TimeSpan.Zero;
            var ragElapsed = TimeSpan.Zero;
            var promptBuildElapsed = TimeSpan.Zero;
            var promptOptimizationText = "нет";
            var promptBudgetChars = 0;

            try
            {
                GenerationStatusText = "Проверяю, загружена ли модель...";
                var modelReadyStopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    await EnsureLocalLlamaLoadedFromSettingsAsync(generationToken);
                }
                finally
                {
                    modelReadyStopwatch.Stop();
                    modelReadyElapsed = modelReadyStopwatch.Elapsed;
                }

                var normalizedRagMode = NormalizeRagMode(RagMode);
                GenerationStatusText = normalizedRagMode == DisabledRagMode
                    ? "RAG отключен для этого ответа."
                    : "Подбираю контекст базы знаний...";
                var ragStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var ragContext = BuildRagContext(userMessage, normalizedRagMode);
                ragStopwatch.Stop();
                ragElapsed = ragStopwatch.Elapsed;
                var ragContextText = ragContext.ContextText;
                ragChars = ragContextText.Length;
                LastRagSourcesText = string.IsNullOrWhiteSpace(ragContext.SourcesText)
                    ? "Источники RAG: нет найденного контекста."
                    : ragContext.SourcesText;
                var effectiveMaxTokens = GetEffectiveMaxTokens();

                if (normalizedRagMode == OnlyRagMode && string.IsNullOrWhiteSpace(ragContextText))
                {
                    totalStopwatch.Stop();
                    GenerationStatusText = "В базе знаний не найден подходящий контекст.";
                    LastRequestSummaryText = BuildLastRequestSummary(
                        promptChars,
                        ragChars,
                        historyMessagesUsed,
                        effectiveMaxTokens,
                        promptBudgetChars,
                        promptOptimizationText,
                        ragContext.SourceCount);
                    GenerationDiagnosticsText = BuildGenerationDiagnostics(
                        totalStopwatch.Elapsed,
                        firstTokenDelay,
                        streamStopwatch.Elapsed,
                        promptChars,
                        ragChars,
                        historyMessagesUsed,
                        0,
                        streamedChunksCount,
                        ResponseMode,
                        effectiveMaxTokens,
                        modelReadyElapsed,
                        ragElapsed,
                        promptBuildElapsed,
                        promptBudgetChars,
                        promptOptimizationText,
                        normalizedRagMode);

                    if (_typingMessage != null)
                    {
                        _typingMessage.IsTyping = false;
                        _typingMessage.Text =
                            "В режиме «Только по базе» подходящий контекст не найден. " +
                            "Я не буду генерировать ответ без подтверждённых данных из базы знаний.";
                        _typingMessage = null;
                    }

                    await YieldToUiAsync();
                    return;
                }

                var promptBuildStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var promptPreparation = BuildPromptPreparation(
                    userMessage,
                    ragContextText,
                    ResponseMode,
                    normalizedRagMode,
                    effectiveMaxTokens);

                var chatMessages = promptPreparation.ChatMessages;
                ragContextText = promptPreparation.RagContextText;
                ragChars = ragContextText.Length;
                effectiveMaxTokens = promptPreparation.EffectiveMaxTokens;
                promptChars = promptPreparation.PromptChars;
                historyMessagesUsed = promptPreparation.HistoryMessagesUsed;
                promptOptimizationText = promptPreparation.OptimizationText;
                promptBudgetChars = promptPreparation.PromptBudgetChars;
                LastRequestSummaryText = BuildLastRequestSummary(
                    promptChars,
                    ragChars,
                    historyMessagesUsed,
                    effectiveMaxTokens,
                    promptBudgetChars,
                    promptOptimizationText,
                    ragContext.SourceCount);
                promptBuildStopwatch.Stop();
                promptBuildElapsed = promptBuildStopwatch.Elapsed;
                GenerationStatusText = BuildWaitingForFirstTokenStatus(
                    promptChars,
                    ragChars,
                    historyMessagesUsed,
                    ResponseMode,
                    effectiveMaxTokens,
                    modelReadyElapsed,
                    ragElapsed,
                    promptBuildElapsed,
                    promptBudgetChars,
                    promptOptimizationText,
                    normalizedRagMode);

                var hasReceivedContent = false;
                var lastUiUpdate = DateTime.UtcNow;
                var lastDiagnosticsUpdate = DateTime.UtcNow;
                streamStopwatch.Start();

                await foreach (var chunk in InfrastructureOrchestrator.LocalLlamaService.GenerateStreamAsync(
                                   chatMessages,
                                   effectiveMaxTokens,
                                   (float)Temperature,
                                   (float)TopP,
                                   (float)RepeatPenalty,
                                   generationToken))
                {
                    streamedChunksCount++;
                    answerBuilder.Append(chunk);

                    if (!hasReceivedContent)
                    {
                        firstTokenDelay = streamStopwatch.Elapsed;
                    }

                    var shouldUpdate =
                        !hasReceivedContent ||
                        (DateTime.UtcNow - lastUiUpdate).TotalMilliseconds >= 60 ||
                        chunk.Contains('\n') ||
                        chunk.Contains('\r');

                    hasReceivedContent = true;

                    if (_typingMessage == null || !shouldUpdate)
                    {
                        continue;
                    }

                    _typingMessage.IsTyping = false;
                    _typingMessage.Text = CleanAssistantText(answerBuilder.ToString());
                    lastUiUpdate = DateTime.UtcNow;

                    if ((DateTime.UtcNow - lastDiagnosticsUpdate).TotalMilliseconds >= 500)
                    {
                        GenerationStatusText = BuildStreamingStatus(
                            answerBuilder.Length,
                            firstTokenDelay,
                            streamStopwatch.Elapsed);
                        GenerationDiagnosticsText = BuildGenerationDiagnostics(
                            totalStopwatch.Elapsed,
                            firstTokenDelay,
                            streamStopwatch.Elapsed,
                            promptChars,
                            ragChars,
                            historyMessagesUsed,
                            answerBuilder.Length,
                            streamedChunksCount,
                            ResponseMode,
                            effectiveMaxTokens,
                            modelReadyElapsed,
                            ragElapsed,
                            promptBuildElapsed,
                            promptBudgetChars,
                            promptOptimizationText,
                            normalizedRagMode);
                        lastDiagnosticsUpdate = DateTime.UtcNow;
                    }

                    await YieldToUiAsync();
                }

                totalStopwatch.Stop();
                var assistantText = CleanAssistantText(answerBuilder.ToString());
                GenerationStatusText = "Ответ получен.";
                GenerationDiagnosticsText = BuildGenerationDiagnostics(
                    totalStopwatch.Elapsed,
                    firstTokenDelay,
                    streamStopwatch.Elapsed,
                    promptChars,
                    ragChars,
                    historyMessagesUsed,
                    assistantText.Length,
                    streamedChunksCount,
                    ResponseMode,
                    effectiveMaxTokens,
                    modelReadyElapsed,
                    ragElapsed,
                    promptBuildElapsed,
                    promptBudgetChars,
                    promptOptimizationText,
                    normalizedRagMode);

                if (_typingMessage != null && !string.IsNullOrWhiteSpace(assistantText))
                {
                    _typingMessage.IsTyping = false;
                    _typingMessage.Text = assistantText;
                    _typingMessage = null;
                    await YieldToUiAsync();
                }
                else
                {
                    RemoveTypingMessage();
                    Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = string.IsNullOrWhiteSpace(assistantText)
                        ? "⚠️ Модель вернула пустой финальный ответ. Останови модель и запусти её снова, чтобы применить параметры без thinking."
                        : assistantText
                });
                }
            }
            catch (OperationCanceledException)
            {
                totalStopwatch.Stop();
                var partialAnswer = CleanAssistantText(answerBuilder.ToString());
                GenerationStatusText = "Генерация остановлена.";
                GenerationDiagnosticsText = BuildGenerationDiagnostics(
                    totalStopwatch.Elapsed,
                    firstTokenDelay,
                    streamStopwatch.Elapsed,
                    promptChars,
                    ragChars,
                    historyMessagesUsed,
                    partialAnswer.Length,
                    streamedChunksCount,
                    ResponseMode,
                    GetEffectiveMaxTokens(),
                    modelReadyElapsed,
                    ragElapsed,
                    promptBuildElapsed,
                    promptBudgetChars,
                    promptOptimizationText,
                    NormalizeRagMode(RagMode));

                if (!string.IsNullOrWhiteSpace(partialAnswer))
                {
                    if (_typingMessage == null)
                    {
                        Messages.Add(new ChatMessage
                        {
                            Sender = MessageSender.Assistant,
                            Text = $"{partialAnswer}{Environment.NewLine}{Environment.NewLine}Генерация остановлена."
                        });
                    }
                    else
                    {
                        _typingMessage.IsTyping = false;
                        _typingMessage.Text = $"{partialAnswer}{Environment.NewLine}{Environment.NewLine}Генерация остановлена.";
                        _typingMessage = null;
                    }
                }
                else
                {
                RemoveTypingMessage();
                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = "⏹️ Генерация ответа отменена."
                });
                }
            }
            catch (Exception ex)
            {
                totalStopwatch.Stop();
                var partialAnswer = CleanAssistantText(answerBuilder.ToString());
                GenerationStatusText = "Генерация прервалась ошибкой.";
                GenerationDiagnosticsText = BuildGenerationDiagnostics(
                    totalStopwatch.Elapsed,
                    firstTokenDelay,
                    streamStopwatch.Elapsed,
                    promptChars,
                    ragChars,
                    historyMessagesUsed,
                    partialAnswer.Length,
                    streamedChunksCount,
                    ResponseMode,
                    GetEffectiveMaxTokens(),
                    modelReadyElapsed,
                    ragElapsed,
                    promptBuildElapsed,
                    promptBudgetChars,
                    promptOptimizationText,
                    NormalizeRagMode(RagMode));

                if (!string.IsNullOrWhiteSpace(partialAnswer) && _typingMessage != null)
                {
                    _typingMessage.IsTyping = false;
                    _typingMessage.Text = $"{partialAnswer}{Environment.NewLine}{Environment.NewLine}Поток ответа прервался: {ex.Message}";
                    _typingMessage = null;
                }
                else
                {
                RemoveTypingMessage();
                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = $"💥 Исключение: {ex.Message}"
                });
                }
            }
            finally
            {
                IsSending = false;
                CanCancelGeneration = false;
                _generationCts?.Dispose();
                _generationCts = null;
            }
        }

        private async Task EnsureLocalLlamaLoadedFromSettingsAsync(CancellationToken ct)
        {
            if (InfrastructureOrchestrator.LocalLlamaService.IsLoaded)
            {
                if (!InfrastructureOrchestrator.LocalLlamaService.IsWarmedUp)
                {
                    await InfrastructureOrchestrator.LocalLlamaService.WarmUpAsync(
                        Math.Clamp(WarmUpMaxTokens <= 0 ? 3 : WarmUpMaxTokens, 1, 3),
                        ct);
                }

                IsServerAvailable = true;
                ServerStatusText =
                    $"🟢 GGUF-модель загружена; warm-up {FormatDuration(InfrastructureOrchestrator.LocalLlamaService.LastWarmUpElapsed)}";
                return;
            }

            ServerStatusText = "🟡 Загружаю GGUF-модель...";
            IsServerAvailable = false;
            IsModelLoading = true;

            try
            {
            if (!string.IsNullOrWhiteSpace(ModelPath) && File.Exists(ModelPath))
            {
                await InfrastructureOrchestrator.LocalLlamaService.LoadAsync(
                    ModelPath,
                    ContextSize,
                    GpuLayerCount,
                    ThreadCount,
                    BatchSize,
                    MicroBatchSize,
                    Math.Clamp(WarmUpMaxTokens <= 0 ? 3 : WarmUpMaxTokens, 1, 3),
                    LlamaServerPath,
                    ct);
            }
            else
            {
                await InfrastructureOrchestrator.EnsureLocalLlamaLoadedAsync(
                    ContextSize,
                    GpuLayerCount,
                    ThreadCount,
                    BatchSize,
                    MicroBatchSize,
                    Math.Clamp(WarmUpMaxTokens <= 0 ? 3 : WarmUpMaxTokens, 1, 3),
                    ct);
            }

            IsServerAvailable = true;
            ServerStatusText =
                $"🟢 GGUF-модель загружена; warm-up {FormatDuration(InfrastructureOrchestrator.LocalLlamaService.LastWarmUpElapsed)}";
            ModelLogPath = InfrastructureOrchestrator.LocalLlamaService.CurrentLogPath;
            }
            finally
            {
                IsModelLoading = false;
            }
        }

        private static async Task YieldToUiAsync()
        {
            await Dispatcher.Yield(DispatcherPriority.Background);
        }

        private static string BuildWaitingForFirstTokenStatus(
            int promptChars,
            int ragChars,
            int historyMessagesUsed,
            string responseMode,
            int maxTokens,
            TimeSpan modelReadyElapsed,
            TimeSpan ragElapsed,
            TimeSpan promptBuildElapsed,
            int promptBudgetChars,
            string promptOptimizationText,
            string ragMode)
        {
            return
                $"RAG режим: {NormalizeRagMode(ragMode)}. " +
                $"Режим: {NormalizeResponseMode(responseMode)}; max tokens: {maxTokens}. " +
                $"Подготовка: модель {FormatDuration(modelReadyElapsed)}, RAG {FormatDuration(ragElapsed)}, prompt {FormatDuration(promptBuildElapsed)}. " +
                $"Обрабатываю prompt: {promptChars:N0} симв. (~{EstimateTokenCountForDisplay(promptChars):N0} ток.); " +
                $"RAG: {ragChars:N0} симв.; история: {historyMessagesUsed}; бюджет: {promptBudgetChars:N0} симв.; " +
                $"автоограничения: {NormalizePromptOptimizationText(promptOptimizationText)}. Жду первый токен...";
        }

        private static string BuildStreamingStatus(
            int outputChars,
            TimeSpan? firstTokenDelay,
            TimeSpan streamElapsed)
        {
            var approximateTokens = EstimateTokenCount(outputChars);
            var effectiveSeconds = GetEffectiveGenerationSeconds(firstTokenDelay, streamElapsed);
            var tokensPerSecond = approximateTokens / effectiveSeconds;

            return
                $"Получаю ответ потоком: ~{tokensPerSecond:F1} ток/с, {outputChars:N0} симв.";
        }

        private static string BuildGenerationDiagnostics(
            TimeSpan totalElapsed,
            TimeSpan? firstTokenDelay,
            TimeSpan streamElapsed,
            int promptChars,
            int ragChars,
            int historyMessagesUsed,
            int outputChars,
            int streamedChunksCount,
            string responseMode,
            int maxTokens,
            TimeSpan modelReadyElapsed,
            TimeSpan ragElapsed,
            TimeSpan promptBuildElapsed,
            int promptBudgetChars,
            string promptOptimizationText,
            string ragMode)
        {
            var approximateTokens = EstimateTokenCount(outputChars);
            var effectiveSeconds = GetEffectiveGenerationSeconds(firstTokenDelay, streamElapsed);
            var tokensPerSecond = outputChars > 0
                ? approximateTokens / effectiveSeconds
                : 0;

            var firstTokenText = firstTokenDelay.HasValue
                ? $"{firstTokenDelay.Value.TotalSeconds:F1} с"
                : "нет";

            return
                $"Этапы: модель {FormatDuration(modelReadyElapsed)}, RAG {FormatDuration(ragElapsed)}, " +
                $"prompt {FormatDuration(promptBuildElapsed)}, поток {FormatDuration(streamElapsed)}. " +
                $"Размеры: prompt {promptChars:N0} симв. (~{EstimateTokenCountForDisplay(promptChars):N0} ток.), " +
                $"RAG {ragChars:N0} симв. (~{EstimateTokenCountForDisplay(ragChars):N0} ток.). " +
                $"Бюджет prompt: {promptBudgetChars:N0} симв.; автоограничения: {NormalizePromptOptimizationText(promptOptimizationText)}. " +
                $"Предупреждения: {BuildGenerationWarnings(modelReadyElapsed, ragElapsed, firstTokenDelay, streamElapsed, promptChars, ragChars, historyMessagesUsed, outputChars, responseMode, maxTokens, ragMode)} " +
                $"RAG режим: {NormalizeRagMode(ragMode)}. " +
                $"Режим: {NormalizeResponseMode(responseMode)}; max tokens: {maxTokens}; " +
                $"первый токен: {firstTokenText}; всего: {totalElapsed.TotalSeconds:F1} с; " +
                $"скорость: ~{tokensPerSecond:F1} ток/с; prompt: {promptChars:N0} симв.; " +
                $"RAG: {ragChars:N0}; история: {historyMessagesUsed}; ответ: {outputChars:N0} симв.; чанков: {streamedChunksCount}.";
        }

        private string BuildLastRequestSummary(
            int promptChars,
            int ragChars,
            int historyMessagesUsed,
            int maxTokens,
            int promptBudgetChars,
            string promptOptimizationText,
            int ragSourceCount)
        {
            var systemPromptMode = UseCompactSystemPrompt
                ? "компактный"
                : "полный";

            return
                $"Последний запрос: профиль {NormalizePerformanceProfile(PerformanceProfile)}; " +
                $"ответ {NormalizeResponseMode(ResponseMode)}; RAG {NormalizeRagMode(RagMode)}; " +
                $"system prompt: {systemPromptMode}; prompt {promptChars:N0}/{promptBudgetChars:N0} симв.; " +
                $"RAG {ragChars:N0} симв., источников {ragSourceCount}; история {historyMessagesUsed}; " +
                $"max tokens {maxTokens}; автоограничения: {NormalizePromptOptimizationText(promptOptimizationText)}.";
        }

        private static string BuildGenerationWarnings(
            TimeSpan modelReadyElapsed,
            TimeSpan ragElapsed,
            TimeSpan? firstTokenDelay,
            TimeSpan streamElapsed,
            int promptChars,
            int ragChars,
            int historyMessagesUsed,
            int outputChars,
            string responseMode,
            int maxTokens,
            string ragMode)
        {
            var warnings = new List<string>();
            var normalizedResponseMode = NormalizeResponseMode(responseMode);
            var normalizedRagMode = NormalizeRagMode(ragMode);

            if (modelReadyElapsed.TotalSeconds >= 5)
            {
                warnings.Add("модель догружалась перед ответом, следующие запросы должны стартовать быстрее;");
            }

            if (normalizedRagMode != DisabledRagMode && ragElapsed.TotalSeconds >= 2)
            {
                warnings.Add("RAG-поиск занял заметное время, стоит уменьшить TopK или объём базы для ответа;");
            }

            if (ragChars >= 4500)
            {
                warnings.Add("RAG-контекст крупный, он увеличивает время первого токена;");
            }

            if (promptChars >= 12000)
            {
                warnings.Add("prompt большой, лучше уменьшить историю, RAG-контекст или context size;");
            }

            if (historyMessagesUsed >= 8)
            {
                warnings.Add("в запрос попало много истории, это замедляет prefill;");
            }

            if (maxTokens >= 1600 && normalizedResponseMode != FastResponseMode)
            {
                warnings.Add("max tokens высокий, подробный ответ может генерироваться долго;");
            }

            if (firstTokenDelay.HasValue && firstTokenDelay.Value.TotalSeconds >= 8)
            {
                warnings.Add("первый токен пришёл медленно, узкое место похоже на prefill prompt;");
            }

            var outputTokens = EstimateTokenCountForDisplay(outputChars);
            var generationSeconds = GetEffectiveGenerationSeconds(firstTokenDelay, streamElapsed);
            var tokensPerSecond = outputTokens > 0
                ? outputTokens / generationSeconds
                : 0;

            if (outputTokens >= 80 && tokensPerSecond > 0 && tokensPerSecond < 5)
            {
                warnings.Add("скорость генерации низкая, стоит проверить GPU layers, batch и число потоков;");
            }

            return warnings.Count == 0
                ? "нет."
                : string.Join(" ", warnings);
        }

        private static string FormatDuration(TimeSpan elapsed)
        {
            return elapsed.TotalSeconds >= 1
                ? $"{elapsed.TotalSeconds:F1} с"
                : $"{elapsed.TotalMilliseconds:F0} мс";
        }

        private static int EstimateTokenCountForDisplay(int textChars)
        {
            return textChars <= 0
                ? 0
                : EstimateTokenCount(textChars);
        }

        private static int EstimateTokenCount(int textChars)
        {
            return Math.Max(1, (int)Math.Ceiling(textChars / 4.0));
        }

        private static double GetEffectiveGenerationSeconds(
            TimeSpan? firstTokenDelay,
            TimeSpan streamElapsed)
        {
            var afterFirstTokenSeconds = firstTokenDelay.HasValue
                ? (streamElapsed - firstTokenDelay.Value).TotalSeconds
                : streamElapsed.TotalSeconds;

            return Math.Max(0.1, afterFirstTokenSeconds);
        }

        private PromptPreparationResult BuildPromptPreparation(
            string userMessage,
            string ragContextText,
            string responseMode,
            string ragMode,
            int effectiveMaxTokens)
        {
            var optimizationActions = new List<string>();
            var optimizedRagContextText = ragContextText;
            var optimizedMaxTokens = effectiveMaxTokens;
            var promptBudgetChars = GetPromptBudgetChars(optimizedMaxTokens);
            var onlyCurrentQuestion =
                NormalizeRagMode(ragMode) == OnlyRagMode ||
                !string.IsNullOrWhiteSpace(optimizedRagContextText) ||
                ShouldUseOnlyCurrentQuestionForRag(userMessage);

            var chatMessages = BuildLocalLlamaMessages(
                userMessage,
                optimizedRagContextText,
                onlyCurrentQuestion,
                responseMode,
                ragMode,
                UseCompactSystemPrompt);
            var promptChars = chatMessages.Sum(message => message.Content.Length);

            if (promptChars > promptBudgetChars && !onlyCurrentQuestion)
            {
                onlyCurrentQuestion = true;
                optimizationActions.Add("история отключена из-за большого prompt");

                chatMessages = BuildLocalLlamaMessages(
                    userMessage,
                    optimizedRagContextText,
                    onlyCurrentQuestion,
                    responseMode,
                    ragMode,
                    UseCompactSystemPrompt);
                promptChars = chatMessages.Sum(message => message.Content.Length);
            }

            if (promptChars > promptBudgetChars && !string.IsNullOrWhiteSpace(optimizedRagContextText))
            {
                var excessChars = promptChars - promptBudgetChars;
                var minRagChars = Math.Min(1000, optimizedRagContextText.Length);
                var targetRagChars = Math.Clamp(
                    optimizedRagContextText.Length - excessChars - 500,
                    minRagChars,
                    optimizedRagContextText.Length);

                if (targetRagChars < optimizedRagContextText.Length)
                {
                    optimizedRagContextText = LimitTextByChars(optimizedRagContextText, targetRagChars);
                    optimizationActions.Add($"RAG-контекст сокращён до {targetRagChars:N0} симв.");

                    chatMessages = BuildLocalLlamaMessages(
                        userMessage,
                        optimizedRagContextText,
                        onlyCurrentQuestion,
                        responseMode,
                        ragMode,
                        UseCompactSystemPrompt);
                    promptChars = chatMessages.Sum(message => message.Content.Length);
                }
            }

            if (promptChars > promptBudgetChars &&
                NormalizeResponseMode(responseMode) == FastResponseMode &&
                optimizedMaxTokens > 450)
            {
                optimizedMaxTokens = Math.Clamp(Math.Min(optimizedMaxTokens, 450), 180, 600);
                promptBudgetChars = GetPromptBudgetChars(optimizedMaxTokens);
                optimizationActions.Add($"max tokens снижен до {optimizedMaxTokens} для быстрого ответа");
            }

            return new PromptPreparationResult
            {
                ChatMessages = chatMessages,
                RagContextText = optimizedRagContextText,
                EffectiveMaxTokens = optimizedMaxTokens,
                PromptChars = promptChars,
                HistoryMessagesUsed = Math.Max(0, chatMessages.Count - 1),
                PromptBudgetChars = promptBudgetChars,
                OptimizationText = optimizationActions.Count == 0
                    ? "нет"
                    : string.Join("; ", optimizationActions)
            };
        }

        private int GetPromptBudgetChars(int maxTokens)
        {
            var contextTokens = ContextSize <= 0 ? 4096 : ContextSize;
            var contextChars = Math.Clamp(contextTokens * 3, 6000, 36000);
            var outputReserveChars = Math.Clamp(maxTokens <= 0 ? 1100 : maxTokens, 180, 2400) * 4;

            return Math.Clamp(contextChars - outputReserveChars - 1800, 2500, 24000);
        }

        private static string NormalizePromptOptimizationText(string? optimizationText)
        {
            return string.IsNullOrWhiteSpace(optimizationText)
                ? "нет"
                : optimizationText.Trim();
        }

        private sealed class PromptPreparationResult
        {
            public List<LocalLlamaChatMessage> ChatMessages { get; set; } = [];

            public string RagContextText { get; set; } = string.Empty;

            public int EffectiveMaxTokens { get; set; }

            public int PromptChars { get; set; }

            public int HistoryMessagesUsed { get; set; }

            public int PromptBudgetChars { get; set; }

            public string OptimizationText { get; set; } = "нет";
        }

        private RagContextBuildResult BuildRagContext(string userMessage, string ragMode)
        {
            if (NormalizeRagMode(ragMode) == DisabledRagMode)
            {
                return new RagContextBuildResult();
            }

            if (!InfrastructureOrchestrator.IsRagReady ||
                InfrastructureOrchestrator.RagDocumentCount <= 0)
            {
                return new RagContextBuildResult();
            }

            var generationRagConfig = CreateGenerationRagConfig(_ragConfig);
            var ragAnswerContext = InfrastructureOrchestrator.RagIndexService
                .BuildAnswerContext(userMessage, generationRagConfig);

            return ragAnswerContext.HasResults
                ? new RagContextBuildResult
                {
                    ContextText = LimitTextByChars(ragAnswerContext.ContextText, generationRagConfig.MaxTotalContextChars),
                    SourcesText = BuildRagSourcesText(ragAnswerContext.Results, generationRagConfig),
                    SourceCount = Math.Min(ragAnswerContext.Results.Count, generationRagConfig.TopK)
                }
                : new RagContextBuildResult();
        }

        private static string BuildRagSourcesText(
            IEnumerable<RagSearchResult> results,
            RagConfig config)
        {
            var selectedResults = results?
                .Where(result => result?.Document != null)
                .Take(Math.Clamp(config.TopK <= 0 ? 4 : config.TopK, 1, 8))
                .ToList() ?? [];

            if (selectedResults.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Источники RAG:");

            var index = 1;
            foreach (var result in selectedResults)
            {
                var document = result.Document;
                var title = string.IsNullOrWhiteSpace(document.Title)
                    ? document.Type
                    : document.Title;
                var idPart = string.IsNullOrWhiteSpace(document.Id)
                    ? string.Empty
                    : $", ID: {document.Id}";
                var pagePart = document.Page > 0
                    ? $", стр. {document.Page}"
                    : string.Empty;

                builder.AppendLine(
                    $"[{index}] {document.Source} / {title}{idPart}{pagePart}; score {result.Score:0.##}");
                index++;
            }

            return builder.ToString().Trim();
        }

        private sealed class RagContextBuildResult
        {
            public string ContextText { get; set; } = string.Empty;

            public string SourcesText { get; set; } = string.Empty;

            public int SourceCount { get; set; }
        }

        private RagConfig CreateGenerationRagConfig(RagConfig config)
        {
            var profileSettings = GetRecommendedSettings(PerformanceProfile);
            var configuredMaxTotalContextChars = config.MaxTotalContextChars <= 0
                ? profileSettings.RagMaxTotalContextChars
                : config.MaxTotalContextChars;
            var contextBudgetChars = Math.Clamp(ContextSize <= 0 ? 12000 : ContextSize * 3, 6000, 24000);
            var historyReserveChars = Math.Clamp((HistoryMessagesCount <= 0 ? 6 : HistoryMessagesCount) * 700, 1200, 6000);
            var maxAllowedRagChars = Math.Clamp(contextBudgetChars - historyReserveChars - 2500, 1500, 6000);
            var safeMaxTotalContextChars = Math.Min(
                Math.Min(configuredMaxTotalContextChars, profileSettings.RagMaxTotalContextChars),
                maxAllowedRagChars);
            var maxTopK = NormalizePerformanceProfile(PerformanceProfile) switch
            {
                FastPerformanceProfile => 3,
                BalancedPerformanceProfile => 4,
                _ => 5
            };

            return new RagConfig
            {
                IsEnabled = true,
                TopK = Math.Clamp(config.TopK <= 0 ? profileSettings.RagTopK : config.TopK, 1, safeMaxTotalContextChars < 2500 ? 3 : maxTopK),
                MaxCharsPerDocument = Math.Clamp(config.MaxCharsPerDocument <= 0 ? profileSettings.RagMaxCharsPerDocument : config.MaxCharsPerDocument, 500, profileSettings.RagMaxCharsPerDocument),
                MaxTotalContextChars = safeMaxTotalContextChars,
                MinScore = config.MinScore <= 0 ? 1.0 : config.MinScore,
                KnowledgeBasePath = config.KnowledgeBasePath,
                ProjectDatabasePath = config.ProjectDatabasePath,
                UseProjectDatabases = config.UseProjectDatabases,
                UseManuals = config.UseManuals,
                UseKasperskyManuals = config.UseKasperskyManuals,
                UseSecretNetStudioManuals = config.UseSecretNetStudioManuals,
                UseDrWebManuals = config.UseDrWebManuals,
                UseThreats = config.UseThreats,
                UseVulnerabilities = config.UseVulnerabilities,
                UseViolators = config.UseViolators,
                UseProtectionMeasures = config.UseProtectionMeasures,
                UseTechniquesAndTactics = config.UseTechniquesAndTactics,
                UseExploits = config.UseExploits,
                UseOutcomes = config.UseOutcomes
            };
        }

        private List<LocalLlamaChatMessage> BuildLocalLlamaMessages(
            string currentUserMessage,
            string ragContextText,
            bool onlyCurrentQuestion,
            string responseMode,
            string ragMode,
            bool useCompactSystemPrompt)
        {
            var messages = new List<LocalLlamaChatMessage>
            {
                new("system", BuildSystemMessage(SystemPrompt, ragContextText, responseMode, ragMode, useCompactSystemPrompt))
            };

            var conversationMessages = BuildConversationMessages(
                currentUserMessage,
                onlyCurrentQuestion,
                ragContextText.Length,
                responseMode);

            messages.AddRange(conversationMessages.Select(message =>
                new LocalLlamaChatMessage(
                    message.Sender == MessageSender.User ? "user" : "assistant",
                    message.Text)));

            return messages;
        }

        private List<ChatMessage> BuildConversationMessages(
            string currentUserMessage,
            bool onlyCurrentQuestion,
            int ragContextChars,
            string responseMode)
        {
            if (onlyCurrentQuestion)
            {
                return
                [
                    new ChatMessage
                    {
                        Sender = MessageSender.User,
                        Text = currentUserMessage
                    }
                ];
            }

            var historyLimit = GetAdaptiveHistoryLimit(ragContextChars, responseMode);

            return Messages
                .Where(message =>
                    !message.IsTyping &&
                    !string.IsNullOrWhiteSpace(message.Text) &&
                    !IsSystemChatMessage(message.Text))
                .TakeLast(historyLimit)
                .ToList();
        }

        private int GetAdaptiveHistoryLimit(int ragContextChars, string responseMode)
        {
            var configuredLimit = Math.Clamp(
                HistoryMessagesCount <= 0 ? 6 : HistoryMessagesCount,
                1,
                12);

            configuredLimit = NormalizeResponseMode(responseMode) switch
            {
                FastResponseMode => Math.Min(configuredLimit, 3),
                ExpertResponseMode => Math.Min(Math.Max(configuredLimit, 8), 12),
                _ => configuredLimit
            };

            if (ragContextChars >= 4500)
            {
                return Math.Min(configuredLimit, 2);
            }

            if (ragContextChars >= 3000)
            {
                return Math.Min(configuredLimit, 3);
            }

            if (ragContextChars >= 1500)
            {
                return Math.Min(configuredLimit, 4);
            }

            return configuredLimit;
        }

        private int GetEffectiveMaxTokens()
        {
            var configuredMaxTokens = MaxTokens <= 0 ? 1100 : MaxTokens;

            return NormalizePerformanceProfile(PerformanceProfile) switch
            {
                FastPerformanceProfile => Math.Clamp(Math.Min(configuredMaxTokens, 650), 180, 900),
                QualityPerformanceProfile => Math.Clamp(Math.Max(configuredMaxTokens, 1300), 700, 1800),
                MaxDetailPerformanceProfile => Math.Clamp(Math.Max(configuredMaxTokens, 1800), 900, 2400),
                _ => Math.Clamp(configuredMaxTokens, 300, 1300)
            };
        }

        private void ApplyRecommendedSettings()
        {
            var settings = GetRecommendedSettings(PerformanceProfile);

            PerformanceProfile = settings.Profile;
            ResponseMode = settings.ResponseMode;
            RagMode = AutoRagMode;
            ContextSize = settings.ContextSize;
            MaxTokens = settings.MaxTokens;
            Temperature = settings.Temperature;
            TopP = settings.TopP;
            RepeatPenalty = settings.RepeatPenalty;
            HistoryMessagesCount = settings.HistoryMessagesCount;
            BatchSize = settings.BatchSize;
            MicroBatchSize = settings.MicroBatchSize;
            WarmUpMaxTokens = 3;

            _ragConfig.IsEnabled = true;
            _ragConfig.TopK = settings.RagTopK;
            _ragConfig.MaxTotalContextChars = settings.RagMaxTotalContextChars;
            _ragConfig.MaxCharsPerDocument = settings.RagMaxCharsPerDocument;
        }

        private static RecommendedModelSettings GetRecommendedSettings(string? profile)
        {
            return NormalizePerformanceProfile(profile) switch
            {
                FastPerformanceProfile => new RecommendedModelSettings
                {
                    Profile = FastPerformanceProfile,
                    ResponseMode = FastResponseMode,
                    ContextSize = 4096,
                    MaxTokens = 650,
                    Temperature = 0.2,
                    TopP = 0.85,
                    RepeatPenalty = 1.07,
                    HistoryMessagesCount = 3,
                    BatchSize = 512,
                    MicroBatchSize = 256,
                    RagTopK = 3,
                    RagMaxTotalContextChars = 2500,
                    RagMaxCharsPerDocument = 900
                },
                QualityPerformanceProfile => new RecommendedModelSettings
                {
                    Profile = QualityPerformanceProfile,
                    ResponseMode = ExpertResponseMode,
                    ContextSize = 8192,
                    MaxTokens = 1600,
                    Temperature = 0.3,
                    TopP = 0.92,
                    RepeatPenalty = 1.08,
                    HistoryMessagesCount = 7,
                    BatchSize = 512,
                    MicroBatchSize = 512,
                    RagTopK = 5,
                    RagMaxTotalContextChars = 5500,
                    RagMaxCharsPerDocument = 1500
                },
                MaxDetailPerformanceProfile => new RecommendedModelSettings
                {
                    Profile = MaxDetailPerformanceProfile,
                    ResponseMode = ExpertResponseMode,
                    ContextSize = 12288,
                    MaxTokens = 2200,
                    Temperature = 0.32,
                    TopP = 0.92,
                    RepeatPenalty = 1.08,
                    HistoryMessagesCount = 8,
                    BatchSize = 512,
                    MicroBatchSize = 512,
                    RagTopK = 5,
                    RagMaxTotalContextChars = 6500,
                    RagMaxCharsPerDocument = 1800
                },
                _ => new RecommendedModelSettings
                {
                    Profile = BalancedPerformanceProfile,
                    ResponseMode = DetailedResponseMode,
                    ContextSize = 6144,
                    MaxTokens = 1100,
                    Temperature = 0.28,
                    TopP = 0.9,
                    RepeatPenalty = 1.08,
                    HistoryMessagesCount = 5,
                    BatchSize = 512,
                    MicroBatchSize = 512,
                    RagTopK = 4,
                    RagMaxTotalContextChars = 4000,
                    RagMaxCharsPerDocument = 1200
                }
            };
        }

        private sealed class RecommendedModelSettings
        {
            public string Profile { get; set; } = BalancedPerformanceProfile;

            public string ResponseMode { get; set; } = DetailedResponseMode;

            public int ContextSize { get; set; }

            public int MaxTokens { get; set; }

            public double Temperature { get; set; }

            public double TopP { get; set; }

            public double RepeatPenalty { get; set; }

            public int HistoryMessagesCount { get; set; }

            public int BatchSize { get; set; }

            public int MicroBatchSize { get; set; }

            public int RagTopK { get; set; }

            public int RagMaxTotalContextChars { get; set; }

            public int RagMaxCharsPerDocument { get; set; }
        }

        private static string NormalizePerformanceProfile(string? profile)
        {
            if (string.Equals(profile, FastPerformanceProfile, StringComparison.OrdinalIgnoreCase))
            {
                return FastPerformanceProfile;
            }

            if (string.Equals(profile, QualityPerformanceProfile, StringComparison.OrdinalIgnoreCase))
            {
                return QualityPerformanceProfile;
            }

            if (string.Equals(profile, MaxDetailPerformanceProfile, StringComparison.OrdinalIgnoreCase))
            {
                return MaxDetailPerformanceProfile;
            }

            return BalancedPerformanceProfile;
        }

        private static string NormalizeResponseMode(string? responseMode)
        {
            if (string.Equals(responseMode, FastResponseMode, StringComparison.OrdinalIgnoreCase))
            {
                return FastResponseMode;
            }

            if (string.Equals(responseMode, ExpertResponseMode, StringComparison.OrdinalIgnoreCase))
            {
                return ExpertResponseMode;
            }

            return DetailedResponseMode;
        }

        private static string NormalizeRagMode(string? ragMode)
        {
            if (string.Equals(ragMode, DisabledRagMode, StringComparison.OrdinalIgnoreCase))
            {
                return DisabledRagMode;
            }

            if (string.Equals(ragMode, OnlyRagMode, StringComparison.OrdinalIgnoreCase))
            {
                return OnlyRagMode;
            }

            return AutoRagMode;
        }

        private static string BuildResponseModeInstruction(string responseMode)
        {
            return NormalizeResponseMode(responseMode) switch
            {
                FastResponseMode =>
                    "Режим ответа: быстрый. Ответь кратко и прикладно, без длинных вступлений и без повторения вопроса. " +
                    "Если вопрос про компьютерный инцидент, используй формат: краткий вывод, вероятный тип, что сделать срочно, что проверить, риск. " +
                    "Не добавляй отдельный блок \"Факты из базы знаний\"; используй только релевантные сведения внутри нужных пунктов. " +
                    "Не называй конкретные семейства ВПО, инструменты, CVE/BDU и версии, если они прямо не указаны или не подтверждены артефактами.",
                ExpertResponseMode =>
                    "Режим ответа: экспертный. Дай развёрнутый технический ответ: вывод, факты, анализ, риски, рекомендации и ограничения. Не экономь на важных деталях.",
                _ =>
                    "Режим ответа: подробный. Дай структурированный ответ средней глубины: вывод, пояснение, рекомендации и ограничения."
            };
        }

        private static string BuildRagModeInstruction(string ragMode)
        {
            return NormalizeRagMode(ragMode) switch
            {
                DisabledRagMode =>
                    "RAG-режим: база знаний отключена. Отвечай только на основе вопроса и истории диалога, явно отмечай неопределённость.",
                OnlyRagMode =>
                    "RAG-режим: только по базе знаний. Используй только предоставленный контекст. Если в контексте нет точных данных, прямо напиши, что в базе знаний нет достаточной информации.",
                _ =>
                    "RAG-режим: авто. Если контекст базы знаний предоставлен, используй его как основной источник фактов; если контекста нет, отвечай как общий технический ассистент."
            };
        }

        private static string BuildCompactSystemMessage(
            string systemPrompt,
            string ragContextText,
            string responseMode,
            string ragMode)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Ты ассистент PragmaticAnalyzer. Отвечай на русском, структурированно и по делу.");
            builder.AppendLine("Не показывай chain-of-thought, reasoning_content, теги <think> и служебные токены.");
            builder.AppendLine("Не выдумывай ID, CVE, BDU, версии, пути, команды, настройки, имена файлов, названия ВПО и факты.");
            builder.AppendLine(BuildResponseModeInstruction(responseMode));
            builder.AppendLine(BuildRagModeInstruction(ragMode));
            builder.AppendLine("Для технических ответов используй: 1) краткий вывод; 2) что известно; 3) анализ; 4) действия/рекомендации; 5) ограничения.");

            var normalizedPrompt = NormalizeSystemPrompt(systemPrompt);

            if (!string.IsNullOrWhiteSpace(normalizedPrompt))
            {
                builder.AppendLine();
                builder.AppendLine("Инструкции проекта:");
                builder.AppendLine(LimitTextByChars(normalizedPrompt.Trim(), 900));
            }

            if (!string.IsNullOrWhiteSpace(ragContextText))
            {
                builder.AppendLine();
                builder.AppendLine("Контекст базы знаний:");
                builder.AppendLine(ragContextText.Trim());
                builder.AppendLine();
                builder.AppendLine("Используй только релевантные факты из контекста и не пересказывай нерелевантные определения. Если точного фрагмента нет, прямо скажи об этом. Не создавай отдельный раздел \"Факты из базы знаний\", если пользователь сам его не попросил.");
            }

            return builder.ToString().Trim();
        }

        private static string BuildSystemMessage(
            string systemPrompt,
            string ragContextText,
            string responseMode,
            string ragMode,
            bool useCompactSystemPrompt)
        {
            var builder = new StringBuilder();

            if (useCompactSystemPrompt)
            {
                return BuildCompactSystemMessage(
                    systemPrompt,
                    ragContextText,
                    responseMode,
                    ragMode);
            }

            builder.AppendLine("Ты ассистент программы PragmaticAnalyzer.");
            builder.AppendLine("Всегда отвечай на русском языке.");
            builder.AppendLine("Не показывай chain-of-thought, reasoning_content, теги <think> и служебные токены.");
            builder.AppendLine(BuildResponseModeInstruction(responseMode));
            builder.AppendLine(BuildRagModeInstruction(ragMode));
            builder.AppendLine("Для содержательных технических вопросов используй структуру:");
            builder.AppendLine("1. Краткий вывод.");
            builder.AppendLine("2. Что известно из вопроса и релевантного контекста.");
            builder.AppendLine("3. Анализ.");
            builder.AppendLine("4. Рекомендации или дальнейшие действия.");
            builder.AppendLine("5. Ограничения ответа, если данных недостаточно.");
            builder.AppendLine("Если вопрос простой, отвечай короче, но сохраняй ясность.");
            builder.AppendLine("Не выдумывай ID, CVE, BDU, версии, настройки, пути, команды, имена файлов, названия ВПО и факты.");
            builder.AppendLine();

            var normalizedPrompt = NormalizeSystemPrompt(systemPrompt);

            if (!string.IsNullOrWhiteSpace(normalizedPrompt))
            {
                builder.AppendLine("Дополнительные инструкции проекта:");
                builder.AppendLine(normalizedPrompt.Trim());
                builder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ragContextText))
            {
                builder.AppendLine("Контекст базы знаний:");
                builder.AppendLine(ragContextText.Trim());
                builder.AppendLine();
                builder.AppendLine("Правила работы с контекстом:");
                builder.AppendLine("- Используй только релевантные факты из контекста, не пересказывай случайные определения.");
                builder.AppendLine("- Если точного фрагмента нет, прямо напиши об этом.");
                builder.AppendLine("- Не смешивай сведения из разных записей без явной связи.");
                builder.AppendLine("- Не создавай отдельный раздел \"Факты из базы знаний\", если пользователь сам его не попросил.");
                builder.AppendLine("- Не называй конкретные семейства ВПО, инструменты, CVE/BDU и версии без подтверждения в вопросе или контексте.");
            }

            return builder.ToString().Trim();
        }

        private static string CleanAssistantText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var cleaned = Regex.Replace(
                text,
                @"<think\b[^>]*>.*?</think>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            cleaned = Regex.Replace(
                cleaned,
                @"</?think\b[^>]*>",
                string.Empty,
                RegexOptions.IgnoreCase);

            return cleaned.Trim();
        }

        private static string LimitTextByChars(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text) || maxChars <= 0 || text.Length <= maxChars)
            {
                return text;
            }

            return text[..maxChars].TrimEnd() +
                   $"{Environment.NewLine}{Environment.NewLine}[Контекст базы знаний сокращен до {maxChars:N0} символов.]";
        }

        private static bool IsSystemChatMessage(string text)
        {
            return text.StartsWith("👋") ||
                   text.StartsWith("🧹") ||
                   text.StartsWith("🤔") ||
                   text.StartsWith("❌") ||
                   text.StartsWith("💥") ||
                   text.StartsWith("⏹️") ||
                   text.StartsWith("⚠️") ||
                   text.StartsWith("✅") ||
                   text.StartsWith("🚀") ||
                   text.StartsWith("🛑") ||
                   text.StartsWith("🔴") ||
                   text.StartsWith("🟢") ||
                   text.StartsWith("🟡") ||
                   text.StartsWith("⏳");
        }

        private void ClearChat()
        {
            Messages.Clear();
            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text = "🧹 Чат очищен. Начнём сначала!"
            });
        }

        private void RemoveTypingMessage()
        {
            if (_typingMessage != null)
            {
                Messages.Remove(_typingMessage);
                _typingMessage = null;
            }
        }

        private static string NormalizeSystemPrompt(string? systemPrompt)
        {
            var fallback = new TranslatorConfig().SystemPrompt;

            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                return fallback;
            }

            var lines = systemPrompt
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Where(line => !IsConflictingThinkingInstruction(line))
                .Select(line => line.TrimEnd())
                .ToList();

            var cleaned = string.Join(Environment.NewLine, lines).Trim();

            return string.IsNullOrWhiteSpace(cleaned)
                ? fallback
                : cleaned;
        }

        private static bool IsConflictingThinkingInstruction(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            return line.Contains("Thinking-режим", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("скрытое рассуждение", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("После рассуждения", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("размышление должно быть", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _ragStatusTimer.Stop();
            _generationCts?.Cancel();
            _generationCts?.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }

        private static bool ShouldUseOnlyCurrentQuestionForRag(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var text = NormalizeRagQuestion(message);

            return
                Regex.IsMatch(text, @"bdu:\d{4}-\d{5}", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(text, @"cve-\d{4}-\d{4,7}", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(text, @"jvndb-\d{4}-\d{6}", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(text, @"(?<![a-zа-я0-9])([а-я]{2,8}\.\d+)(?![a-zа-я0-9])", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(text, @"(?<![a-zа-я0-9])([тt]\d+(\.\d+)?)(?![a-zа-я0-9])", RegexOptions.IgnoreCase);
        }

        private static string NormalizeRagQuestion(string text)
        {
            return text
                .ToLowerInvariant()
                .Replace('ё', 'е')
                .Trim();
        }
    }

    public class ChatMessage : ViewModelBase
    {
        public MessageSender Sender { get; set; }
        public string Text { get => Get<string>(); set => Set(value); }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsTyping { get => Get<bool>(); set => Set(value); }
    }
}
