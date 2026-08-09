using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Services;

namespace PragmaticAnalyzer.MVVM.ViewModel.AlgorithmInformation
{
    public class FastTextInformationViewModel : ViewModelBase
    {
        public RelayCommand OpenFileCommand => GetCommand(o =>
        {
            ExternalLinkService.Open(o as string);
        }); // команда для открытия файла
    } // vm для FastTextInformationView
}
