using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.ViewModel.Main;
using System.Collections.ObjectModel;


namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class CurrentStatusViewModel : ViewModelBase
    {
        private readonly Func<string, DataType, Task> UpdateConfig;
        public ObservableCollection<CurrentStatus> CurrentsStatus { get; set; }
        public LocalDatabaseSearchViewModel LocalSearch { get; }
        public CurrentStatus? SelectedCurrentStatus { get => Get<CurrentStatus?>(); set => Set(value); }

        public CurrentStatusViewModel(
            ObservableCollection<CurrentStatus> currentStatus,
            Func<string, DataType, Task> updateConfig,
            Action<object> setCurrentView)
        {
            CurrentsStatus = currentStatus;
            UpdateConfig += updateConfig;
            LocalSearch = new(
                "Поиск только по БД текущего состояния",
                () => [new DatabaseSearchSource("БД текущего состояния", CurrentsStatus)],
                this,
                setCurrentView);
        }
    }
}
