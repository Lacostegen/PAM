using Microsoft.Win32;

using System.IO;

namespace PragmaticAnalyzer.Services
{
    public class DialogService
    {
        public const string ExcelFilter = "Excel files (*.xlsx)|*.xlsx";
        public const string WordFilter = "Word files (*.docx)|*.docx";
        public const string JsonFilter = "Json files (*.json)|*.json";
        public const string ModelFilter = "Model files (*.bin)|*.bin";
        public const string GgufModelFilter = "GGUF model files (*.gguf)|*.gguf|All files (*.*)|*.*";
        public const string ExeFilter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";

        public static string? OpenFileDialog(string? filter = null, string? initialDirectory = null)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter
            };

            if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            return dialog.ShowDialog() is true ? dialog.FileName : null;
        } // возварщает абсолютный путь выбранный в проводнике

        public static string? SaveFileDialog(string defaultFileName, string filter)
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName
            };
            return dialog.ShowDialog() is true ? dialog.FileName : null;
        } // возварщает абсолютный путь выбранный в проводнике для сохранения
    } // сервис для работы с проводником Windows
}
