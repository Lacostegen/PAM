using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.Views.Viewer;
using System.Collections.ObjectModel;


namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class ProtectionMeasureViewModel : ViewModelBase
    {
        private readonly Func<string, DataType, Task> UpdateConfig;
        private Action<ProtectionMeasure> Add;
        private Action<ProtectionMeasure> Change;

        public ObservableCollection<ProtectionMeasure> ProtectionMeasures { get; set; }
        public ProtectionMeasure? SelectedProtectionMeasure { get => Get<ProtectionMeasure?>(); set => Set(value); }

        public ProtectionMeasureViewModel(ObservableCollection<ProtectionMeasure> protectionMeasures, Func<string, DataType, Task> updateConfig)
        {
            Add += OnAdd;
            Change += OnChange;
            ProtectionMeasures = protectionMeasures;
            UpdateConfig += updateConfig;
        }

        public RelayCommand AddCommand => GetCommand(o =>
        {
            ProtectionMeasureManagerView view = new(OnAdd, OnChange ,true);
            view.ShowDialog();
        });

        public RelayCommand DeleteCommand => GetCommand(o =>
        {
            if (SelectedProtectionMeasure is null) return;
            for (int i = 0; i < ProtectionMeasures.Count; i++)
            {
                if (ProtectionMeasures[i].Name == SelectedProtectionMeasure.Name)
                {
                    ProtectionMeasures.Remove(ProtectionMeasures[i]);
                    break;
                }
            }
        }, o => SelectedProtectionMeasure is not null);

        public RelayCommand ChangeCommand => GetCommand(o =>
        {
            if (SelectedProtectionMeasure is null) return;
            ProtectionMeasureManagerView view = new(OnAdd, OnChange, false, SelectedProtectionMeasure);
            view.ShowDialog();
        }, o => SelectedProtectionMeasure is not null);

        public void OnAdd(ProtectionMeasure protectionMeasure)
        {
            ProtectionMeasures.Add(protectionMeasure);
        }

        public void OnChange(ProtectionMeasure protectionMeasure)
        {
            SelectedProtectionMeasure.NameGroup = protectionMeasure.NameGroup;
            SelectedProtectionMeasure.Name = protectionMeasure.Name;
            SelectedProtectionMeasure.Number = protectionMeasure.Number;
            SelectedProtectionMeasure.FullName = protectionMeasure.FullName;
            SelectedProtectionMeasure.SecurityClasses = protectionMeasure.SecurityClasses;
        }
    }
}