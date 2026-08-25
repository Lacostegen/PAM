using CommunityToolkit.Mvvm.Messaging;
using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.Messages;
using PragmaticAnalyzer.MVVM.Model;
using PragmaticAnalyzer.MVVM.Views;
using PragmaticAnalyzer.Services;
using PragmaticAnalyzer.WorkingServer.Matcher;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class ConnectionViewModel : ViewModelBase,
        IRecipient<FastTextModelSelectedMessage>,
        IRecipient<WordTwoVecModelSelectedMessage>
    {
        private readonly IInfrastructureOrchestrator _viewModelsService;
        private readonly IApiService _apiService;
        private readonly Dictionary<string, object> _filePathToDatabase;
        private SettingSearchView? _settingSearchView;
        private string? _wordTwoVecModelPath;
        private string? _fastTextModelPath;
        public ObservableCollection<AvailableDatabaseConfig> AvailableDatabasesConfig { get; private set; }
        public ObservableCollection<Report> Reports { get; private set; }
        public Report SelectedReport { get => Get<Report>(); set => Set(value); }
        public ObservableCollection<ProtectionMeasure> ProtectionMeasures { get; private set; }
        public ObservableCollection<Specialist> Specialists { get; private set; }
        public ObservableCollection<Consequence> Consequences { get; private set; }
        public ObservableCollection<Technology> Technologys { get; private set; }
        public ObservableCollection<Algorithm> Algorithms { get; private set; }
        public ProtectionMeasure SelectedProtectionMeasures { get => Get<ProtectionMeasure>(); set => Set(value); }
        public Specialist SelectedSpecialist { get => Get<Specialist>(); set => Set(value); }
        public Consequence SelectedConsequence { get => Get<Consequence>(); set => Set(value); }
        public Technology SelectedTechnology { get => Get<Technology>(); set => Set(value); }
        public Algorithm SelectedAlgorithm { get => Get<Algorithm>(); set => Set(value); }
        public string RequestText { get => Get<string>(); set => Set(value); }
        public bool Progress { get => Get<bool>(); set => Set(value); }
        public bool FilteringCvss { get => Get<bool>(); set => Set(value); }
        public Visibility ReportVisibility { get => Get<Visibility>(); set => Set(value); }
        public string SearchStatusText { get => Get<string>(); set => Set(value); }

        public ConnectionViewModel(IInfrastructureOrchestrator viewModelsService, IApiService apiService,
                                                          ObservableCollection<AvailableDatabaseConfig> availableDatabasesConfig, Dictionary<string, object> filePathToDatabase)
        {
            WeakReferenceMessenger.Default.Register<FastTextModelSelectedMessage>(this);
            WeakReferenceMessenger.Default.Register<WordTwoVecModelSelectedMessage>(this);
            _viewModelsService = viewModelsService;
            _apiService = apiService;
            AvailableDatabasesConfig = availableDatabasesConfig;
            _filePathToDatabase = filePathToDatabase;
            Reports = [];
            ProtectionMeasures = viewModelsService.ProtectionMeasureVm.ProtectionMeasures;
            Specialists = viewModelsService.SpecialistVm.Specialists;
            Consequences = viewModelsService.OutcomeVm.Outcomes.Consequences;
            Technologys = viewModelsService.OutcomeVm.Outcomes.Technologys;
            Algorithms = new(Enum.GetValues(typeof(Algorithm)).Cast<Algorithm>());
            RequestText = string.Empty;
            Progress = false;
            FilteringCvss = true;
            ReportVisibility = Visibility.Hidden;
            SelectedAlgorithm = Algorithm.TfIdf;
            SearchStatusText = "Готово к формированию отчета.";
        }

        public RelayCommand GenerateCommand => GetCommand(async o =>
        {
            Progress = true;
            SearchStatusText = "Проверяю matcher-сервер...";

            try
            {
                var serverResult = await _apiService.EnsureMatcherServerAvailableAsync();
                if (!serverResult.IsSuccess)
                {
                    SearchStatusText = "Matcher-сервер недоступен.";
                    MessageBox.Show(serverResult.ErrorMessage);
                    return;
                }

                var usedModel = GetSelectedModelPathForReport();
                if (usedModel is null)
                {
                    return;
                }

                var matcherModelPath = usedModel;
                if (!string.IsNullOrWhiteSpace(usedModel) && SelectedAlgorithm != Algorithm.TfIdf)
                {
                    var matcherModelPathResult = _apiService.PrepareMatcherModelPath(usedModel);
                    if (!matcherModelPathResult.IsSuccess)
                    {
                        SearchStatusText = "Не удалось подготовить модель для matcher.";
                        MessageBox.Show(matcherModelPathResult.ErrorMessage);
                        return;
                    }

                    matcherModelPath = matcherModelPathResult.Value;
                }

                var usedSources = AvailableDatabasesConfig
                    .Where(config => config.IsChecked)
                    .Select(config => config.FullName)
                    .ToList();

                if (usedSources.Count == 0)
                {
                    SearchStatusText = "Не выбраны источники знаний.";
                    MessageBox.Show("Выбери хотя бы один источник знаний в настройках поиска.");
                    return;
                }

                var matcherSourcePathsResult = _apiService.PrepareMatcherSourcePaths(usedSources);
                if (!matcherSourcePathsResult.IsSuccess)
                {
                    SearchStatusText = "Не удалось подготовить источники знаний для matcher.";
                    MessageBox.Show(matcherSourcePathsResult.ErrorMessage);
                    return;
                }

                var sourcePathMap = matcherSourcePathsResult.Value;
                var matcherUsedSources = sourcePathMap.Values.ToList();
                var originalSourcePathByMatcherPath = sourcePathMap.ToDictionary(
                    pair => pair.Value,
                    pair => pair.Key,
                    StringComparer.OrdinalIgnoreCase);

                SearchStatusText = "Отправляю запрос на сопоставление...";
                RequestMatcher request = new(
                    "127.0.0.1",
                    GlobalConfig.MatcherPort,
                    RequestText,
                    SelectedAlgorithm,
                    FilteringCvss,
                    matcherModelPath,
                    matcherUsedSources);

                var result = await _apiService.SendRequestAsync<ResponseMatcher>(request);
                if (!result.IsSuccess)
                {
                    SearchStatusText = "Ошибка формирования отчета.";
                    MessageBox.Show(result.ErrorMessage);
                    return;
                }

                var matcherObjects = result.Value?.MatcherObjects;
                if (matcherObjects is null || matcherObjects.Count == 0)
                {
                    SearchStatusText = "Matcher не нашел подходящие записи.";
                    MessageBox.Show("По заданному описанию matcher не вернул подходящие записи. Попробуй изменить описание или выбрать другие источники знаний.");
                    return;
                }

                NormalizeMatcherSourcePaths(matcherObjects, originalSourcePathByMatcherPath);

                var reports = GetReports(matcherObjects);
                if (reports is null || reports.Count == 0)
                {
                    SearchStatusText = "Не удалось собрать отчет из найденных записей.";
                    MessageBox.Show("Matcher вернул ссылки на записи, но программа не смогла сопоставить их с загруженными базами данных.");
                    return;
                }

                Reports.Clear();
                foreach (var report in reports)
                {
                    Reports.Add(report);
                }

                SearchStatusText = $"Отчет сформирован. Вариантов: {Reports.Count}.";
                ReportVisibility = Visibility.Visible;
            }
            catch (OperationCanceledException ex)
            {
                SearchStatusText = "Формирование отчета отменено.";
                MessageBox.Show(ex.Message);
                return;
            }
            catch (Exception ex)
            {
                SearchStatusText = "Ошибка формирования отчета.";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Progress = false;
            }
        }, o => !Progress && RequestText != string.Empty && RequestText is not null && SelectedSpecialist is not null && SelectedProtectionMeasures is not null
                    && SelectedConsequence is not null && SelectedTechnology is not null);

        public RelayCommand SaveReportCommand => GetCommand(async o =>
        {
            var savePath = DialogService.SaveFileDialog($"Рапорт от {DateTime.Now:D}", DialogService.WordFilter);

            if (string.IsNullOrWhiteSpace(savePath))
            {
                SearchStatusText = "Сохранение отчета отменено.";
                return;
            }

            Progress = true;
            SearchStatusText = "Сохраняю отчет в Word...";

            try
            {
                using var doc = DocX.Create(savePath, DocumentTypes.Document);
                await Task.Run(() => ReportWorker.CreateReport(doc, SelectedReport));
                doc.Save();
                SearchStatusText = $"Отчет сохранен: {savePath}";
            }
            catch (Exception ex)
            {
                SearchStatusText = "Ошибка сохранения отчета.";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Progress = false;
            }
        }, o => SelectedReport is not null);

        private string? GetSelectedModelPathForReport()
        {
            switch (SelectedAlgorithm)
            {
                case Algorithm.TfIdf:
                    return string.Empty;

                case Algorithm.FastText:
                    if (string.IsNullOrWhiteSpace(_fastTextModelPath))
                    {
                        SearchStatusText = "Не выбрана FastText-модель.";
                        MessageBox.Show("Для алгоритма FastText выбери используемую модель во вкладке «Работа с моделями».");
                        return null;
                    }

                    if (!File.Exists(_fastTextModelPath))
                    {
                        SearchStatusText = "Файл FastText-модели не найден.";
                        MessageBox.Show($"Файл FastText-модели не найден:{Environment.NewLine}{_fastTextModelPath}");
                        return null;
                    }

                    return _fastTextModelPath;

                case Algorithm.WordTwoVec:
                    if (string.IsNullOrWhiteSpace(_wordTwoVecModelPath))
                    {
                        SearchStatusText = "Не выбрана Word2Vec-модель.";
                        MessageBox.Show("Для алгоритма Word2Vec выбери используемую модель во вкладке «Работа с моделями».");
                        return null;
                    }

                    if (!File.Exists(_wordTwoVecModelPath))
                    {
                        SearchStatusText = "Файл Word2Vec-модели не найден.";
                        MessageBox.Show($"Файл Word2Vec-модели не найден:{Environment.NewLine}{_wordTwoVecModelPath}");
                        return null;
                    }

                    return _wordTwoVecModelPath;

                default:
                    SearchStatusText = "Не выбран алгоритм поиска.";
                    MessageBox.Show("Выбери алгоритм поиска в настройках.");
                    return null;
            }
        }

        public RelayCommand SettingSearchCommand => GetCommand(o =>
        {
            EnsureDefaultReportSettings();

            if (Consequences.Count == 0 || Technologys.Count == 0)
            {
                MessageBox.Show(
                    "База рисков не загружена или загружена не полностью. Проверь файл outcomesDb.json во вкладке «Загрузить БД».");
            }

            _settingSearchView = new(this);
            _settingSearchView.ShowDialog();
        });

        public RelayCommand CloseSettingSearchCommand => GetCommand(o =>
        {
            _settingSearchView?.Close();
        });

        public RelayCommand OpenFileCommand => GetCommand(o =>
        {
            string fullPath = Path.Combine(GlobalConfig.ExploitTextPath, $"{o}.txt");
            if (File.Exists(fullPath))
            {
                System.Diagnostics.Process.Start("notepad.exe", fullPath);
            }
        });

        private static void NormalizeMatcherSourcePaths(
            ObservableCollection<ResponseMatcher.MatcherObject> responseMatchers,
            IReadOnlyDictionary<string, string> originalSourcePathByMatcherPath)
        {
            foreach (var responseMatcher in responseMatchers)
            {
                foreach (var source in responseMatcher.Sources.ToList())
                {
                    if (originalSourcePathByMatcherPath.TryGetValue(source.Value, out var originalPath))
                    {
                        responseMatcher.Sources[source.Key] = originalPath;
                    }
                }
            }
        }

        private void EnsureDefaultReportSettings()
        {
            if (SelectedProtectionMeasures is null)
            {
                var protectionMeasure = ProtectionMeasures.FirstOrDefault();
                if (protectionMeasure is not null)
                {
                    SelectedProtectionMeasures = protectionMeasure;
                }
            }

            if (SelectedSpecialist is null)
            {
                var specialist = Specialists.FirstOrDefault();
                if (specialist is not null)
                {
                    SelectedSpecialist = specialist;
                }
            }

            if (SelectedConsequence is null)
            {
                var consequence = Consequences.FirstOrDefault();
                if (consequence is not null)
                {
                    SelectedConsequence = consequence;
                }
            }

            if (SelectedTechnology is null)
            {
                var technology = Technologys.FirstOrDefault();
                if (technology is not null)
                {
                    SelectedTechnology = technology;
                }
            }
        }

        public ObservableCollection<Report>? GetReports(ObservableCollection<ResponseMatcher.MatcherObject> responseMatchers)
        {
            if (responseMatchers is null || responseMatchers.Count is 0)
            {
                return null;
            }

            var vulnerabilitiesDict = _viewModelsService.VulnerabilitieVm.VulnerabilitiesFstec.ToDictionary(v => v.GuidId);
            var threatsDict = _viewModelsService.ThreatVm.Threats.ToDictionary(t => t.GuidId);
            var tacticsDict = _viewModelsService.TacticVm.Tactics.ToDictionary(t => t.GuidId);
            var exploitsDict = _viewModelsService.ExploitVm.Exploits.ToDictionary(e => e.GuidId);
            var violatorDict = _viewModelsService.ViolatorVm.Violators.ToDictionary(e => e.GuidId);

            var results = new ObservableCollection<Report>();

            foreach (var responseMatcher in responseMatchers)
            {
                var report = new Report
                {
                    Coefficient = responseMatcher.Coefficient,
                    ProtectionMeasure = SelectedProtectionMeasures,
                    Specialist = SelectedSpecialist,
                    Consequence = SelectedConsequence,
                    Technology = SelectedTechnology,
                    DynamicRecords = []
                };

                foreach (var source in responseMatcher.Sources)
                {
                    foreach (var item in _filePathToDatabase)
                    {
                        if (item.Key == source.Value)
                        {
                            var config = AvailableDatabasesConfig.FirstOrDefault(path => path.FullName == item.Key);
                            if (config is null)
                            {
                                break;
                            }

                            switch (config.DetectedType)
                            {
                                case DataType.VulnerabilitiesFstec:
                                    if (vulnerabilitiesDict.TryGetValue(source.Key, out var vulnerabilitie))
                                    {
                                        report.Vulnerabilitie = vulnerabilitie;
                                    }
                                    break;
                                case DataType.Threat:
                                    if (threatsDict.TryGetValue(source.Key, out var threat))
                                    {
                                        report.Threat = threat;
                                    }
                                    break;
                                case DataType.Tactic:
                                    if (tacticsDict.TryGetValue(source.Key, out var tactic))
                                    {
                                        report.Tactic = tactic;
                                    }
                                    break;
                                case DataType.Exploit:
                                    if (exploitsDict.TryGetValue(source.Key, out var exploit))
                                    {
                                        report.Exploit = exploit;
                                    }
                                    break;
                                case DataType.Violator:
                                    if (violatorDict.TryGetValue(source.Key, out var violator))
                                    {
                                        report.Violator = violator;
                                    }
                                    break;
                                case DataType.DunamicDatabase:
                                    var dumanicRecords = (ObservableCollection<DynamicRecord>)_filePathToDatabase.FirstOrDefault(db => db.Key == item.Key).Value;
                                    foreach (var dunamicRecord in dumanicRecords)
                                    {
                                        if (dunamicRecord.GuidId == source.Key)
                                        {
                                            report.DynamicRecords.Add(dunamicRecord);
                                        }
                                    }
                                    break;
                            }
                            break;
                        }
                    }
                }
                results.Add(report);
            }
            return results;
        }

        public void Receive(FastTextModelSelectedMessage message)
        {
            _fastTextModelPath = message.ModelPath;
        }

        public void Receive(WordTwoVecModelSelectedMessage message)
        {
            _wordTwoVecModelPath = message.ModelPath;
        }
    }
    public class Report : ViewModelBase
    {
        public float Coefficient { get => Get<float>(); set => Set(value); }
        public ProtectionMeasure? ProtectionMeasure { get => Get<ProtectionMeasure>(); set => Set(value); }
        public Specialist? Specialist { get => Get<Specialist>(); set => Set(value); }
        public Consequence? Consequence { get => Get<Consequence>(); set => Set(value); }
        public Technology? Technology { get => Get<Technology>(); set => Set(value); }
        public VulnerabilitieFstec? Vulnerabilitie { get => Get<VulnerabilitieFstec>(); set => Set(value); }
        public Threat? Threat { get => Get<Threat>(); set => Set(value); }
        public Tactic? Tactic { get => Get<Tactic>(); set => Set(value); }
        public Exploit? Exploit { get => Get<Exploit>(); set => Set(value); }
        public Violator? Violator { get => Get<Violator>(); set => Set(value); }
        public ObservableCollection<DynamicRecord>? DynamicRecords { get => Get<ObservableCollection<DynamicRecord>>(); set => Set(value); }
    }
}
