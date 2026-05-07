using PragmaticAnalyzer.WorkingServer.Core;

namespace PragmaticAnalyzer.Abstractions
{
    /// <summary>
    /// Интерфейс для сервиса взаимодействия с серверами
    /// </summary>
    public interface IApiService
    {
        void StartServer();

        void StopServer();

        Task<bool> IsServerAvailableAsync(
            string host,
            string port,
            CancellationToken ct = default);

        Task<Result<bool>> StartTranslatorServerAsync(
            string koboldCppPath,
            string modelPath,
            string port,
            int contextSize,
            CancellationToken ct = default);

        Task<Result<bool>> StopTranslatorServerAsync();

        Task<Result<T>> SendRequestAsync<T>(
            IRequest request,
            CancellationToken ct = default,
            int delay = 0);
    }
}