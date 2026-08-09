using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.ViewModel.Main;


namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class OutcomesViewModel : ViewModelBase
    {
        private readonly Func<string, DataType, Task> UpdateConfig;
        public Outcomes Outcomes { get; set; }
        public LocalDatabaseSearchViewModel LocalSearch { get; }
        public Technology? SelectedItemTechnology { get => Get<Technology?>(); set => Set(value); }
        public Consequence? SelectedItemConsequence { get => Get<Consequence?>(); set => Set(value); }

        public OutcomesViewModel(
            Outcomes outcomes,
            Func<string, DataType, Task> updateConfig,
            Action<object> setCurrentView)
        {
            Outcomes = outcomes;
            UpdateConfig += updateConfig;
            LocalSearch = new(
                "Поиск только по БД рисков",
                GetSearchSources,
                this,
                setCurrentView);
        }

        private IEnumerable<DatabaseSearchSource> GetSearchSources()
        {
            yield return new("БД рисков: негативные последствия", Outcomes.Consequences);
            yield return new("БД рисков: технологии оценки", Outcomes.Technologys);
        }
    }
}
