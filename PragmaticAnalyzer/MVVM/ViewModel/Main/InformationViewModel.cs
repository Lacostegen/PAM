using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.MVVM.ViewModel.AlgorithmInformation;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class InformationViewModel(Action<object> setCurrentView) : ViewModelBase
    {
        private readonly Action<object> SetCurrentView = setCurrentView;
        public FastTextInformationViewModel FasttextInformationVm { get; } = new();
        public WordTwoVecInformationViewModel WordTwoVecInformationVm { get; } = new();
        public TfIdfInformationViewModel TfIdfInformationVm { get; } = new();

        public RelayCommand ShowInformationCommand => GetCommand(o =>
        {
            SetCurrentView?.Invoke(o);
        });
    } // vm для InformationView
}