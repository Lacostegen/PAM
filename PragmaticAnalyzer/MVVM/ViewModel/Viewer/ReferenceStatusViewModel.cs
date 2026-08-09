using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.ViewModel.Main;
using System.Collections.ObjectModel;


namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class ReferenceStatusViewModel : ViewModelBase
    {
        private readonly Func<string, DataType, Task> UpdateConfig;
        public ObservableCollection<ReferenceStatus> ReferencesStatus { get; set; } = [];
        public LocalDatabaseSearchViewModel LocalSearch { get; }
        public ReferenceStatus? SelectedReferenceStatus { get => Get<ReferenceStatus>(); set => Set(value); }

        public ReferenceStatusViewModel(
            ObservableCollection<ReferenceStatus> referencesStatus,
            Func<string, DataType, Task> updateConfig,
            Action<object> setCurrentView)
        {
            ReferencesStatus = referencesStatus;
            UpdateConfig += updateConfig;
            LocalSearch = new(
                "Поиск только по БД эталонного состояния",
                () => [new DatabaseSearchSource("БД эталонного состояния", ReferencesStatus)],
                this,
                setCurrentView);
        }
    }
}
