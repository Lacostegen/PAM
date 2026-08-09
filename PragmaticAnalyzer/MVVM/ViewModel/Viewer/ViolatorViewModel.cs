using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.ViewModel.Main;
using PragmaticAnalyzer.MVVM.Views.Viewer;
using PragmaticAnalyzer.Services;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class ViolatorViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private readonly Func<string, DataType, Task> UpdateConfig;
        private ViolatorManagerView? _manager;
        private bool _isAdd;
        public ObservableCollection<Violator> Violators { get; set; }
        public LocalDatabaseSearchViewModel LocalSearch { get; }
        public Violator? SelectedViolator { get => Get<Violator?>(); set => Set(value); }
        public Violator ManagerViolator { get => Get<Violator>(); set => Set(value); }

        public ViolatorViewModel(
            ObservableCollection<Violator> violators,
            Func<string, DataType, Task> updateConfig,
            Action<object> setCurrentView)
        {
            Violators = violators;
            UpdateConfig += updateConfig;
            LocalSearch = new(
                "Поиск только по БД нарушителей",
                () => [new DatabaseSearchSource("БД нарушителей", Violators)],
                this,
                setCurrentView);
            ManagerViolator = new();
            _fileService = new FileService();
            Violators.CollectionChanged += Violators_CollectionChanged;
            EnsureSelection();
        }

        public void EnsureSelection()
        {
            if (SelectedViolator is null && Violators.Count > 0)
            {
                SelectedViolator = Violators[0];
            }
        }

        private void Violators_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedViolator is null || !Violators.Contains(SelectedViolator))
            {
                EnsureSelection();
            }
        }

        public RelayCommand AddCommand => GetCommand(async o =>
        {
           /* _isAdd = true;
            ManagerViolator = new()
            {
                SequenceNumber = Violators.Count + 1,
                Potential = new(),
                Source = new()
            };
            _manager = new(this);
            _manager.ShowDialog();
            await _fileService.SaveDTOAsync(Violators, DataType.Violator, GlobalConfig.ViolatorPath);
            UpdateConfig?.Invoke(DateTime.Now.ToString("f"), DataType.Violator);*/
        });

        public RelayCommand DeleteCommand => GetCommand(async o =>
        {
         /*   if (SelectedViolator is Violator selectedViolator)
            {
                Violators.Remove(selectedViolator);
            }
            await _fileService.SaveDTOAsync(Violators, DataType.Violator, GlobalConfig.ViolatorPath);
            UpdateConfig?.Invoke(DateTime.Now.ToString("f"), DataType.Violator);*/
        });

        public RelayCommand ChangeCommand => GetCommand(async o =>
        {
          /*  _isAdd = false;
            if (SelectedViolator is null) return;
            ManagerViolator = SelectedViolator;
            _manager = new(this);
            _manager.ShowDialog();
            await _fileService.SaveDTOAsync(Violators, DataType.Violator, GlobalConfig.ViolatorPath);
            UpdateConfig?.Invoke(DateTime.Now.ToString("f"), DataType.Violator);*/
        });

        public RelayCommand DoneManagerCommand => GetCommand(o =>
        {
            if (_isAdd)
            {
                Violators.Add(ManagerViolator);
            }
            _manager?.Close();
        });

        public RelayCommand CancelManagerCommand => GetCommand(o =>
        {
            _manager?.Close();
        });
    }
}
