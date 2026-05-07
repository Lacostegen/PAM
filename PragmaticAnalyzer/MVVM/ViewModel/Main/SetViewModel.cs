using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Enums;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class SetViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private readonly IInfrastructureOrchestrator _viewModelsService;
        public Func<Task>? UpdateThreatDb;
        public Func<Task>? UpdateExploitDb;
        public Func<Task>? UpdateVulDb;

        public readonly Func<string, DataType, Task> UpdateConfig;
        public ViewPath DisplayedPaths { get => Get<ViewPath>(); set => Set(value); }
        public LastUpdateConfig LastUpdateConfig { get => Get<LastUpdateConfig>(); set => Set(value); }
        public Brush VulnerabilitieButtonBackground { get => Get<Brush>(); set => Set(value); }
        public Brush ThreatButtonBackground { get => Get<Brush>(); set => Set(value); }
        public Brush TacticButtonBackground { get => Get<Brush>(); set => Set(value); }
        public Brush ProtectionMeasureButtonBackground { get => Get<Brush>(); set => Set(value); }
        public Brush OutcomesButtonBackground { get => Get<Brush>(); set => Set(value); }
        public Brush ExploitButtonBackground { get => Get<Brush>(); set => Set(value); }
        public Brush ViolatorButtonBackground { get => Get<Brush>(); set => Set(value); }
        public Brush CurrentStatusButtonBackground { get => Get<Brush>(); set => Set(value); }
        public Brush ReferenceStatusButtonBackground { get => Get<Brush>(); set => Set(value); }
        public Brush SpecialistButtonBackground { get => Get<Brush>(); set => Set(value); }

        public SetViewModel(LastUpdateConfig lastUpdateConfig, IInfrastructureOrchestrator viewModelsService)
        {
            LastUpdateConfig = lastUpdateConfig;
            DisplayedPaths = new();
            _fileService = new FileService();
            _viewModelsService = viewModelsService;
            UpdateConfig += OnUpdateLastUpdateConfig;

            ThreatButtonBackground = Brushes.Red;
            VulnerabilitieButtonBackground = Brushes.Red;
            TacticButtonBackground = Brushes.Red;
            ProtectionMeasureButtonBackground = Brushes.Red;
            OutcomesButtonBackground = Brushes.Red;
            ExploitButtonBackground = Brushes.Red;
            ViolatorButtonBackground = Brushes.Red;
            SpecialistButtonBackground = Brushes.Red;
            ReferenceStatusButtonBackground = Brushes.Red;
            CurrentStatusButtonBackground = Brushes.Red;
        }

        public RelayCommand SetCommand => GetCommand(async o =>
        {
            var pathDto = DialogService.OpenFileDialog(DialogService.JsonFilter);
            if (pathDto is null) return;
            var rawDto = await _fileService.LoadFileToPathAsync<DTO<object>>(pathDto);
            if (rawDto is null) return;

            if (rawDto.DtoType is DataType.Threat)
            {
                var threats = await _fileService.LoadDTOAsync<ObservableCollection<Threat>>(pathDto, DataType.Threat);
                if (threats is null)
                {
                    return;
                }
                foreach (var threat in threats)
                {
                    _viewModelsService.ThreatVm.Threats.Add(threat);
                }
                await _fileService.SaveDTOAsync(threats, DataType.Threat, GlobalConfig.ThreatPath);
                UpdateConfig?.Invoke(rawDto.DateCreation.ToString("f"), DataType.Threat);
            }
            else if (rawDto.DtoType is DataType.ProtectionMeasures)
            {
                var protectionMeasures = await _fileService.LoadDTOAsync<ObservableCollection<ProtectionMeasure>>(pathDto, DataType.ProtectionMeasures);
                if (protectionMeasures is null)
                {
                    return;
                }
                foreach (var protectionMeasure in protectionMeasures)
                {
                    _viewModelsService.ProtectionMeasureVm.ProtectionMeasures.Add(protectionMeasure);
                }
                await _fileService.SaveDTOAsync(protectionMeasures, DataType.ProtectionMeasures, GlobalConfig.ProtectionMeasurePath);
                UpdateConfig?.Invoke(rawDto.DateCreation.ToString("f"), DataType.ProtectionMeasures);
            }
            else if (rawDto.DtoType is DataType.Tactic)
            {
                var techniquesTactics = await _fileService.LoadDTOAsync<ObservableCollection<Tactic>>(pathDto, DataType.Tactic);
                if (techniquesTactics is null)
                {
                    return;
                }
                foreach (var techniquesTactic in techniquesTactics)
                {
                    _viewModelsService.TacticVm.Tactics.Add(techniquesTactic);
                }
                await _fileService.SaveDTOAsync(techniquesTactics, DataType.Tactic, GlobalConfig.TacticPath);
                UpdateConfig?.Invoke(rawDto.DateCreation.ToString("f"), DataType.Tactic);
            }
            else if (rawDto.DtoType is DataType.VulnerabilitiesFstec)
            {
                var vulnerabilities = await _fileService.LoadDTOAsync<ObservableCollection<VulnerabilitieFstec>>(pathDto, DataType.VulnerabilitiesFstec);
                if (vulnerabilities is null)
                {
                    return;
                }
                foreach (var vulnerabilitie in vulnerabilities)
                {
                    _viewModelsService.VulnerabilitieVm.VulnerabilitiesFstec.Add(vulnerabilitie);
                }
                await _fileService.SaveDTOAsync(vulnerabilities, DataType.VulnerabilitiesFstec, GlobalConfig.VulnerabilitieFstecPath);
                UpdateConfig?.Invoke(rawDto.DateCreation.ToString("f"), DataType.VulnerabilitiesFstec);
            }
            else if (rawDto.DtoType is DataType.Outcomes)
            {
                var outcomes = await _fileService.LoadDTOAsync<Outcomes>(pathDto, DataType.Outcomes);
                if (outcomes is null)
                {
                    return;
                }
                foreach (var consequence in outcomes.Consequences)
                {
                    _viewModelsService.OutcomeVm.Outcomes.Consequences.Add(consequence);
                }
                foreach (var technology in outcomes.Technologys)
                {
                    _viewModelsService.OutcomeVm.Outcomes.Technologys.Add(technology);
                }
                await _fileService.SaveDTOAsync(outcomes, DataType.Outcomes, GlobalConfig.OutcomesPath);
                UpdateConfig?.Invoke(rawDto.DateCreation.ToString("f"), DataType.Outcomes);
            }
            else if (rawDto.DtoType is DataType.Exploit)
            {
                var exploits = await _fileService.LoadDTOAsync<ObservableCollection<Exploit>>(pathDto, DataType.Exploit);
                if (exploits is null)
                {
                    return;
                }
                foreach (var exploit in exploits)
                {
                    _viewModelsService.ExploitVm.Exploits.Add(exploit);
                }
                await _fileService.SaveDTOAsync(exploits, DataType.Exploit, GlobalConfig.ExploitPath);
                UpdateConfig?.Invoke(rawDto.DateCreation.ToString("f"), DataType.Exploit);
            }
            else if (rawDto.DtoType is DataType.Violator)
            {
                var violators = await _fileService.LoadDTOAsync<ObservableCollection<Violator>>(pathDto, DataType.Violator);
                if (violators is null)
                {
                    return;
                }
                foreach (var violator in violators)
                {
                    _viewModelsService.ViolatorVm.Violators.Add(violator);
                }
                await _fileService.SaveDTOAsync(violators, DataType.Violator, GlobalConfig.ViolatorPath);
                UpdateConfig?.Invoke(rawDto.DateCreation.ToString("f"), DataType.Violator);
            }
            else if (rawDto.DtoType is DataType.Specialist)
            {
                var specialists = await _fileService.LoadDTOAsync<ObservableCollection<Specialist>>(pathDto, DataType.Specialist);
                if (specialists is null)
                {
                    return;
                }
                foreach (var specialist in specialists)
                {
                    _viewModelsService.SpecialistVm.Specialists.Add(specialist);
                }
                await _fileService.SaveDTOAsync(specialists, DataType.Specialist, GlobalConfig.SpecialistPath);
                UpdateConfig?.Invoke(rawDto.DateCreation.ToString("f"), DataType.Specialist);
            }
            else if (rawDto.DtoType is DataType.ReferenceStatus)
            {
                var referencesStatus = await _fileService.LoadDTOAsync<ObservableCollection<ReferenceStatus>>(pathDto, DataType.ReferenceStatus);
                if (referencesStatus is null)
                {
                    return;
                }
                foreach (var referenceStatus in referencesStatus)
                {
                    _viewModelsService.ReferenceStatusVm.ReferencesStatus.Add(referenceStatus);
                }
                await _fileService.SaveDTOAsync(referencesStatus, DataType.ReferenceStatus, GlobalConfig.RefStatPath);
                UpdateConfig?.Invoke(rawDto.DateCreation.ToString("f"), DataType.ReferenceStatus);
            }
            else if (rawDto.DtoType is DataType.CurrentStatus)
            {
                var currentsStatus = await _fileService.LoadDTOAsync<ObservableCollection<CurrentStatus>>(pathDto, DataType.CurrentStatus);
                if (currentsStatus is null)
                {
                    return;
                }
                foreach (var currentStatus in currentsStatus)
                {
                    _viewModelsService.CurrentStatusVm.CurrentsStatus.Add(currentStatus);
                }
                await _fileService.SaveDTOAsync(currentsStatus, DataType.CurrentStatus, GlobalConfig.CurStatPath);
                UpdateConfig?.Invoke(rawDto.DateCreation.ToString("f"), DataType.CurrentStatus);
            }
        });

        public RelayCommand UpdateThreatDbCommand => GetCommand(o =>
        {
            UpdateThreatDb?.Invoke();
        });

        public RelayCommand UpdateExploitDbCommand => GetCommand(o =>
        {
            UpdateExploitDb?.Invoke();
        });

        public RelayCommand UpdateVulDbCommand => GetCommand(o =>
        {
            UpdateVulDb?.Invoke();
        });

        private async Task OnUpdateLastUpdateConfig(string updateTime, DataType type)
        {
            if (type is DataType.VulnerabilitiesFstec)
            {
                VulnerabilitieButtonBackground = Brushes.Green;
                LastUpdateConfig.Vulnerabilitie = updateTime;
                DisplayedPaths.VulnerabilitiePath = GlobalConfig.VulnerabilitieFstecPath;
            }
            else if (type is DataType.Threat)
            {
                ThreatButtonBackground = Brushes.Green;
                LastUpdateConfig.Threat = updateTime;
                DisplayedPaths.ThreatPath = GlobalConfig.ThreatPath;
            }
            else if (type is DataType.ProtectionMeasures)
            {
                ProtectionMeasureButtonBackground = Brushes.Green;
                LastUpdateConfig.ProtectionMeasure = updateTime;
                DisplayedPaths.ProtectionMeasurePath = GlobalConfig.ProtectionMeasurePath;
            }
            else if (type is DataType.Tactic)
            {
                TacticButtonBackground = Brushes.Green;
                LastUpdateConfig.Tactic = updateTime;
                DisplayedPaths.TacticPath = GlobalConfig.TacticPath;
            }
            else if (type is DataType.Outcomes)
            {
                OutcomesButtonBackground = Brushes.Green;
                LastUpdateConfig.Outcomes = updateTime;
                DisplayedPaths.OutcomesPath = GlobalConfig.OutcomesPath;
            }
            else if (type is DataType.Exploit)
            {
                ExploitButtonBackground = Brushes.Green;
                LastUpdateConfig.Exploit = updateTime;
                DisplayedPaths.ExploitPath = GlobalConfig.ExploitPath;
            }
            else if (type is DataType.Violator)
            {
                ViolatorButtonBackground = Brushes.Green;
                LastUpdateConfig.Violator = updateTime;
                DisplayedPaths.ViolatorPath = GlobalConfig.ViolatorPath;
            }
            else if (type is DataType.Specialist)
            {
                SpecialistButtonBackground = Brushes.Green;
                LastUpdateConfig.Specialist = updateTime;
                DisplayedPaths.SpecialistPath = GlobalConfig.SpecialistPath;
            }
            else if (type is DataType.ReferenceStatus)
            {
                ReferenceStatusButtonBackground = Brushes.Green;
                LastUpdateConfig.RefStatus = updateTime;
                DisplayedPaths.RefStatusPath = GlobalConfig.RefStatPath;
            }
            else if (type is DataType.CurrentStatus)
            {
                CurrentStatusButtonBackground = Brushes.Green;
                LastUpdateConfig.CurStatus = updateTime;
            }
            await _fileService.SaveDTOAsync(LastUpdateConfig, DataType.LastUpdateConfig, GlobalConfig.LastUpdateConfig);
        }
    }

    public class ViewPath : ViewModelBase
    {
        public string VulnerabilitiePath { get => Get<string>(); set => Set(value); }
        public string ThreatPath { get => Get<string>(); set => Set(value); }
        public string TacticPath { get => Get<string>(); set => Set(value); }
        public string ProtectionMeasurePath { get => Get<string>(); set => Set(value); }
        public string OutcomesPath { get => Get<string>(); set => Set(value); }
        public string ExploitPath { get => Get<string>(); set => Set(value); }
        public string ViolatorPath { get => Get<string>(); set => Set(value); }
        public string SpecialistPath { get => Get<string>(); set => Set(value); }
        public string RefStatusPath { get => Get<string>(); set => Set(value); }
        public string CurStatusPath { get => Get<string>(); set => Set(value); }
    }
}