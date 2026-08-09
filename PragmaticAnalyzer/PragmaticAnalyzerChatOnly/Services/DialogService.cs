using Microsoft.Win32;

namespace PragmaticAnalyzer.Services
{
    public static class DialogService
    {
        public const string GgufModelFilter =
            "GGUF model files (*.gguf)|*.gguf|All files (*.*)|*.*";

        public static string? OpenFileDialog(string? filter = null)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter
            };

            return dialog.ShowDialog() is true ? dialog.FileName : null;
        }
    }
}
