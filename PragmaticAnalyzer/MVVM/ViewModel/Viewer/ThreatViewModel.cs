using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.Model;
using PragmaticAnalyzer.Services;
using System.Collections.ObjectModel;
using System.Net.Http;

namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class ThreatViewModel : ViewModelBase
    {
        private ThreatConfig _threatConfig;
        private CancellationTokenSource? _updateCancellationTokenSource;
        private readonly ThreatModel _model;
        private readonly IFileService _fileService;
        private readonly Func<string, DataType, Task> LastUpdateConfig;
        public ObservableCollection<Threat> Threats { get; }
        public Threat? SelectedThreat { get => Get<Threat?>(); set => Set(value); }
        public string? Status { get => Get<string>(); set => Set(value); }
        public bool Progress { get => Get<bool>(); set => Set(value); }

        public ThreatViewModel(ObservableCollection<Threat> threats, Func<string, DataType, Task> lastUpdateConfig, ThreatConfig threatConfig)
        {
            Threats = threats;
            LastUpdateConfig = lastUpdateConfig;
            _threatConfig = threatConfig;
            Progress = false;
            _fileService = new FileService();
            _model = new();
            _model.NotifyRequested += (msg) => Status += "\n" + msg;
        }

        public RelayCommand UpdateCommand => GetCommand(async o =>
        {
            await UpdateThreatDb();
        }, o => _updateCancellationTokenSource is null);

        public RelayCommand CancelUpdateCommand => GetCommand(o =>
        {
            _updateCancellationTokenSource?.Cancel();
        }, o => _updateCancellationTokenSource is not null);

        public async Task UpdateThreatDb()
        {
            Progress = true;
            _updateCancellationTokenSource = new CancellationTokenSource();
            try
            {
                var newThreats = await _model.CreateDatabase(_threatConfig.ParsingFstecUrl, _updateCancellationTokenSource.Token);
                if (newThreats is null) return;
                Threats.Clear();
                foreach (var threat in newThreats)
                {
                    Threats.Add(threat);
                }
                await _fileService.SaveDTOAsync(Threats, DataType.Threat, GlobalConfig.ThreatPath);
                LastUpdateConfig?.Invoke(DateTime.Now.ToString("f"), DataType.Threat);
            }
            catch (HttpRequestException httpEx)
            {
                Status += "\n" + httpEx.Message;
            }
            catch (OperationCanceledException ctEx)
            {
                Status += "\n" + ctEx.Message;
            }
            catch (Exception ex)
            {
                Status += "\n" + ex.Message;
            }
            finally
            {
                Progress = false;
                _updateCancellationTokenSource.Dispose();
                _updateCancellationTokenSource = null;
            }
        }
    }
}