using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.WorkingServer.Core;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using System.Net.Sockets;


namespace PragmaticAnalyzer.Services
{
    public class ApiService : IApiService
    {
        private Process? _koboldcppProcess; // процесс для сервера, который развертывает локальную модель
        private Process? _matcherProcess; // процесс для сервера работы с моделями
        public bool IsRunningKoboldcpp => _koboldcppProcess?.HasExited == false; // если процесс для сервера , который развертывает локальную модель активен true
        public bool IsRunningMatcher => _matcherProcess?.HasExited == false; // если процесс для сервера работы с моделями активен true
        private readonly HttpClient _httpClient; 
        private readonly IFileService _fileService;

        public ApiService()
        {
            var handler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(250),
            };
            _httpClient = new(handler);
            _fileService = new FileService();
        }

        public void StartServer()
        {
            
            /*           if (!IsRunningKoboldcpp)
                       {
                           var startInfo = new ProcessStartInfo
                           {
                               FileName = GlobalConfig.TranslatorPath,
                               Arguments = $"--model \"{GlobalConfig.TranslatorYandexModelPath}\" --port {GlobalConfig.TranslatorPort} --contextsize 2048",
                               UseShellExecute = false,
                               CreateNoWindow = true,
                               RedirectStandardOutput = false,
                               RedirectStandardError = false
                           };

                           _koboldcppProcess = new Process { StartInfo = startInfo };
                           _koboldcppProcess.Start();
                       }
                       else
                       {
                           return;
                       }*/

            /*  if (!IsRunningMatcher)
              {
                  var startInfo = new ProcessStartInfo
                  {
                      FileName = GlobalConfig.MatcherPath,
                      UseShellExecute = false,
                      CreateNoWindow = true,
                      RedirectStandardOutput = false,
                      RedirectStandardError = false
                  };

                  _matcherProcess = new Process { StartInfo = startInfo };
                   _matcherProcess.Start();
              }
              else
              {
                  return;
              }*/
        } // запуск серверов

        public void StopServer()
        {
            try
            {
                if (_koboldcppProcess is not null && !_koboldcppProcess.HasExited)
                {
                    _koboldcppProcess.Kill(entireProcessTree: true);
                    _koboldcppProcess.WaitForExit(3000);
                }

                if (_matcherProcess is not null && !_matcherProcess.HasExited)
                {
                    _matcherProcess.Kill(entireProcessTree: true);
                    _matcherProcess.WaitForExit(3000);
                }
            }
            catch
            {
                // Пока не показываем MessageBox из сервиса.
                // Ошибки остановки сервера не должны ломать закрытие приложения.
            }
            finally
            {
                _koboldcppProcess?.Dispose();
                _matcherProcess?.Dispose();

                _koboldcppProcess = null;
                _matcherProcess = null;
            }
            /*  try
              {
                  var translatorProcesses = Process.GetProcessesByName("koboldcpp");
                  var matcherProcesses = Process.GetProcessesByName("matcher.exe");
                  var result = translatorProcesses.Union(matcherProcesses).ToArray();

                  foreach (var process in result)
                  {
                      if (!process.HasExited)
                      {
                          process.Kill();
                          process.WaitForExit(3000);
                      }
                      process.Dispose();
                  }
              }
              catch (Exception ex) { }
              finally
              {
                  _koboldcppProcess?.Dispose();
                  _matcherProcess?.Dispose();
                  _koboldcppProcess = null;
                  _matcherProcess = null;
              }*/
        } // остановка серверов
        private void StartKoboldCppServer()
        {
            if (IsRunningKoboldcpp)
            {
                return;
            }

            if (!int.TryParse(GlobalConfig.TranslatorPort, out var port))
            {
                return;
            }

            // Если сервер уже запущен вручную на 5001, второй раз его не запускаем.
            if (IsPortOpen("127.0.0.1", port))
            {
                return;
            }

            if (!File.Exists(GlobalConfig.TranslatorPath))
            {
                return;
            }

            if (!File.Exists(GlobalConfig.TranslatorYandexModelPath))
            {
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = GlobalConfig.TranslatorPath,
                Arguments = $"--model \"{GlobalConfig.TranslatorYandexModelPath}\" --port {GlobalConfig.TranslatorPort} --contextsize 2048",
                UseShellExecute = false,

                // Пока оставляем окно KoboldCpp видимым, чтобы видеть загрузку модели и ошибки.
                CreateNoWindow = false,

                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            _koboldcppProcess = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            _koboldcppProcess.Start();
        }

        private static bool IsPortOpen(string host, int port)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);

                return connectTask.Wait(TimeSpan.FromMilliseconds(500)) && client.Connected;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> IsServerAvailableAsync(string host, string port, CancellationToken ct = default)
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

        public async Task<Result<bool>> StartTranslatorServerAsync(
    string koboldCppPath,
    string modelPath,
    string port,
    int contextSize,
    CancellationToken ct = default)
        {
            try
            {
                if (!int.TryParse(port, out _))
                {
                    return Result<bool>.Failure($"Некорректный порт модели: {port}");
                }

                if (await IsServerAvailableAsync("127.0.0.1", port, ct))
                {
                    return Result<bool>.Success(true);
                }

                if (!File.Exists(koboldCppPath))
                {
                    return Result<bool>.Failure($"Не найден koboldcpp.exe по пути: {koboldCppPath}");
                }

                if (!File.Exists(modelPath))
                {
                    return Result<bool>.Failure($"Не найдена модель по пути: {modelPath}");
                }

                var translatorDirectory = Path.GetDirectoryName(koboldCppPath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = koboldCppPath,
                    Arguments = $"--model \"{modelPath}\" --port {port} --contextsize {contextSize}",
                    WorkingDirectory = translatorDirectory,

                    UseShellExecute = false,
                    CreateNoWindow = true,

                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _koboldcppProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                _koboldcppProcess.OutputDataReceived += (_, _) => { };
                _koboldcppProcess.ErrorDataReceived += (_, _) => { };

                _koboldcppProcess.Start();

                _koboldcppProcess.BeginOutputReadLine();
                _koboldcppProcess.BeginErrorReadLine();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Не удалось запустить модель: {ex.Message}");
            }
        }

        public Task<Result<bool>> StopTranslatorServerAsync()
        {
            try
            {
                if (_koboldcppProcess is not null && !_koboldcppProcess.HasExited)
                {
                    _koboldcppProcess.Kill(entireProcessTree: true);
                    _koboldcppProcess.WaitForExit(3000);
                    _koboldcppProcess.Dispose();
                    _koboldcppProcess = null;

                    return Task.FromResult(Result<bool>.Success(true));
                }

                return Task.FromResult(Result<bool>.Failure(
                    "Процесс модели не был запущен из приложения. Если KoboldCpp запущен вручную, закрой его окно вручную."));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Result<bool>.Failure($"Не удалось остановить модель: {ex.Message}"));
            }
        }

       

        public async Task<Result<T>> SendRequestAsync<T>(IRequest request, CancellationToken ct = default, int delay = 0)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(delay, ct).ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();
                var responseHttp = await _httpClient.PostAsync(request.Url, request.Content, ct);

                if (!responseHttp.IsSuccessStatusCode)
                {
                    var errorContent = await responseHttp.Content.ReadAsStringAsync(ct);
                    return Result<T>.Failure($"SОшибка сервера: {responseHttp.StatusCode}. Детали: {errorContent}");
                }
                var json = await responseHttp.Content.ReadAsStringAsync(ct);
                var data = await _fileService.LoadJsonAsync<T>(json, ct);
                return Result<T>.Success(data);
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException("Операция была отменена");
            }
            catch (HttpRequestException ex)
            {
                return Result<T>.Failure($"Сетевая ошибка: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return Result<T>.Failure($"Ошибка парсинга JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"Неизвестная ошибка: {ex.Message}");
            }
        }
    } // сервис для работы с сервером (внешними ресурсами), управляет процессами и проксирует запросы
}