using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.WorkingServer.Core;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;

namespace PragmaticAnalyzer.Services
{
    public class ApiService : IApiService
    {
        private const string LocalHost = "127.0.0.1";
        private static readonly TimeSpan MatcherStartupTimeout = TimeSpan.FromSeconds(25);
        private const string MatcherExecutableName = "matcher.exe";
        private const string DatabaseDirectoryName = "Database";
        private const string ConfigDirectoryName = "Config";
        private const string ModelsDirectoryName = "Models";
        private const string BinModelExtension = ".bin";

        private Process? _matcherProcess;
        private readonly HttpClient _httpClient;
        private readonly IFileService _fileService;
        private readonly SemaphoreSlim _requestGate = new(1, 1);
        private string? _matcherRuntimePath;

        public bool IsRunningMatcher => _matcherProcess?.HasExited == false;

        public ApiService()
        {
            var handler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(250)
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(30)
            };
            _fileService = new FileService();
        }

        public void StartServer()
        {
            StartMatcherProcess();
        }

        public void StopServer()
        {
            try
            {
                if (_matcherProcess is not null && !_matcherProcess.HasExited)
                {
                    _matcherProcess.Kill(entireProcessTree: true);
                    _matcherProcess.WaitForExit(3000);
                }
            }
            catch
            {
                // Ошибка остановки matcher.exe не должна ломать закрытие приложения.
            }
            finally
            {
                _matcherProcess?.Dispose();
                _matcherProcess = null;
            }
        }

        public async Task<Result<bool>> EnsureMatcherServerAvailableAsync(CancellationToken ct = default)
        {
            if (!int.TryParse(GlobalConfig.MatcherPort, out _))
            {
                return Result<bool>.Failure($"Некорректный порт matcher-сервера: {GlobalConfig.MatcherPort}");
            }

            if (await IsServerAvailableAsync(LocalHost, GlobalConfig.MatcherPort, ct))
            {
                return Result<bool>.Success(true);
            }

            var startResult = StartMatcherProcess();
            if (!startResult.IsSuccess)
            {
                return startResult;
            }

            var deadline = DateTime.UtcNow + MatcherStartupTimeout;
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();

                if (await IsServerAvailableAsync(LocalHost, GlobalConfig.MatcherPort, ct))
                {
                    return Result<bool>.Success(true);
                }

                if (_matcherProcess is not null && _matcherProcess.HasExited)
                {
                    return Result<bool>.Failure(
                        $"matcher.exe завершился сразу после запуска. Проверь файл {GlobalConfig.MatcherPath} и зависимости сервера.");
                }

                await Task.Delay(500, ct);
            }

            return Result<bool>.Failure(
                $"matcher.exe запущен, но порт {GlobalConfig.MatcherPort} не стал доступен за {MatcherStartupTimeout.TotalSeconds:F0} с.");
        }

        private Result<bool> StartMatcherProcess()
        {
            try
            {
                if (IsRunningMatcher)
                {
                    return Result<bool>.Success(true);
                }

                var runtimeMatcherPath = PrepareMatcherRuntimeFiles();
                StopStaleMatcherProcesses(runtimeMatcherPath);

                if (IsPortOpen(LocalHost, GlobalConfig.MatcherPort, TimeSpan.FromMilliseconds(500)))
                {
                    return Result<bool>.Success(true);
                }

                if (!File.Exists(GlobalConfig.MatcherPath))
                {
                    return Result<bool>.Failure(
                        $"Не найден matcher.exe по пути: {GlobalConfig.MatcherPath}. " +
                        "Без него вкладки «Сформировать отчет» и «Работа с моделями» не смогут выполнять поиск, обучение и дообучение.");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = runtimeMatcherPath,
                    WorkingDirectory = GetMatcherRuntimePath(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                _matcherProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                _matcherProcess.Exited += (_, _) =>
                {
                    _matcherProcess?.Dispose();
                    _matcherProcess = null;
                };

                _matcherProcess.Start();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Не удалось запустить matcher.exe: {ex.Message}");
            }
        }

        public Result<string> PrepareMatcherModelPath(string modelPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modelPath))
                {
                    return Result<string>.Failure("Путь к .bin-модели пустой.");
                }

                if (!File.Exists(modelPath))
                {
                    return Result<string>.Failure($"Файл .bin-модели не найден: {modelPath}");
                }

                if (!string.Equals(Path.GetExtension(modelPath), BinModelExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<string>.Failure(
                        "Для Word2Vec/FastText нужен файл .bin. GGUF-модели чата используются отдельно во вкладке «Общение с моделью».");
                }

                var runtimeModelsPath = Path.Combine(GetMatcherRuntimePath(), ModelsDirectoryName);
                Directory.CreateDirectory(runtimeModelsPath);

                var runtimeModelPath = Path.Combine(runtimeModelsPath, Path.GetFileName(modelPath));
                CopyFileIfChanged(modelPath, runtimeModelPath);

                return Result<string>.Success(runtimeModelPath);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Не удалось подготовить .bin-модель для matcher.exe: {ex.Message}");
            }
        }

        public Result<Dictionary<string, string>> PrepareMatcherSourcePaths(IEnumerable<string> sourcePaths)
        {
            try
            {
                var preparedPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var runtimeDatabasePath = Path.Combine(GetMatcherRuntimePath(), DatabaseDirectoryName);
                Directory.CreateDirectory(runtimeDatabasePath);

                foreach (var sourcePath in sourcePaths)
                {
                    if (string.IsNullOrWhiteSpace(sourcePath))
                    {
                        continue;
                    }

                    if (!File.Exists(sourcePath))
                    {
                        return Result<Dictionary<string, string>>.Failure(
                            $"Файл базы данных не найден: {sourcePath}");
                    }

                    if (!string.Equals(Path.GetExtension(sourcePath), ".json", StringComparison.OrdinalIgnoreCase))
                    {
                        return Result<Dictionary<string, string>>.Failure(
                            $"Источник знаний должен быть JSON-файлом: {sourcePath}");
                    }

                    var runtimeSourcePath = Path.Combine(runtimeDatabasePath, Path.GetFileName(sourcePath));
                    CopyFileIfChanged(sourcePath, runtimeSourcePath);
                    preparedPaths[Path.GetFullPath(sourcePath)] = runtimeSourcePath;
                }

                if (preparedPaths.Count == 0)
                {
                    return Result<Dictionary<string, string>>.Failure(
                        "Не удалось подготовить источники знаний для matcher.exe.");
                }

                return Result<Dictionary<string, string>>.Success(preparedPaths);
            }
            catch (Exception ex)
            {
                return Result<Dictionary<string, string>>.Failure(
                    $"Не удалось подготовить источники знаний для matcher.exe: {ex.Message}");
            }
        }

        private string PrepareMatcherRuntimeFiles()
        {
            var runtimePath = GetMatcherRuntimePath();
            Directory.CreateDirectory(runtimePath);

            var runtimeMatcherPath = Path.Combine(runtimePath, MatcherExecutableName);
            CopyFileIfChanged(GlobalConfig.MatcherPath, runtimeMatcherPath);

            CopyDirectoryFiles(GlobalConfig.DatabasePath, Path.Combine(runtimePath, DatabaseDirectoryName), "*.json");
            CopyDirectoryFiles(GlobalConfig.ConfigPath, Path.Combine(runtimePath, ConfigDirectoryName), "*.json");
            Directory.CreateDirectory(Path.Combine(runtimePath, ModelsDirectoryName));

            return runtimeMatcherPath;
        }

        private string GetMatcherRuntimePath()
        {
            if (!string.IsNullOrWhiteSpace(_matcherRuntimePath))
            {
                return _matcherRuntimePath;
            }

            _matcherRuntimePath = ResolveWritableAsciiRuntimePath();
            return _matcherRuntimePath;
        }

        private static string ResolveWritableAsciiRuntimePath()
        {
            var rootCandidates = new[]
            {
                Environment.GetEnvironmentVariable("ProgramData"),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                Path.GetTempPath()
            };

            foreach (var rootCandidate in rootCandidates)
            {
                if (string.IsNullOrWhiteSpace(rootCandidate))
                {
                    continue;
                }

                var candidatePath = Path.Combine(rootCandidate, "PragmaticAnalyzer", "MatcherRuntime");
                if (!IsAsciiPath(candidatePath))
                {
                    continue;
                }

                if (TryEnsureWritableDirectory(candidatePath))
                {
                    return candidatePath;
                }
            }

            var fallbackPath = Path.Combine(
                Path.GetTempPath(),
                "PragmaticAnalyzer",
                "MatcherRuntime");

            if (TryEnsureWritableDirectory(fallbackPath))
            {
                return fallbackPath;
            }

            throw new DirectoryNotFoundException(
                "Не удалось создать рабочую папку matcher.exe с ASCII-путем. Перемести программу в путь без кириллицы или проверь права записи в ProgramData/Temp.");
        }

        private static bool TryEnsureWritableDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);

                var probePath = Path.Combine(path, ".write_probe");
                File.WriteAllText(probePath, "ok");
                File.Delete(probePath);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsAsciiPath(string path)
        {
            return path.All(c => c <= sbyte.MaxValue);
        }

        private static void CopyDirectoryFiles(string sourceDirectory, string destinationDirectory, string searchPattern)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                return;
            }

            Directory.CreateDirectory(destinationDirectory);

            foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectory, searchPattern, SearchOption.TopDirectoryOnly))
            {
                var destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(sourcePath));
                CopyFileIfChanged(sourcePath, destinationPath);
            }
        }

        private static void CopyFileIfChanged(string sourcePath, string destinationPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Исходный файл не найден.", sourcePath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? Environment.CurrentDirectory);

            var sourceInfo = new FileInfo(sourcePath);
            var destinationInfo = new FileInfo(destinationPath);

            if (destinationInfo.Exists
                && destinationInfo.Length == sourceInfo.Length
                && destinationInfo.LastWriteTimeUtc >= sourceInfo.LastWriteTimeUtc)
            {
                return;
            }

            File.Copy(sourcePath, destinationPath, overwrite: true);
            File.SetLastWriteTimeUtc(destinationPath, sourceInfo.LastWriteTimeUtc);
        }

        private static void StopStaleMatcherProcesses(string runtimeMatcherPath)
        {
            var originalMatcherPath = Path.GetFullPath(GlobalConfig.MatcherPath);
            var normalizedRuntimeMatcherPath = Path.GetFullPath(runtimeMatcherPath);

            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(MatcherExecutableName)))
            {
                try
                {
                    var processPath = process.MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(processPath))
                    {
                        continue;
                    }

                    var normalizedProcessPath = Path.GetFullPath(processPath);
                    var isOldWorkspaceMatcher = string.Equals(
                        normalizedProcessPath,
                        originalMatcherPath,
                        StringComparison.OrdinalIgnoreCase);

                    var isAnotherRuntimeMatcher = string.Equals(
                        normalizedProcessPath,
                        normalizedRuntimeMatcherPath,
                        StringComparison.OrdinalIgnoreCase);

                    if (!isOldWorkspaceMatcher && !isAnotherRuntimeMatcher)
                    {
                        continue;
                    }

                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
                catch
                {
                    // Если старый matcher.exe уже завершился или недоступен, продолжаем обычный запуск.
                }
                finally
                {
                    process.Dispose();
                }
            }
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

        private static bool IsPortOpen(string host, string port, TimeSpan timeout)
        {
            if (!int.TryParse(port, out var portNumber))
            {
                return false;
            }

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, portNumber);

                return connectTask.Wait(timeout) && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Result<T>> SendRequestAsync<T>(
            IRequest request,
            CancellationToken ct = default,
            int delay = 0)
        {
            await _requestGate.WaitAsync(ct);

            try
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(delay, ct).ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();
                var responseHttp = await _httpClient.PostAsync(request.Url, request.Content, ct);

                if (!responseHttp.IsSuccessStatusCode)
                {
                    var errorContent = await responseHttp.Content.ReadAsStringAsync(ct);
                    return Result<T>.Failure($"Ошибка сервера: {responseHttp.StatusCode}. Детали: {errorContent}");
                }

                var json = await responseHttp.Content.ReadAsStringAsync(ct);
                var data = await _fileService.LoadJsonAsync<T>(json, ct);
                if (data is null)
                {
                    return Result<T>.Failure("Сервер вернул пустой или некорректный JSON-ответ.");
                }

                return Result<T>.Success(data);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return Result<T>.Failure(
                    "Истекло время ожидания ответа matcher.exe. Обучение или дообучение может идти слишком долго; проверь, не завис ли matcher.exe, и повтори операцию.");
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException("Операция была отменена");
            }
            catch (HttpRequestException ex)
            {
                var details = ex.InnerException?.Message ?? ex.Message;
                var serverAvailable = await IsServerAvailableAsync(
                    LocalHost,
                    GlobalConfig.MatcherPort,
                    CancellationToken.None);

                if (!serverAvailable)
                {
                    return Result<T>.Failure(
                        $"matcher.exe завершился или разорвал соединение во время обработки запроса. Детали: {details}");
                }

                return Result<T>.Failure(
                    $"Сетевая ошибка при запросе к matcher.exe: {details}. Если обучение уже запущено, дождись его завершения или перезапусти программу.");
            }
            catch (JsonException ex)
            {
                return Result<T>.Failure($"Ошибка парсинга JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"Неизвестная ошибка: {ex.Message}");
            }
            finally
            {
                _requestGate.Release();
            }
        }
    }
}
