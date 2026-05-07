using PragmaticAnalyzer.Core;

namespace PragmaticAnalyzer.MVVM.ViewModel.Viewer
{
    public  class VulnerabilitieJvnViewModel : ViewModelBase
    {
        public object? SelectedVulnerabilitie { get => Get<object>(); set => Set(value); }
    }
}