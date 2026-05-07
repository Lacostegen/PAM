using PragmaticAnalyzer.Core;

namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public class VulnerabilitieNvdViewModel : ViewModelBase
    {
        public object? SelectedVulnerabilitie { get => Get<object>(); set => Set(value); }
    }
}