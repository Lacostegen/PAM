using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class ViewerViewModel : ViewModelBase
    {
        private Action<object> SetCurrentView;
        public IInfrastructureOrchestrator ViewModelsService { get => Get<IInfrastructureOrchestrator>(); private set => Set(value); }
        public LastUpdateConfig LastUpdateConfig { get => Get<LastUpdateConfig>(); set => Set(value); }

        public ViewerViewModel(IInfrastructureOrchestrator viewModelsService, LastUpdateConfig lastUpdateConfig, Action<object> setCurrentView)
        {
            ViewModelsService = viewModelsService;
            LastUpdateConfig = lastUpdateConfig;
            SetCurrentView = setCurrentView;
        }

        public RelayCommand SetCurrentViewCommand => GetCommand(vm =>
        {
            if (vm is not null)
            {
                SetCurrentView?.Invoke(vm);
            }
        });
    }
}