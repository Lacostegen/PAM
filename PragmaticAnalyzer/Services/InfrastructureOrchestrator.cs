using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.Extensions;
using PragmaticAnalyzer.MVVM.ViewModel.Main;
using PragmaticAnalyzer.MVVM.ViewModel.Viewer;
using System.Collections.ObjectModel;
using System.IO;

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

        public InfrastructureOrchestrator()
        {
            MainVm = new(this);
            SetVm = new(_lastUpdateConfig, this);
            ThreatVm = new([], SetVm.UpdateConfig, _threatConfig);
            VulnerabilitieVm = new(SetVm.UpdateConfig, _vulConfig, _vulJvnHashSet, _vulNvdHashSet);
            ExploitVm = new([], SetVm.UpdateConfig, _exploitConfig);
            OntologyVm = new([]);
            TacticVm = new([], SetVm.UpdateConfig);
            ViolatorVm = new([], SetVm.UpdateConfig);
            ProtectionMeasureVm = new([], SetVm.UpdateConfig);
            SpecialistVm = new([], SetVm.UpdateConfig);
            ReferenceStatusVm = new([], SetVm.UpdateConfig);
            CurrentStatusVm = new([], SetVm.UpdateConfig);
            OutcomeVm = new(new(), SetVm.UpdateConfig);
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
                var wordTwoVecConfigs = await _fileService.LoadDTOAsync<ObservableCollection<ModelConfig>>(GlobalConfig.WordTwoVecConfigPath, DataType.WordTwoVecConfig) ?? [];
                foreach (var config in wordTwoVecConfigs)
                {
                    if (config.Path != Path.Combine(GlobalConfig.ModelsPath, config.DisplayedName))
                    {
                        config.Path = Path.Combine(GlobalConfig.ModelsPath, config.DisplayedName);
                        await _fileService.SaveDTOAsync(wordTwoVecConfigs, DataType.WordTwoVecConfig, GlobalConfig.WordTwoVecConfigPath);
                    }
                }
                _wordTwoVecConfig.ReplaceAll(wordTwoVecConfigs);
            } //
            if (File.Exists(GlobalConfig.FastTextConfigPath))
            {
                var fastTextConfigs = await _fileService.LoadDTOAsync<ObservableCollection<ModelConfig>>(GlobalConfig.FastTextConfigPath, DataType.FastTextConfig) ?? [];
                foreach (var config in fastTextConfigs)
                {
                    if (config.DisplayedName != Path.Combine(GlobalConfig.ModelsPath, config.DisplayedName))
                    {
                        config.Path = Path.Combine(GlobalConfig.ModelsPath, config.DisplayedName);
                        await _fileService.SaveDTOAsync(fastTextConfigs, DataType.FastTextConfig, GlobalConfig.FastTextConfigPath);
                    }
                }
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

            var outcomesDto = await _fileService.LoadFileToPathAsync<DTO<Outcomes>>(GlobalConfig.OutcomesPath);
            if (outcomesDto != default)
            {
                foreach (var technology in outcomesDto.Value.Technologys)
                {
                    OutcomeVm.Outcomes.Technologys.Add(technology);
                }
                foreach (var consequence in outcomesDto.Value.Consequences)
                {
                    OutcomeVm.Outcomes.Consequences.Add(consequence);
                }
                SetVm.UpdateConfig?.Invoke(outcomesDto.DateCreation.ToString("f"), DataType.Outcomes);
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
        } // проверятет все глобальные пути (конфиги, базы данных) если нет - создает, если есть - загргужает

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
            await _fileService.SaveDTOAsync(SettingVm.WordTwoVecConfigs, DataType.WordTwoVecConfig, GlobalConfig.WordTwoVecConfigPath);
            await _fileService.SaveDTOAsync(SettingVm.FastTextConfigs, DataType.FastTextConfig, GlobalConfig.FastTextConfigPath);
        }
    } // сервис инициализации программы
}