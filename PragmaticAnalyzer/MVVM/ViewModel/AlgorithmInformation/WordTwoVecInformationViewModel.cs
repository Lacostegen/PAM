using PragmaticAnalyzer.Core;
using System.Diagnostics;
using System.IO;

namespace PragmaticAnalyzer.MVVM.ViewModel.AlgorithmInformation
{
    public class WordTwoVecInformationViewModel : ViewModelBase
    {
        public RelayCommand OpenFileCommand => GetCommand(o =>
        {
            if (o is string path)
            {
                if (!File.Exists(path))
                {
                    return;
                }

                if (Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                    }
                }
            } // открытие pdf-документа
        }); // команда для открытия файла
    } // vm для TfIdfInformationView
}