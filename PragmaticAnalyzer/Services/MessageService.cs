using PragmaticAnalyzer.Abstractions;
using System.Windows;

namespace PragmaticAnalyzer.Services
{
    public class MessageService : IMessageService
    {
        public void ShowInformation(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        } // обертка для MessageBox информативного содержания

        public void ShowError(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        } // обертка для MessageBox с содержанием ошибки
    } // сервис уведомлений MessageBox
}