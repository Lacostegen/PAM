using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.Model;
using PragmaticAnalyzer.Services;
using PragmaticAnalyzer.WorkingServer.Translate;
using System.Collections.ObjectModel;
using System.Net.Http;

namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class VulnerabilitieViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
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

        public VulnerabilitieViewModel(Func<string, DataType, Task> updateConfig, VulConfig vulConfig, HashSet<Guid> vulJvnHashSet, HashSet<Guid> vulNvdHashSet)
        {
            _apiService = new ApiService();
            _fileService = new FileService();
            _config = vulConfig;
            _vulJvnHashSet = vulJvnHashSet;
            _vulNvdHashSet = vulNvdHashSet;
            _model = new(_config);
            _model.NotifyRequested += (msg) => Status += "\n\n" + msg;
            CurrentView = _fstecVm;
            UpdateConfig = updateConfig;
            Progress = false;
            NamesDatabases =
            [
                DataType.VulnerabilitiesFstec,
                DataType.VulnerabilitiesNvd,
                 DataType.VulnerabilitiesNvdTranslated,
                DataType.VulnerabilitiesJvn,
                DataType.VulnerabilitiesJvnTranslated,
                //DataType.VulnerabilitiesExtended
            ];
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

                            Status += "\n\n" + $"Обращение к серверу (127.0.0.1:{GlobalConfig.TranslatorPort})";
                            RequestTranslate request = new("127.0.0.1", GlobalConfig.TranslatorPort, vul.Description);
                            var response = await _apiService.SendRequestAsync<ResponseTranslate>(request, _updateCancellationTokenSource.Token, 1000);
                            if (response.IsSuccess)
                            {
                                var translatedText = response.Value.Results[0].Text;
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
                                Status += "\n\n" + $"{response.ErrorMessage})";
                            }
                        }
                        break;
                    case DataType.VulnerabilitiesJvnTranslated:
                        foreach (var vul in VulnerabilitiesJvn)
                        {
                            if (_vulJvnHashSet.Contains(vul.GuidId))
                                continue;

                            Status += "\n\n" + $"Обращение к серверу (127.0.0.1:{GlobalConfig.TranslatorPort})";
                            RequestTranslate request = new("127.0.0.1", GlobalConfig.TranslatorPort, vul.Description);
                            var response = await _apiService.SendRequestAsync<ResponseTranslate>(request, _updateCancellationTokenSource.Token, 1000);
                            if (response.IsSuccess)
                            {
                                var translatedText = response.Value.Results[0].Text;
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
    }
}
