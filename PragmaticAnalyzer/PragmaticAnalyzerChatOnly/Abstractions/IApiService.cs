namespace PragmaticAnalyzer.Abstractions
{
    public interface IApiService
    {
        void StartServer();

        void StopServer();

        Task<bool> IsServerAvailableAsync(
            string host,
            string port,
            CancellationToken ct = default);
    }
}
