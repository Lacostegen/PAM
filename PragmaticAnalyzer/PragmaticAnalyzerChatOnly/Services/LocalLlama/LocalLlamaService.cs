
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PragmaticAnalyzer.Services.LocalLlama
{
    public class LocalLlamaChatMessage
    {
        public string Role { get; }
        public string Content { get; }

        public LocalLlamaChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    /// <summary>
    /// Локальный запуск обязательной GGUF-модели через llama.cpp server.
    /// Модель передается в llama-server относительным путем, чтобы избежать проблем
    /// с кириллицей в абсолютном пути Windows.
    /// </summary>
    public sealed class LocalLlamaService : IDisposable
    {
        private const string RequiredModelFileName =
            "Qwen3.5-9B-Uncensored-HauhauCS-Aggressive-Q4_K_M.gguf";

        public const string DefaultHost = "127.0.0.1";
        public const int DefaultPort = 11435;
        public static string Endpoint => $"http://{DefaultHost}:{DefaultPort}";

        private readonly SemaphoreSlim _lock = new(1, 1);

        private Process? _serverProcess;
        private HttpClient? _httpClient;
        private bool _isReady;
        private bool _ownsProcess;

        public bool IsLoaded => _isReady && (_serverProcess == null || !_serverProcess.HasExited);

        public string LoadedModelPath { get; private set; } = string.Empty;

        public string NativeBackendPath { get; private set; } = string.Empty;

        public string CurrentLogPath { get; private set; } = string.Empty;

        public bool IsWarmedUp { get; private set; }

        public TimeSpan LastWarmUpElapsed { get; private set; } = TimeSpan.Zero;

        public async Task LoadAsync(
            string modelPath,
            int contextSize = 4096,
            int gpuLayerCount = 0,
            int threadCount = 0,
            int batchSize = 512,
            int microBatchSize = 512,
            int readinessProbeMaxTokens = 24,
            string llamaServerPath = "",
            CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);

            try
            {
                var resolvedModelPath = ResolveRequiredModelPath(modelPath);

                if (string.IsNullOrWhiteSpace(resolvedModelPath))
                {
                    throw new FileNotFoundException(
                        "GGUF-модель не найдена. " +
                        "Выбери существующий .gguf файл или положи модель в папку Translator рядом с exe.");
                }

                if (IsLoaded &&
                    string.Equals(
                        Path.GetFullPath(LoadedModelPath),
                        Path.GetFullPath(resolvedModelPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsWarmedUp)
                    {
                        await WarmUpAsync(readinessProbeMaxTokens, ct);
                    }

                    return;
                }

                if (IsLoaded)
                {
                    _isReady = false;
                    IsWarmedUp = false;
                    StopOwnedProcessIfRunning();
                }

                var serverPath = ResolveLlamaServerPath(llamaServerPath);

                if (string.IsNullOrWhiteSpace(serverPath))
                {
                    throw new FileNotFoundException(
                        "llama-server.exe не найден. " +
                        "Положи llama-server.exe и все DLL из сборки llama.cpp в папку NativeLlama рядом с exe.");
                }

                LoadedModelPath = resolvedModelPath;
                NativeBackendPath = serverPath;
                IsWarmedUp = false;
                LastWarmUpElapsed = TimeSpan.Zero;

                _httpClient?.Dispose();
                _httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(30)
                };

                StopOwnedProcessIfRunning();
                StopStaleLlamaServerProcesses(serverPath);

                if (IsPortOpen(DefaultHost, DefaultPort, TimeSpan.FromMilliseconds(500)))
                {
                    var portOwner = GetPortOwnerDescription(DefaultPort);
                    throw new InvalidOperationException(
                        $"Порт {DefaultPort} уже занят другим процессом. " +
                        (string.IsNullOrWhiteSpace(portOwner) ? string.Empty : $"{portOwner}. ") +
                        "Закрой процесс, который слушает этот порт, и запусти модель снова.");
                }

                StartServerProcess(
                    serverPath,
                    resolvedModelPath,
                    contextSize,
                    gpuLayerCount,
                    threadCount,
                    batchSize,
                    microBatchSize);

                await WaitUntilReadyAsync(ct);

                _isReady = true;
                await WarmUpAsync(readinessProbeMaxTokens, ct);
            }
            catch
            {
                _isReady = false;
                IsWarmedUp = false;
                LoadedModelPath = string.Empty;
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<string> GenerateAsync(
            IEnumerable<LocalLlamaChatMessage> messages,
            int maxTokens = 700,
            float temperature = 0.2f,
            float topP = 0.9f,
            float repeatPenalty = 1.1f,
            CancellationToken ct = default)
        {
            var preparedMessages = PrepareMessages(messages);

            if (preparedMessages.Length == 0)
            {
                return string.Empty;
            }

            if (!IsLoaded)
            {
                throw new InvalidOperationException("Локальный llama-server ещё не запущен.");
            }

            if (_httpClient == null)
            {
                throw new InvalidOperationException("HTTP-клиент локального llama-server не инициализирован.");
            }

            var safeMaxTokens = maxTokens <= 0 ? 700 : maxTokens;
            var safeTemperature = Clamp(temperature, 0.0f, 2.0f);
            var safeTopP = Clamp(topP, 0.05f, 1.0f);
            var safeRepeatPenalty = repeatPenalty <= 0 ? 1.1f : repeatPenalty;

            var requestBody = new
            {
                model = "local-qwen",
                stream = false,
                temperature = safeTemperature,
                top_p = safeTopP,
                repeat_penalty = safeRepeatPenalty,
                max_tokens = safeMaxTokens,
                messages = preparedMessages
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(
                $"{Endpoint}/v1/chat/completions",
                content,
                ct);

            var responseText = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"llama-server вернул ошибку {(int)response.StatusCode}: {responseText}");
            }

            return ExtractAnswer(responseText);
        }

        public async Task WarmUpAsync(
            int maxTokens = 3,
            CancellationToken ct = default)
        {
            var safeMaxTokens = Math.Clamp(maxTokens <= 0 ? 3 : maxTokens, 1, 3);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var probeAnswer = await GenerateAsync(
                    new[]
                    {
                        new LocalLlamaChatMessage(
                            "system",
                            "Ты проверяешь готовность модели. Ответь только одним словом на русском языке."),
                        new LocalLlamaChatMessage(
                            "user",
                            "Напиши слово: готово")
                    },
                    safeMaxTokens,
                    0.1f,
                    0.9f,
                    1.05f,
                    ct);

                if (string.IsNullOrWhiteSpace(probeAnswer))
                {
                    throw new InvalidOperationException(
                        "llama-server запустился, но warm-up генерация вернула пустой ответ." +
                        Environment.NewLine +
                        ReadLogTail(CurrentLogPath));
                }

                IsWarmedUp = true;
            }
            finally
            {
                stopwatch.Stop();
                LastWarmUpElapsed = stopwatch.Elapsed;
            }
        }

        public async IAsyncEnumerable<string> GenerateStreamAsync(
            IEnumerable<LocalLlamaChatMessage> messages,
            int maxTokens = 700,
            float temperature = 0.2f,
            float topP = 0.9f,
            float repeatPenalty = 1.1f,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var preparedMessages = PrepareMessages(messages);

            if (preparedMessages.Length == 0)
            {
                yield break;
            }

            if (!IsLoaded)
            {
                throw new InvalidOperationException("Локальный llama-server ещё не запущен.");
            }

            if (_httpClient == null)
            {
                throw new InvalidOperationException("HTTP-клиент локального llama-server не инициализирован.");
            }

            var safeMaxTokens = maxTokens <= 0 ? 700 : maxTokens;
            var safeTemperature = Clamp(temperature, 0.0f, 2.0f);
            var safeTopP = Clamp(topP, 0.05f, 1.0f);
            var safeRepeatPenalty = repeatPenalty <= 0 ? 1.1f : repeatPenalty;

            var requestBody = new
            {
                model = "local-qwen",
                stream = true,
                temperature = safeTemperature,
                top_p = safeTopP,
                repeat_penalty = safeRepeatPenalty,
                max_tokens = safeMaxTokens,
                messages = preparedMessages
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{Endpoint}/v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Accept.ParseAdd("text/event-stream");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync(ct);

                throw new InvalidOperationException(
                    $"llama-server вернул ошибку {(int)response.StatusCode}: {responseText}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(responseStream, Encoding.UTF8);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(ct);

                if (line == null)
                {
                    yield break;
                }

                if (string.IsNullOrWhiteSpace(line) ||
                    !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var payload = line["data:".Length..].Trim();

                if (string.Equals(payload, "[DONE]", StringComparison.OrdinalIgnoreCase))
                {
                    yield break;
                }

                var delta = ExtractStreamDelta(payload);

                if (!string.IsNullOrEmpty(delta))
                {
                    yield return delta;
                }
            }
        }

        public void Unload()
        {
            _lock.Wait();

            try
            {
                _isReady = false;
                IsWarmedUp = false;
                LastWarmUpElapsed = TimeSpan.Zero;
                LoadedModelPath = string.Empty;

                var serverPath = string.IsNullOrWhiteSpace(NativeBackendPath)
                    ? ResolveLlamaServerPath()
                    : NativeBackendPath;

                StopOwnedProcessIfRunning();

                if (!string.IsNullOrWhiteSpace(serverPath))
                {
                    StopStaleLlamaServerProcesses(serverPath);
                }

                NativeBackendPath = string.Empty;

                _httpClient?.Dispose();
                _httpClient = null;
            }
            finally
            {
                _lock.Release();
            }
        }

        private void StartServerProcess(
            string serverPath,
            string modelPath,
            int contextSize,
            int gpuLayerCount,
            int threadCount,
            int batchSize,
            int microBatchSize)
        {
            var safeContextSize = contextSize <= 0 ? 4096 : contextSize;
            var safeGpuLayerCount = gpuLayerCount < 0 ? 0 : gpuLayerCount;
            var safeBatchSize = batchSize <= 0 ? 512 : batchSize;
            var safeMicroBatchSize = microBatchSize <= 0 ? 512 : microBatchSize;
            var safeThreadCount = threadCount <= 0
                ? Math.Max(4, Math.Min(Environment.ProcessorCount - 2, 10))
                : threadCount;

            var workingDirectory = Path.GetDirectoryName(serverPath)
                ?? Environment.CurrentDirectory;

            var modelArgument = BuildModelArgumentForLlamaServer(
                workingDirectory,
                modelPath);

            CurrentLogPath = CreateSessionLogPath();
            var logPath = CurrentLogPath;
            var launchDiagnostics = BuildLaunchDiagnostics(
                serverPath,
                workingDirectory,
                modelPath,
                modelArgument,
                logPath,
                safeContextSize,
                safeGpuLayerCount,
                safeThreadCount,
                safeBatchSize,
                safeMicroBatchSize);

            File.AppendAllText(
                logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting llama-server: {serverPath}{Environment.NewLine}" +
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Model full path: {modelPath}{Environment.NewLine}" +
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Model argument: {modelArgument}{Environment.NewLine}" +
                launchDiagnostics);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = serverPath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            processStartInfo.ArgumentList.Add("-m");
            processStartInfo.ArgumentList.Add(modelArgument);

            processStartInfo.ArgumentList.Add("-c");
            processStartInfo.ArgumentList.Add(safeContextSize.ToString());

            processStartInfo.ArgumentList.Add("--host");
            processStartInfo.ArgumentList.Add(DefaultHost);

            processStartInfo.ArgumentList.Add("--port");
            processStartInfo.ArgumentList.Add(DefaultPort.ToString());
            // Один пользовательский слот. Нам не нужен сервер на 4 параллельных запроса.
            processStartInfo.ArgumentList.Add("--parallel");
            processStartInfo.ArgumentList.Add("1");

            // Qwen thinking-модели иначе могут вернуть только reasoning_content,
            // а финальный message.content останется пустым.
            processStartInfo.ArgumentList.Add("--reasoning");
            processStartInfo.ArgumentList.Add("off");

            processStartInfo.ArgumentList.Add("--reasoning-budget");
            processStartInfo.ArgumentList.Add("0");

            processStartInfo.ArgumentList.Add("--reasoning-format");
            processStartInfo.ArgumentList.Add("none");

            processStartInfo.ArgumentList.Add("--cache-prompt");

            processStartInfo.ArgumentList.Add("--no-webui");

            processStartInfo.ArgumentList.Add("-t");
            processStartInfo.ArgumentList.Add(safeThreadCount.ToString());

            processStartInfo.ArgumentList.Add("-tb");
            processStartInfo.ArgumentList.Add(safeThreadCount.ToString());

            // Ускоряет обработку prompt'а. Значение безопасное для первого теста.
            processStartInfo.ArgumentList.Add("-b");
            processStartInfo.ArgumentList.Add(safeBatchSize.ToString());

            processStartInfo.ArgumentList.Add("-ub");
            processStartInfo.ArgumentList.Add(safeMicroBatchSize.ToString());

            if (safeGpuLayerCount > 0)
            {
                processStartInfo.ArgumentList.Add("-ngl");
                processStartInfo.ArgumentList.Add(safeGpuLayerCount.ToString());
            }

            _serverProcess = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            _serverProcess.OutputDataReceived += (_, args) =>
                AppendLogLine(logPath, args.Data);

            _serverProcess.ErrorDataReceived += (_, args) =>
                AppendLogLine(logPath, args.Data);

            if (!_serverProcess.Start())
            {
                throw new InvalidOperationException("Не удалось запустить llama-server.exe.");
            }

            _ownsProcess = true;
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();
        }

        private static string BuildModelArgumentForLlamaServer(
            string workingDirectory,
            string modelPath)
        {
            try
            {
                var relativePath = Path.GetRelativePath(
                    workingDirectory,
                    modelPath);

                if (!string.IsNullOrWhiteSpace(relativePath))
                {
                    return relativePath;
                }
            }
            catch
            {
                // Если относительный путь построить не удалось, используем абсолютный.
            }

            return modelPath;
        }

        private async Task WaitUntilReadyAsync(CancellationToken ct)
        {
            if (_httpClient == null)
            {
                throw new InvalidOperationException("HTTP-клиент локального llama-server не инициализирован.");
            }

            var deadline = DateTime.UtcNow.AddMinutes(10);
            Exception? lastException = null;

            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();

                if (_serverProcess != null && _serverProcess.HasExited)
                {
                    await Task.Delay(300, ct);
                    var logTail = ReadLogTail(CurrentLogPath);

                    throw new InvalidOperationException(BuildServerExitMessage(
                        _serverProcess.ExitCode,
                        logTail,
                        CurrentLogPath));
                }

                var healthReady = false;

                try
                {
                    using var healthResponse = await _httpClient.GetAsync(
                        $"{Endpoint}/health",
                        ct);

                    if (healthResponse.IsSuccessStatusCode)
                    {
                        healthReady = true;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    lastException = ex;
                }

                if (healthReady)
                {
                    try
                    {
                        using var modelsResponse = await _httpClient.GetAsync(
                            $"{Endpoint}/v1/models",
                            ct);

                        if (modelsResponse.IsSuccessStatusCode)
                        {
                            return;
                        }
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                    {
                        lastException = ex;
                    }
                }

                await Task.Delay(1000, ct);
            }

            throw new TimeoutException(
                "llama-server не стал готовым за 10 минут." +
                Environment.NewLine +
                (lastException == null ? string.Empty : lastException.Message) +
                Environment.NewLine +
                ReadLogTail(CurrentLogPath));
        }

        private void StopOwnedProcessIfRunning()
        {
            if (_serverProcess == null)
            {
                return;
            }

            try
            {
                if (_ownsProcess && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill(entireProcessTree: true);
                    _serverProcess.WaitForExit(5000);
                }
            }
            catch
            {
                // Остановка backend не должна ломать закрытие приложения.
            }
            finally
            {
                _serverProcess.Dispose();
                _serverProcess = null;
                _ownsProcess = false;
            }
        }

        private static void StopStaleLlamaServerProcesses(string serverPath)
        {
            var expectedPath = Path.GetFullPath(serverPath);
            var processName = Path.GetFileNameWithoutExtension(serverPath);

            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    var processPath = process.MainModule?.FileName;

                    if (string.IsNullOrWhiteSpace(processPath))
                    {
                        continue;
                    }

                    if (!string.Equals(
                            Path.GetFullPath(processPath),
                            expectedPath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(5000);
                    }
                }
                catch
                {
                    // Нет доступа к чужому процессу или он уже завершился.
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private static bool IsPortOpen(string host, int port, TimeSpan timeout)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);

                return connectTask.Wait(timeout) && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private static string GetPortOwnerDescription(int port)
        {
            try
            {
                var ownerPid = GetTcpListenerOwnerPid(port);

                if (ownerPid.HasValue)
                {
                    try
                    {
                        using var process = Process.GetProcessById(ownerPid.Value);
                        return $"Порт {port} занят PID {ownerPid.Value} ({process.ProcessName})";
                    }
                    catch
                    {
                        return $"Порт {port} занят PID {ownerPid.Value}";
                    }
                }

                var properties = IPGlobalProperties.GetIPGlobalProperties();
                var connection = properties
                    .GetActiveTcpListeners()
                    .FirstOrDefault(endpoint => endpoint.Port == port);

                if (connection == null)
                {
                    return string.Empty;
                }

                var activeConnection = properties
                    .GetActiveTcpConnections()
                    .FirstOrDefault(info => info.LocalEndPoint.Port == port);

                if (activeConnection == null)
                {
                    return $"Адрес {connection.Address}:{port} уже слушается";
                }

                return $"Адрес {connection.Address}:{port} уже занят";
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int? GetTcpListenerOwnerPid(int port)
        {
            const int afInet = 2;
            var bufferSize = 0;

            var result = GetExtendedTcpTable(
                IntPtr.Zero,
                ref bufferSize,
                true,
                afInet,
                TcpTableClass.TcpTableOwnerPidListener,
                0);

            if (result != 0 && result != 122)
            {
                return null;
            }

            var tablePointer = Marshal.AllocHGlobal(bufferSize);

            try
            {
                result = GetExtendedTcpTable(
                    tablePointer,
                    ref bufferSize,
                    true,
                    afInet,
                    TcpTableClass.TcpTableOwnerPidListener,
                    0);

                if (result != 0)
                {
                    return null;
                }

                var rowCount = Marshal.ReadInt32(tablePointer);
                var rowPointer = IntPtr.Add(tablePointer, sizeof(int));
                var rowSize = Marshal.SizeOf<TcpRowOwnerPid>();

                for (var i = 0; i < rowCount; i++)
                {
                    var row = Marshal.PtrToStructure<TcpRowOwnerPid>(rowPointer);
                    var rowPort = ConvertNetworkPort(row.LocalPort);

                    if (rowPort == port)
                    {
                        return (int)row.OwningPid;
                    }

                    rowPointer = IntPtr.Add(rowPointer, rowSize);
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(tablePointer);
            }
        }

        private static int ConvertNetworkPort(uint networkPort)
        {
            var bytes = BitConverter.GetBytes(networkPort);
            return (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes, 0));
        }

        private static string ResolveLlamaServerPath(string preferredPath = "")
        {
            var candidates = new List<string>();
            var directory = new DirectoryInfo(Environment.CurrentDirectory);

            if (!string.IsNullOrWhiteSpace(preferredPath))
            {
                candidates.Add(preferredPath);
            }

            candidates.Add(Path.Combine(Environment.CurrentDirectory, "NativeLlama", "llama-server.exe"));
            candidates.Add(Path.Combine(Environment.CurrentDirectory, "llama-server.exe"));

            for (var i = 0; i < 8 && directory != null; i++)
            {
                candidates.Add(Path.Combine(directory.FullName, "NativeLlama", "llama-server.exe"));
                candidates.Add(Path.Combine(directory.FullName, "llama-server.exe"));

                directory = directory.Parent;
            }

            return candidates
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(File.Exists) ?? string.Empty;
        }

        private static string ResolveRequiredModelPath(string modelPath)
        {
            if (!string.IsNullOrWhiteSpace(modelPath) &&
                File.Exists(modelPath) &&
                string.Equals(
                    Path.GetExtension(modelPath),
                    ".gguf",
                    StringComparison.OrdinalIgnoreCase))
            {
                return modelPath;
            }

            var candidates = new List<string>();
            var directory = new DirectoryInfo(Environment.CurrentDirectory);

            candidates.Add(Path.Combine(
                Environment.CurrentDirectory,
                "Translator",
                RequiredModelFileName));

            for (var i = 0; i < 8 && directory != null; i++)
            {
                candidates.Add(Path.Combine(
                    directory.FullName,
                    "Translator",
                    RequiredModelFileName));

                directory = directory.Parent;
            }

            return candidates
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(File.Exists) ?? string.Empty;
        }

        private static string ExtractAnswer(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return string.Empty;
            }

            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;

            if (!root.TryGetProperty("choices", out var choices) ||
                choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0)
            {
                return responseText.Trim();
            }

            var firstChoice = choices[0];

            if (firstChoice.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.Object &&
                message.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.String)
            {
                var answer = CleanAnswer(content.GetString() ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(answer))
                {
                    return answer;
                }

                if (message.TryGetProperty("reasoning_content", out var reasoningContent) &&
                    reasoningContent.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(reasoningContent.GetString()))
                {
                    return string.Empty;
                }

                return answer;
            }

            if (firstChoice.TryGetProperty("text", out var text) &&
                text.ValueKind == JsonValueKind.String)
            {
                return CleanAnswer(text.GetString() ?? string.Empty);
            }

            return responseText.Trim();
        }

        private static object[] PrepareMessages(IEnumerable<LocalLlamaChatMessage>? messages)
        {
            return messages?
                .Where(message =>
                    message != null &&
                    !string.IsNullOrWhiteSpace(message.Role) &&
                    !string.IsNullOrWhiteSpace(message.Content))
                .Select(message => new
                {
                    role = message.Role.Trim(),
                    content = message.Content.Trim()
                })
                .ToArray() ?? Array.Empty<object>();
        }

        private static string ExtractStreamDelta(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return string.Empty;
            }

            try
            {
                using var document = JsonDocument.Parse(payload);
                var root = document.RootElement;

                if (!root.TryGetProperty("choices", out var choices) ||
                    choices.ValueKind != JsonValueKind.Array ||
                    choices.GetArrayLength() == 0)
                {
                    return string.Empty;
                }

                var firstChoice = choices[0];

                if (firstChoice.TryGetProperty("delta", out var delta) &&
                    delta.ValueKind == JsonValueKind.Object)
                {
                    if (delta.TryGetProperty("content", out var content) &&
                        content.ValueKind == JsonValueKind.String)
                    {
                        return CleanAnswerFragment(content.GetString() ?? string.Empty);
                    }

                    if (delta.TryGetProperty("reasoning_content", out var reasoningContent) &&
                        reasoningContent.ValueKind == JsonValueKind.String)
                    {
                        return string.Empty;
                    }
                }

                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.Object &&
                    message.TryGetProperty("content", out var messageContent) &&
                    messageContent.ValueKind == JsonValueKind.String)
                {
                    return CleanAnswerFragment(messageContent.GetString() ?? string.Empty);
                }

                if (firstChoice.TryGetProperty("text", out var text) &&
                    text.ValueKind == JsonValueKind.String)
                {
                    return CleanAnswerFragment(text.GetString() ?? string.Empty);
                }
            }
            catch (JsonException)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        private static void AppendLogLine(string logPath, string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            try
            {
                File.AppendAllText(
                    logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line}{Environment.NewLine}");
            }
            catch
            {
                // Логирование backend не должно ломать работу приложения.
            }
        }

        private static string CreateSessionLogPath()
        {
            var logsPath = GetWritableLogDirectory();

            return Path.Combine(
                logsPath,
                $"llama-server-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        }

        private static string GetWritableLogDirectory()
        {
            var localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                candidates.Add(Path.Combine(localAppData, "PragmaticAnalyzer", "Logs"));
            }

            candidates.Add(Path.Combine(Path.GetTempPath(), "PragmaticAnalyzer", "Logs"));

            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (CanWriteDirectory(candidate))
                {
                    return candidate;
                }
            }

            throw new UnauthorizedAccessException(
                "Не удалось создать папку логов ни в LocalAppData, ни во временной папке Windows.");
        }

        private static bool CanWriteDirectory(string directoryPath)
        {
            try
            {
                Directory.CreateDirectory(directoryPath);

                var probePath = Path.Combine(
                    directoryPath,
                    $".write-test-{Guid.NewGuid():N}.tmp");

                File.WriteAllText(probePath, "ok");
                File.Delete(probePath);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string BuildLaunchDiagnostics(
            string serverPath,
            string workingDirectory,
            string modelPath,
            string modelArgument,
            string logPath,
            int contextSize,
            int gpuLayerCount,
            int threadCount,
            int batchSize,
            int microBatchSize)
        {
            var lines = new List<string>
            {
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Diagnostics:",
                $"  App directory: {Environment.CurrentDirectory}",
                $"  App directory writable: {CanWriteDirectory(Environment.CurrentDirectory)}",
                $"  Log path: {logPath}",
                $"  Server directory: {workingDirectory}",
                $"  Server file readable: {CanReadFile(serverPath)}",
                $"  Model file readable: {CanReadFile(modelPath)}",
                $"  Arguments: -m {modelArgument} -c {contextSize} -t {threadCount} -b {batchSize} -ub {microBatchSize} -ngl {gpuLayerCount}",
                $"  OS: {RuntimeInformation.OSDescription}",
                $"  Process: {(Environment.Is64BitProcess ? "x64" : "x86")}",
                $"  CPU threads visible: {Environment.ProcessorCount}",
                $"  CPU SIMD: SSE4.1={Sse41.IsSupported}, AVX={Avx.IsSupported}, AVX2={Avx2.IsSupported}, FMA={Fma.IsSupported}"
            };

            if (File.Exists(modelPath))
            {
                var modelInfo = new FileInfo(modelPath);
                lines.Add($"  Model size: {FormatBytes(modelInfo.Length)}");

                if (modelInfo.Length < 100L * 1024 * 1024)
                {
                    lines.Add("  WARNING: GGUF-файл выглядит слишком маленьким; возможно, модель скопирована не полностью.");
                }
            }

            if (TryGetMemoryStatus(out var totalPhysicalMemory, out var availablePhysicalMemory))
            {
                lines.Add($"  RAM total: {FormatBytes((long)totalPhysicalMemory)}");
                lines.Add($"  RAM available: {FormatBytes((long)availablePhysicalMemory)}");

                if (File.Exists(modelPath))
                {
                    var modelSize = new FileInfo(modelPath).Length;
                    if (availablePhysicalMemory > 0 && availablePhysicalMemory < (ulong)(modelSize * 1.25))
                    {
                        lines.Add("  WARNING: свободной ОЗУ может быть мало для этой GGUF-модели.");
                    }
                }
            }

            var nativeDlls = Directory.Exists(workingDirectory)
                ? Directory.EnumerateFiles(workingDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : [];

            lines.Add(nativeDlls.Length == 0
                ? "  WARNING: в NativeLlama не найдены DLL; если llama-server не статический, запуск сорвется."
                : $"  NativeLlama DLL: {string.Join(", ", nativeDlls)}");

            var missingRuntimeDlls = new[]
                {
                    "vcruntime140.dll",
                    "vcruntime140_1.dll",
                    "msvcp140.dll"
                }
                .Where(name => !CanFindDll(name, workingDirectory))
                .ToArray();

            if (missingRuntimeDlls.Length > 0)
            {
                lines.Add(
                    "  WARNING: не найдены DLL Microsoft Visual C++ Runtime: " +
                    string.Join(", ", missingRuntimeDlls) +
                    ". На другом ПК может потребоваться Microsoft Visual C++ Redistributable 2015-2022 x64.");
            }

            if (gpuLayerCount > 0)
            {
                lines.Add(
                    "  WARNING: GPU layers включены. Если на ПК нет подходящего драйвера/GPU backend, поставь GPU layers = 0 и запусти CPU-режим.");
            }

            if (!Environment.Is64BitProcess)
            {
                lines.Add("  ERROR: приложение запущено как x86. Для GGUF-модели нужен x64-процесс.");
            }

            if (!Avx.IsSupported)
            {
                lines.Add("  WARNING: CPU не сообщает поддержку AVX. Обычная сборка llama.cpp может не запуститься; нужна no-AVX/старшая совместимая сборка.");
            }
            else if (!Avx2.IsSupported)
            {
                lines.Add("  WARNING: CPU не сообщает поддержку AVX2. Если NativeLlama собран под AVX2, нужна AVX/no-AVX сборка llama.cpp.");
            }

            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        private static bool CanReadFile(string path)
        {
            try
            {
                using var _ = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool CanFindDll(string dllName, string nativeDirectory)
        {
            if (File.Exists(Path.Combine(nativeDirectory, dllName)) ||
                File.Exists(Path.Combine(Environment.SystemDirectory, dllName)))
            {
                return true;
            }

            var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

            return pathVariable
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(directory =>
                {
                    try
                    {
                        return File.Exists(Path.Combine(directory, dllName));
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        private static string BuildServerExitMessage(
            int exitCode,
            string logTail,
            string logPath)
        {
            var exitCodeHex = unchecked((uint)exitCode).ToString("X8");
            var builder = new StringBuilder();

            builder.AppendLine(
                $"llama-server завершился до готовности. Код выхода: {exitCode} (0x{exitCodeHex}).");

            var hint = GetExitCodeHint(exitCode);
            if (!string.IsNullOrWhiteSpace(hint))
            {
                builder.AppendLine(hint);
            }

            builder.AppendLine($"Лог запуска: {logPath}");
            builder.AppendLine("Что проверить на другом ПК:");
            builder.AppendLine("1. Папка NativeLlama должна содержать llama-server.exe и DLL из той же сборки llama.cpp.");
            builder.AppendLine("2. Установить Microsoft Visual C++ Redistributable 2015-2022 x64, если не хватает vcruntime/msvcp DLL.");
            builder.AppendLine("3. Если CPU старый, использовать совместимую сборку llama-server: AVX, no-AVX или без AVX2.");
            builder.AppendLine("4. Проверить, что GGUF-файл полностью скопирован в Translator и открывается на чтение.");
            builder.AppendLine("5. Для первого запуска поставить GPU layers = 0, context = 2048-4096, batch = 256-512.");

            if (!string.IsNullOrWhiteSpace(logTail))
            {
                builder.AppendLine();
                builder.AppendLine(logTail);
            }

            return builder.ToString().Trim();
        }

        private static string GetExitCodeHint(int exitCode)
        {
            var exitCodeHex = unchecked((uint)exitCode);

            return exitCodeHex switch
            {
                0xC0000005 => "Расшифровка: 0xC0000005 — native access violation. Обычно это несовместимая сборка llama-server/DLL, проблема GPU backend, поврежденная модель или нехватка памяти.",
                0xC000001D => "Расшифровка: 0xC000001D — CPU не поддерживает инструкцию, под которую собран llama-server. Нужна AVX/no-AVX совместимая сборка.",
                0xC0000135 => "Расшифровка: 0xC0000135 — Windows не нашла нужную DLL. Проверь DLL в NativeLlama и Microsoft Visual C++ Redistributable x64.",
                0xC000007B => "Расшифровка: 0xC000007B — смешаны x86/x64 компоненты или битая native-зависимость. Нужна x64-сборка llama-server и x64 DLL.",
                _ => string.Empty
            };
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = ["Б", "КБ", "МБ", "ГБ", "ТБ"];
            var value = (double)Math.Max(0, bytes);
            var unitIndex = 0;

            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:0.##} {units[unitIndex]}";
        }

        private static bool TryGetMemoryStatus(
            out ulong totalPhysicalMemory,
            out ulong availablePhysicalMemory)
        {
            totalPhysicalMemory = 0;
            availablePhysicalMemory = 0;

            try
            {
                var status = new MemoryStatusEx
                {
                    Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
                };

                if (!GlobalMemoryStatusEx(ref status))
                {
                    return false;
                }

                totalPhysicalMemory = status.TotalPhys;
                availablePhysicalMemory = status.AvailPhys;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ReadLogTail(string? logPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
                {
                    return string.Empty;
                }

                var lines = File.ReadAllLines(logPath);
                var tail = lines
                    .Skip(Math.Max(0, lines.Length - 80))
                    .ToArray();

                return string.Join(Environment.NewLine, tail);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return min;
            }

            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static string CleanAnswer(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return CleanAnswerFragment(text)
                .Trim();
        }

        private static string CleanAnswerFragment(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text
                .Replace("<|im_end|>", string.Empty)
                .Replace("<|im_start|>", string.Empty)
                .Replace("</s>", string.Empty);
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int dwOutBufLen,
            bool sort,
            int ipVersion,
            TcpTableClass tblClass,
            uint reserved);

        private enum TcpTableClass
        {
            TcpTableOwnerPidListener = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TcpRowOwnerPid
        {
            public uint State;
            public uint LocalAddr;
            public uint LocalPort;
            public uint RemoteAddr;
            public uint RemotePort;
            public uint OwningPid;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        public void Dispose()
        {
            Unload();
            _lock.Dispose();
        }
    }
}
