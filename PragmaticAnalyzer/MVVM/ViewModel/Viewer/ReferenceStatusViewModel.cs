using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using System.Collections.ObjectModel;


namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class ReferenceStatusViewModel : ViewModelBase
    {
        private readonly Func<string, DataType, Task> UpdateConfig;
        public ObservableCollection<ReferenceStatus> ReferencesStatus { get; set; } = [];
        public ReferenceStatus? SelectedReferenceStatus { get => Get<ReferenceStatus>(); set => Set(value); }

        public ReferenceStatusViewModel(ObservableCollection<ReferenceStatus> referencesStatus, Func<string, DataType, Task> updateConfig)
        {
            ReferencesStatus = referencesStatus;
            UpdateConfig += updateConfig;
        }
    }
}