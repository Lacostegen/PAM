using System.Net.Http;

namespace PragmaticAnalyzer.Abstractions
{
    /// <summary>
    /// Интерфейс для запроса к серверу
    /// </summary>
    public interface IRequest
    {
        string Url { get; }
        StringContent? Content { get; }
    }
}
