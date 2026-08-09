using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class MainViewModel : ViewModelBase
    {
        public IInfrastructureOrchestrator ViewModelsService { get => Get<IInfrastructureOrchestrator>(); private set => Set(value); }
        public object? CurrentView { get => Get<object>(); private set => Set(value); } // текущее представление в UI 

        public MainViewModel(IInfrastructureOrchestrator viewModelsService)
        {
            ViewModelsService = viewModelsService;
        }

        public RelayCommand SetCurrentViewCommand => GetCommand(vm => OnSetCurrentView(vm)); // устанавливает представление в UI

        public void OnSetCurrentView(object vm)
        {
            if (vm is not null)
            {
                CurrentView = vm;
            }
        } // метод для передачи на vm. Устанавливает представление в UI

        public async Task OnCompletionWork()
        {
            await ViewModelsService.CompletionWorkAsync();
        } // выполняется после завершения программы
    } // vm для MainView
}