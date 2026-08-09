using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PragmaticAnalyzer.Services
{
    public static class ExternalLinkService
    {
        public static void Open(string? target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                MessageBox.Show(
                    "Ссылка не указана.",
                    "Ссылка недоступна",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (Uri.TryCreate(target, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = target,
                        UseShellExecute = true
                    });
                    return;
                }

                if (File.Exists(target) || Directory.Exists(target))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = target,
                        UseShellExecute = true
                    });
                    return;
                }

                MessageBox.Show(
                    $"Файл или ссылка не найдены:{Environment.NewLine}{target}",
                    "Ссылка недоступна",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть ссылку:{Environment.NewLine}{target}{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "Ошибка открытия",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
