using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.Model;
using PragmaticAnalyzer.MVVM.ViewModel.Main;
using PragmaticAnalyzer.Services;
using PragmaticAnalyzer.Services.LocalLlama;
using System.Collections.ObjectModel;
using System.Net.Http;

namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class VulnerabilitieViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private readonly VulnerabilityModel _model;
        private readonly Func<string, DataType, Task> UpdateConfig;
        private VulnerabilitieFstecViewModel _fstecVm = new();
        private VulnerabilitieNvdViewModel _nvdVm = new();
        private VulnerabilitieJvnViewModel _jvnVm = new();
        private CancellationTokenSource? _updateCancellationTokenSource;
        private VulConfig _config;
        private HashSet<Guid> _vulJvnHashSet;
        private HashSet<Guid> _vulNvdHashSet;
        public object? CurrentView { get => Get<object>(); private set => Set(value); }
        public ObservableCollection<object> DisplayedVulnerabilities { get; private set; } = [];
        public LocalDatabaseSearchViewModel LocalSearch { get; }
        public ObservableCollection<VulnerabilitieFstec> VulnerabilitiesFstec { get; } = [];
        public ObservableCollection<VulnerabilitieNvd> VulnerabilitiesNvd { get; } = [];
        public ObservableCollection<VulnerabilitieJvn> VulnerabilitiesJvn { get; } = [];
        public ObservableCollection<VulnerabilitieNvd> VulnerabilitiesNvdTranslated { get; } = [];
        public ObservableCollection<VulnerabilitieJvn> VulnerabilitiesJvnTranslated { get; } = [];
        // public ObservableCollection<> ExtendedtVulnerabilities { get; }
        public object? SelectedVulnerabilitie
        {
            get => Get<object?>();
            set
            {
                Set(value);
                switch (SelectedDatabase)
                {
                    case DataType.VulnerabilitiesFstec:
                        _fstecVm.SelectedVulnerabilitie = value;
                        break;
                    case DataType.VulnerabilitiesNvd:
                        _nvdVm.SelectedVulnerabilitie = value;
                        break;
                    case DataType.VulnerabilitiesJvn:
                        _jvnVm.SelectedVulnerabilitie = value;
                        break;
                    case DataType.VulnerabilitiesJvnTranslated:
                        _jvnVm.SelectedVulnerabilitie = value;
                        break;
                    case DataType.VulnerabilitiesNvdTranslated:
                        _nvdVm.SelectedVulnerabilitie = value;
                        break;
                }
            }
        }
        public ObservableCollection<DataType> NamesDatabases { get; }
        public DataType SelectedDatabase
        {
            get => Get<DataType>();
            set
            {
                Set(value);
                switch (value)
                {
                    case DataType.VulnerabilitiesFstec:
                        DisplayedVulnerabilities.Clear();
                        foreach (var vul in VulnerabilitiesFstec)
                        {
                            DisplayedVulnerabilities.Add(vul);
                        }
                        CurrentView = _fstecVm;
                        break;
                    case DataType.VulnerabilitiesNvd:
                        DisplayedVulnerabilities.Clear();
                        foreach (var vul in VulnerabilitiesNvd)
                        {
                            DisplayedVulnerabilities.Add(vul);
                        }
                        CurrentView = _nvdVm;
                        break;
                    case DataType.VulnerabilitiesJvn:
                        DisplayedVulnerabilities.Clear();
                        foreach (var vul in VulnerabilitiesJvn)
                        {
                            DisplayedVulnerabilities.Add(vul);
                        }
                        CurrentView = _jvnVm;
                        break;
                    case DataType.VulnerabilitiesJvnTranslated:
                        DisplayedVulnerabilities.Clear();
                        foreach (var vul in VulnerabilitiesJvnTranslated)
                        {
                            DisplayedVulnerabilities.Add(vul);
                        }
                        CurrentView = _jvnVm;
                        break;
                    case DataType.VulnerabilitiesNvdTranslated:
                        DisplayedVulnerabilities.Clear();
                        foreach (var vul in VulnerabilitiesNvdTranslated)
                        {
                            DisplayedVulnerabilities.Add(vul);
                        }
                        CurrentView = _nvdVm;
                        break;
                }
            }
        }
        public string? Status { get => Get<string>(); set => Set(value); }
        public bool Progress { get => Get<bool>(); set => Set(value); }

        public VulnerabilitieViewModel(
            Func<string, DataType, Task> updateConfig,
            VulConfig vulConfig,
            HashSet<Guid> vulJvnHashSet,
            HashSet<Guid> vulNvdHashSet,
            Action<object> setCurrentView)
        {
            _fileService = new FileService();
            _config = vulConfig;
            _vulJvnHashSet = vulJvnHashSet;
            _vulNvdHashSet = vulNvdHashSet;
            _model = new(_config);
            _model.NotifyRequested += (msg) => Status += "\n\n" + msg;
            CurrentView = _fstecVm;
            UpdateConfig = updateConfig;
            Progress = false;
            LocalSearch = new(
                "Поиск только по выбранной БД уязвимостей",
                GetSearchSources,
                this,
                setCurrentView);
            NamesDatabases =
            [
                DataType.VulnerabilitiesFstec,
                DataType.VulnerabilitiesNvd,
                 DataType.VulnerabilitiesNvdTranslated,
                DataType.VulnerabilitiesJvn,
                DataType.VulnerabilitiesJvnTranslated,
                //DataType.VulnerabilitiesExtended
            ];
            SelectedDatabase = DataType.VulnerabilitiesFstec;
        }

        private IEnumerable<DatabaseSearchSource> GetSearchSources()
        {
            yield return new($"БД уязвимостей: {GetSelectedDatabaseName()}", GetSelectedDatabaseItems());
        }

        private IEnumerable<object> GetSelectedDatabaseItems()
        {
            return SelectedDatabase switch
            {
                DataType.VulnerabilitiesFstec => VulnerabilitiesFstec,
                DataType.VulnerabilitiesNvd => VulnerabilitiesNvd,
                DataType.VulnerabilitiesNvdTranslated => VulnerabilitiesNvdTranslated,
                DataType.VulnerabilitiesJvn => VulnerabilitiesJvn,
                DataType.VulnerabilitiesJvnTranslated => VulnerabilitiesJvnTranslated,
                _ => DisplayedVulnerabilities
            };
        }

        private string GetSelectedDatabaseName()
        {
            return SelectedDatabase switch
            {
                DataType.VulnerabilitiesFstec => "ФСТЭК",
                DataType.VulnerabilitiesNvd => "NVD",
                DataType.VulnerabilitiesNvdTranslated => "Русифицированная NVD",
                DataType.VulnerabilitiesJvn => "JVN",
                DataType.VulnerabilitiesJvnTranslated => "Русифицированная JVN",
                _ => "выбранная база"
            };
        }

        public RelayCommand UpdateCommand => GetCommand(async o =>
        {
            await UpdateVulDb();
        }, o => _updateCancellationTokenSource is null);

        public RelayCommand CancelUpdateCommand => GetCommand(o =>
        {
            _updateCancellationTokenSource?.Cancel();
        }, o => _updateCancellationTokenSource is not null);

        public async Task UpdateVulDb()
        {
            try
            {
                Progress = true;
                _updateCancellationTokenSource = new();

                switch (SelectedDatabase)
                {
                    case DataType.VulnerabilitiesFstec:
                        var newVulnerabilitiesFstec = await _model.GetByLink(_updateCancellationTokenSource.Token);
                        if (newVulnerabilitiesFstec is null) return;
                        VulnerabilitiesFstec.Clear();
                        foreach (var value in newVulnerabilitiesFstec)
                        {
                            VulnerabilitiesFstec.Add(value);
                        }
                        await _fileService.SaveDTOAsync(VulnerabilitiesFstec, DataType.VulnerabilitiesFstec, GlobalConfig.VulnerabilitieFstecPath);
                        break;
                    case DataType.VulnerabilitiesNvd:
                        var newVulnerabilitiesNvd = await _model.GetByApiRequest(_updateCancellationTokenSource.Token);
                        if (newVulnerabilitiesNvd is null) return;
                        foreach (var value in newVulnerabilitiesNvd)
                        {
                            VulnerabilitiesNvd.Add(value);
                        }
                        await _fileService.SaveDTOAsync(VulnerabilitiesNvd, DataType.VulnerabilitiesNvd, GlobalConfig.VulnerabilitieNvdPath);
                        break;
                    case DataType.VulnerabilitiesJvn:
                        var newVulnerabilitiesJvn = await _model.GetByPageParsing(_updateCancellationTokenSource.Token);
                        if (newVulnerabilitiesJvn is null) return;
                        VulnerabilitiesJvn.Clear();
                        foreach (var value in newVulnerabilitiesJvn)
                        {
                            VulnerabilitiesJvn.Add(value);
                        }
                        await _fileService.SaveDTOAsync(VulnerabilitiesJvn, DataType.VulnerabilitiesJvn, GlobalConfig.VulnerabilitieJvnPath);
                        break;
                    case DataType.VulnerabilitiesNvdTranslated:
                        foreach (var vul in VulnerabilitiesNvd)
                        {
                            if (_vulNvdHashSet.Contains(vul.GuidId))
                                continue;

                            Status += "\n\n" + "Перевод через локальную GGUF-модель";
                            var translatedText = await TranslateDescriptionAsync(
                                vul.Description,
                                _updateCancellationTokenSource.Token);

                            if (!string.IsNullOrWhiteSpace(translatedText))
                            {
                                var translatedVul = vul.Clone();
                                translatedVul.Description = translatedText;
                                VulnerabilitiesNvdTranslated.Add(translatedVul);
                                _vulNvdHashSet.Add(vul.GuidId);
                                await _fileService.SaveDTOAsync(VulnerabilitiesNvdTranslated, DataType.VulnerabilitiesNvdTranslated, GlobalConfig.VulnerabilitieNvdTranslated);
                                await _fileService.SaveFileAsync(_vulNvdHashSet.ToArray(), GlobalConfig.VulNvdHashSetPath);
                                Status += "\n\n" + $"Запись {vul.Identifier} ({vul.GuidId}) переведена";
                            }
                            else
                            {
                                Status += "\n\n" + "Модель вернула пустой перевод";
                            }
                        }
                        break;
                    case DataType.VulnerabilitiesJvnTranslated:
                        foreach (var vul in VulnerabilitiesJvn)
                        {
                            if (_vulJvnHashSet.Contains(vul.GuidId))
                                continue;

                            Status += "\n\n" + "Перевод через локальную GGUF-модель";
                            var translatedText = await TranslateDescriptionAsync(
                                vul.Description,
                                _updateCancellationTokenSource.Token);

                            if (!string.IsNullOrWhiteSpace(translatedText))
                            {
                                var translatedVul = vul.Clone();
                                translatedVul.Description = translatedText;
                                VulnerabilitiesJvnTranslated.Add(translatedVul);
                                _vulJvnHashSet.Add(vul.GuidId);
                                await _fileService.SaveDTOAsync(VulnerabilitiesJvnTranslated, DataType.VulnerabilitiesJvnTranslated, GlobalConfig.VulnerabilitieJvnTranslated);
                                await _fileService.SaveFileAsync(_vulJvnHashSet.ToArray(), GlobalConfig.VulJvnHashSetPath);
                                Status += "\n\n" + $"Запись {vul.Identifier} ({vul.GuidId}) переведена";
                            }
                        }
                        break;
                }
                await _fileService.SaveDTOAsync(_config, DataType.VulConfig, GlobalConfig.VulConfigPath);
                UpdateConfig?.Invoke(DateTime.Now.ToString("f"), DataType.VulnerabilitiesFstec);
            }
            catch (HttpRequestException httpEx)
            {
                Status += "\n\n" + httpEx.Message;
            }
            catch (OperationCanceledException ctEx)
            {
                Status += "\n\n" + ctEx.Message;
            }
            catch (Exception ex)
            {
                Status += "\n\n" + ex.Message;
            }
            finally
            {
                Progress = false;
                _updateCancellationTokenSource?.Dispose();
                _updateCancellationTokenSource = null;
            }
        }

        private async Task<string> TranslateDescriptionAsync(
            string? text,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var config = await _fileService.LoadFileToPathAsync<TranslatorConfig>(
                GlobalConfig.TranslatorConfigPath,
                ct) ?? new TranslatorConfig();

            await InfrastructureOrchestrator.EnsureLocalLlamaLoadedAsync(
                config.ContextSize,
                config.GpuLayerCount,
                config.ThreadCount,
                config.BatchSize,
                config.MicroBatchSize,
                config.ReadinessProbeMaxTokens,
                ct);

            var messages = new[]
            {
                new LocalLlamaChatMessage(
                    "system",
                    "Ты технический переводчик. Переводи текст на русский язык точно, без добавления фактов. Сохраняй CVE, BDU, версии, названия продуктов и технические идентификаторы без изменений."),
                new LocalLlamaChatMessage(
                    "user",
                    $"Переведи на русский язык следующий текст:\n{text.Trim()}")
            };

            return await InfrastructureOrchestrator.LocalLlamaService.GenerateAsync(
                messages,
                Math.Clamp(config.MaxTokens, 300, 1000),
                0.1f,
                0.9f,
                1.05f,
                ct);
        }
    }
}
