using PragmaticAnalyzer.Abstractions;
using System.Net.Sockets;

namespace PragmaticAnalyzer.Services
{
    public class ApiService : IApiService
    {
        public void StartServer()
        {
            // Локальная GGUF-модель запускается через LocalLlamaService.
        }

        public void StopServer()
        {
            // Жизненный цикл GGUF-модели управляется через LocalLlamaService.
        }

        public async Task<bool> IsServerAvailableAsync(
            string host,
            string port,
            CancellationToken ct = default)
        {
            if (!int.TryParse(port, out var portNumber))
            {
                return false;
            }

            try
            {
                using var client = new TcpClient();

                var connectTask = client.ConnectAsync(host, portNumber, ct).AsTask();
                var timeoutTask = Task.Delay(1000, ct);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask != connectTask)
                {
                    return false;
                }

                await connectTask;

                return client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
