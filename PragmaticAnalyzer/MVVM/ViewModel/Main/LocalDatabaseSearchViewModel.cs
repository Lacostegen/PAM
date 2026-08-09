using PragmaticAnalyzer.Core;
using System.Collections.ObjectModel;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class LocalDatabaseSearchViewModel : ViewModelBase
    {
        private const string SearchModeContains = "Содержит текст";
        private const string SearchModeWholeWord = "Слово целиком";
        private const string SearchModeAllWords = "Все слова";
        private const string SearchModeAnyWord = "Любое слово";

        private readonly Func<IEnumerable<DatabaseSearchSource>> _sourceProvider;
        private readonly object _backViewModel;
        private readonly Action<object> _setCurrentView;

        public string Placeholder { get; }

        public ObservableCollection<string> SearchModes { get; } =
        [
            SearchModeContains,
            SearchModeWholeWord,
            SearchModeAllWords,
            SearchModeAnyWord
        ];

        public string SearchText
        {
            get => Get<string>() ?? string.Empty;
            set => Set(value);
        }

        public string SelectedSearchMode
        {
            get => Get<string>() ?? SearchModeContains;
            set
            {
                Set(string.IsNullOrWhiteSpace(value) ? SearchModeContains : value);
                NotifyPropertyChanged(nameof(SearchModeButtonText));
            }
        }

        public string SearchModeButtonText => SelectedSearchMode switch
        {
            SearchModeWholeWord => "Слово",
            SearchModeAllWords => "Все",
            SearchModeAnyWord => "Любое",
            _ => "Текст"
        };

        public LocalDatabaseSearchViewModel(
            string placeholder,
            Func<IEnumerable<DatabaseSearchSource>> sourceProvider,
            object backViewModel,
            Action<object> setCurrentView)
        {
            Placeholder = placeholder;
            _sourceProvider = sourceProvider;
            _backViewModel = backViewModel;
            _setCurrentView = setCurrentView;
            SelectedSearchMode = SearchModeContains;
        }

        public RelayCommand SetSearchModeCommand => GetCommand(mode =>
        {
            if (mode is string selectedMode)
            {
                SelectedSearchMode = selectedMode;
            }
        });

        public RelayCommand FindCommand => GetCommand(o =>
        {
            var query = SearchText.Trim();
            var results = ViewerViewModel.SearchSources(
                _sourceProvider(),
                query,
                SelectedSearchMode);

            _setCurrentView?.Invoke(new DatabaseSearchResultsViewModel(
                query,
                SelectedSearchMode,
                results,
                _backViewModel,
                _setCurrentView));
        }, o => !string.IsNullOrWhiteSpace(SearchText));
    }
}
