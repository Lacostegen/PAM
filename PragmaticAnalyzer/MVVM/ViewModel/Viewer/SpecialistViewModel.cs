using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.Views.Viewer;
using PragmaticAnalyzer.Services;
using System.Collections.ObjectModel;


namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class SpecialistViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private readonly Func<string, DataType, Task> UpdateConfig;
        private SpecialistManagerView? _manager;
        private bool _isAdd;
        public ObservableCollection<Specialist> Specialists { get; set; }
        public Specialist? SelectedSpecialist { get => Get<Specialist?>(); set => Set(value); }
        public Specialist ManagerSpecialist { get => Get<Specialist>(); set => Set(value); }
        public string SelectedInteractingOrgan { get => Get<string>(); set => Set(value); }
        public string SelectedMeasure { get => Get<string>(); set => Set(value); }
        public string NewInteractingOrgan { get => Get<string>(); set => Set(value); }
        public string NewMeasure { get => Get<string>(); set => Set(value); }


        public SpecialistViewModel(ObservableCollection<Specialist> specialists, Func<string, DataType, Task> updateConfig)
        {
            Specialists = specialists;
            UpdateConfig += updateConfig;
            _fileService = new FileService();
            ManagerSpecialist = new();
        }

        public RelayCommand AddCommand => GetCommand(async o =>
        {
            _isAdd = true;
            ManagerSpecialist = new();
            _manager = new(this);
            _manager.ShowDialog();
            await _fileService.SaveDTOAsync(Specialists, DataType.Specialist, GlobalConfig.SpecialistPath);
            UpdateConfig?.Invoke(DateTime.Now.ToString("f"), DataType.Specialist);
        });

        public RelayCommand DeleteCommand => GetCommand(async o =>
        {
            if (SelectedSpecialist is Specialist selectedSpecialist)
            {
                Specialists.Remove(selectedSpecialist);
            }
            await _fileService.SaveDTOAsync(Specialists, DataType.Specialist, GlobalConfig.SpecialistPath);
            UpdateConfig?.Invoke(DateTime.Now.ToString("f"), DataType.Specialist);
        });

        public RelayCommand ChangeCommand => GetCommand(async o =>
        {
            _isAdd = false;
            if (SelectedSpecialist is null) return;
            ManagerSpecialist = SelectedSpecialist;
            _manager = new(this);
            _manager.ShowDialog();
            await _fileService.SaveDTOAsync(Specialists, DataType.Specialist, GlobalConfig.SpecialistPath);
            UpdateConfig?.Invoke(DateTime.Now.ToString("f"), DataType.Specialist);
        });

        public RelayCommand AddMeasureCommand => GetCommand(o =>
        {
            if (!string.IsNullOrWhiteSpace(NewMeasure) && !ManagerSpecialist.UsingMeasures.Contains(NewMeasure))
                ManagerSpecialist.UsingMeasures.Add(NewMeasure);
            NewMeasure = string.Empty;
        });

        public RelayCommand DeleteMeasureCommand => GetCommand(o =>
        {
            if (ManagerSpecialist.UsingMeasures.Contains(SelectedMeasure))
            {
                ManagerSpecialist.UsingMeasures.Remove(SelectedMeasure);
            }
        });

        public RelayCommand AddInteractingOrgansCommand => GetCommand(o =>
        {
            if (!string.IsNullOrWhiteSpace(NewInteractingOrgan) && !ManagerSpecialist.NameInteractingOrgans.Contains(NewInteractingOrgan))
                ManagerSpecialist.NameInteractingOrgans.Add(NewInteractingOrgan);
            NewInteractingOrgan = string.Empty;
        });

        public RelayCommand DeleteInteractingOrgansCommand => GetCommand(o =>
        {
            if (ManagerSpecialist.NameInteractingOrgans.Contains(SelectedInteractingOrgan))
            {
                ManagerSpecialist.NameInteractingOrgans.Remove(SelectedInteractingOrgan);
            }
        });

        public RelayCommand DoneManagerCommand => GetCommand(o =>
        {
            if (_isAdd)
            {
                Specialists.Add(ManagerSpecialist);
            }
            _manager?.Close();
        });

        public RelayCommand CancelManagerCommand => GetCommand(o =>
        {
            _manager?.Close();
        });
    }
}