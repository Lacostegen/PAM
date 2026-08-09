using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Services;

namespace PragmaticAnalyzer.MVVM.ViewModel.AlgorithmInformation
{
    public class TfIdfInformationViewModel : ViewModelBase
    {
        public RelayCommand OpenFileCommand => GetCommand(o =>
        {
            ExternalLinkService.Open(o as string);
        }); // команда для открытия файла
    } // vm для TfIdfInformationView
}
