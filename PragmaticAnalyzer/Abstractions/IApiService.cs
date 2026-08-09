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

        Task<Result<bool>> EnsureMatcherServerAvailableAsync(CancellationToken ct = default);

        Result<string> PrepareMatcherModelPath(string modelPath);

        Result<Dictionary<string, string>> PrepareMatcherSourcePaths(IEnumerable<string> sourcePaths);

        Task<Result<T>> SendRequestAsync<T>(
            IRequest request,
            CancellationToken ct = default,
            int delay = 0);
    }
}
