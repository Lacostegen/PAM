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
        }

        public RelayCommand GenerateCommand => GetCommand(async o =>
        {
            Progress = true;

            string usedModel = string.Empty;
            switch (SelectedAlgorithm)
            {
                case Algorithm.TfIdf:
                    usedModel = string.Empty;
                    break;
                case Algorithm.FastText:
                    if (_fastTextModelPath is null)
                    {
                        Progress = false;
                        MessageBox.Show("Не выбрана модель");
                        return;
                    }
                    else
                    {
                        usedModel = _fastTextModelPath;
                    }
                    break;
                case Algorithm.WordTwoVec:
                    if (_wordTwoVecModelPath is null)
                    {
                        Progress = false;
                        MessageBox.Show("Не выбрана модель");
                        return;
                    }
                    else
                    {
                        usedModel = _wordTwoVecModelPath;
                    }
                    break;
            }

            List<string> usedSources = [];
            foreach (var config in AvailableDatabasesConfig)
            {
                if (config.IsChecked)
                {
                    usedSources.Add(config.FullName);
                }
            }
            RequestMatcher request = new("127.0.0.1", GlobalConfig.MatcherPort, RequestText, SelectedAlgorithm, FilteringCvss, usedModel, usedSources);

            var result = await _apiService.SendRequestAsync<ResponseMatcher>(request);
            if (!result.IsSuccess)
            {
                Progress = false;
                MessageBox.Show(result.ErrorMessage);
                return;
            }

            var reports = GetReports(result.Value.MatcherObjects);
            if (reports is null)
            {
                Progress = false;
                return;
            }

            Reports.Clear();
            foreach (var report in reports)
            {
                Reports.Add(report);
            }

            Progress = false;
            ReportVisibility = Visibility.Visible;
        }, o => RequestText != string.Empty && RequestText is not null && SelectedSpecialist is not null && SelectedProtectionMeasures is not null
                    && SelectedConsequence is not null && SelectedTechnology is not null);

        public RelayCommand SaveReportCommand => GetCommand(async o =>
        {
            var savePath = DialogService.SaveFileDialog($"Рапорт от {DateTime.Now:D}", DialogService.WordFilter);
            Progress = true;
            try
            {
                using var doc = DocX.Create(savePath, DocumentTypes.Document);
                await Task.Run(() => ReportWorker.CreateReport(doc, SelectedReport));
                doc.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            Progress = false;
        }, o => SelectedReport is not null);

        public RelayCommand SettingSearchCommand => GetCommand(o =>
        {
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