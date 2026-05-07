namespace PragmaticAnalyzer.Abstractions
{
    /// <summary>
    /// Интерфейс для сервиса уведомлений
    /// </summary>
    public interface IMessageService
    {
        void ShowInformation(string message, string? title = null);
        void ShowError(string message, string? title = null);
    }
}
