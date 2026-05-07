using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using System.Collections.ObjectModel;


namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class CurrentStatusViewModel : ViewModelBase
    {
        private readonly Func<string, DataType, Task> UpdateConfig;
        public ObservableCollection<CurrentStatus> CurrentsStatus { get; set; }
        public CurrentStatus? SelectedCurrentStatus { get => Get<CurrentStatus?>(); set => Set(value); }

        public CurrentStatusViewModel(ObservableCollection<CurrentStatus> currentStatus, Func<string, DataType, Task> updateConfig)
        {
            CurrentsStatus = currentStatus;
            UpdateConfig += updateConfig;
        }
    }
}