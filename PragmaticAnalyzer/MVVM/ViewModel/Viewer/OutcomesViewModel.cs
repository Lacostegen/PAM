using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;


namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class OutcomesViewModel : ViewModelBase
    {
        private readonly Func<string, DataType, Task> UpdateConfig;
        public Outcomes Outcomes { get; set; }
        public Technology? SelectedItemTechnology { get => Get<Technology?>(); set => Set(value); }
        public Consequence? SelectedItemConsequence { get => Get<Consequence?>(); set => Set(value); }

        public OutcomesViewModel(Outcomes outcomes, Func<string, DataType, Task> updateConfig)
        {
            Outcomes = outcomes;
            UpdateConfig += updateConfig;
        }
    }
}